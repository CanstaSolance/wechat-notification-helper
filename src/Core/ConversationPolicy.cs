using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace WeChatNotifier.Core;

// Parsing approach adapted from ZDDIAO-MAX, MIT; see THIRD_PARTY_NOTICES.txt.
// Names and UIA identities are ephemeral. Do not serialize these objects.
public sealed class ConversationRow(string runtimeId, string automationId, string displayName, int unread, bool selected)
{
    public string RuntimeId { get; } = runtimeId;
    public string AutomationId { get; } = automationId;
    public string DisplayName { get; } = displayName;
    public int Unread { get; } = unread;
    public bool Selected { get; } = selected;
    public override string ToString() => "ConversationRow[redacted]";
}
public static class ConversationParser
{
    static readonly Regex Unread = new(@"^(?:\[\s*(?<n>\d+)\s*条?\s*\]|(?<n>\d+)\s*条(?:未读(?:消息)?|新消息))$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50));
    public static ConversationRow? Parse(string runtimeId, string automationId, string rawName, bool selected)
    {
        if (rawName.Length > 4096 || !automationId.StartsWith("session_item_", StringComparison.Ordinal) ||
            automationId.Length <= 13 || string.IsNullOrEmpty(runtimeId)) return null;
        var lines = rawName.Replace("\r", "").Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).Take(32).ToArray();
        if (lines.Length < 2 || !ValidName(lines[0])) return null;
        int unread = -1; // Unknown is not zero; previews are never used to infer arrivals.
        foreach (var line in lines.Skip(1))
        {
            var match = Unread.Match(line);
            if (match.Success && (!int.TryParse(match.Groups["n"].Value, out unread) || unread > 100000)) return null;
        }
        return new(runtimeId, automationId, lines[0], unread, selected);
    }
    public static bool ValidName(string? name)
    {
        // Preserve the existing 64 UTF-16 code-unit limit; never truncate or normalize a name.
        if (string.IsNullOrWhiteSpace(name) || name.Length > 64) return false;
        for (int offset = 0; offset < name.Length;)
        {
            if (Rune.DecodeFromUtf16(name.AsSpan(offset), out var rune, out int consumed) != OperationStatus.Done)
                return false;
            if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.Control or UnicodeCategory.Format) return false;
            offset += consumed;
        }
        return true;
    }
}
public static class ConversationSnapshotFingerprint
{
    public static string Create(IReadOnlyList<ConversationRow> rows)
    {
        using var buffer = new MemoryStream();
        using var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true);
        writer.Write(1); // Encoding version and row count make field/row boundaries explicit.
        writer.Write(rows.Count);
        foreach (var row in rows.OrderBy(x => x.RuntimeId, StringComparer.Ordinal)
            .ThenBy(x => x.AutomationId, StringComparer.Ordinal).ThenBy(x => x.DisplayName, StringComparer.Ordinal))
        {
            Field(row.RuntimeId); Field(row.AutomationId); Field(row.DisplayName);
        }
        writer.Flush();
        var bytes = buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length));
        try { return Convert.ToHexString(SHA256.HashData(bytes)); }
        finally { CryptographicOperations.ZeroMemory(bytes); }

        void Field(string value)
        {
            writer.Write(value.Length);
            foreach (char codeUnit in value) writer.Write((ushort)codeUnit); // Lossless, even for identifier code units.
        }
    }
}
public sealed class UnreadBaseline
{
    Dictionary<string, (string Id, string Name, int Unread)> previous = [];
    string? currentContext;
    public IReadOnlyList<ConversationRow> Update(IReadOnlyList<ConversationRow> rows, string context = "")
    {
        if (currentContext != context) { previous.Clear(); currentContext = context; }
        if (rows.Count > 128) { Reset(); return []; }
        var unique = rows.Where(x => x.Unread >= 0).GroupBy(x => x.RuntimeId).Where(g => g.Count() == 1).Select(g => g.Single()).ToArray();
        var changes = unique.Where(x => x.Unread > 0 && previous.TryGetValue(x.RuntimeId, out var prior) &&
            prior.Id == x.AutomationId && prior.Name == x.DisplayName && x.Unread > prior.Unread).ToArray();
        previous = unique.ToDictionary(x => x.RuntimeId, x => (x.AutomationId, x.DisplayName, x.Unread));
        return changes;
    }
    public void Reset() { previous.Clear(); currentContext = null; }
}
public sealed class ConversationTickets
{
    sealed class Ticket(string epoch, ConversationRow row, long expires)
    { public string Epoch = epoch; public ConversationRow Row = row; public long Expires = expires; }
    readonly Dictionary<string, Ticket> tickets = [];
    public string Issue(string epoch, ConversationRow row, long now)
    {
        foreach (var key in tickets.Where(x => x.Value.Expires <= now).Select(x => x.Key).ToArray()) tickets.Remove(key);
        if (tickets.Count >= 16) tickets.Clear();
        string token = Guid.NewGuid().ToString("N");
        tickets.Add(token, new(epoch, row, now + 120000));
        return token;
    }
    public ConversationRow? Consume(string token, string epoch, IReadOnlyList<ConversationRow> current, long now)
    {
        if (!Guid.TryParseExact(token, "N", out _) || !tickets.Remove(token, out var ticket) || ticket.Expires <= now || ticket.Epoch != epoch) return null;
        var matches = current.Where(x => x.DisplayName == ticket.Row.DisplayName).ToArray();
        if (matches.Length != 1) return null;
        var row = matches[0];
        return row.RuntimeId == ticket.Row.RuntimeId && row.AutomationId == ticket.Row.AutomationId ? row : null;
    }
    public void Clear() => tickets.Clear();
}
