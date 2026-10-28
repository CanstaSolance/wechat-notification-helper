using System.Xml.Linq;

namespace WeChatNotifier.Core;

public interface IStartupStore
{
    string? Read();
    void Write(string command);
    void Remove();
}
public sealed record ResidentConfiguration(int Version, string Mode, string ExecutablePath, bool Enabled);
public static class OwnedStartup
{
    public static string Command(string executable) => "\"" + executable + "\" --observe-wechat";
    public static bool IsObserverArguments(string[] args) => args.Length == 1 && args[0] == "--observe-wechat";
    public static bool IsResidentArguments(string[] args) => args.Length == 1 && args[0] == "--resident-notifications-only";
    public static bool ConfigurationMatches(ResidentConfiguration? config, string executable) =>
        config != null && config.Version == 1 && config.Mode == "process-observer-notifications-only" &&
        string.Equals(config.ExecutablePath, executable, StringComparison.OrdinalIgnoreCase);
    public static void AssertOwnedOrAbsent(IStartupStore store, string expected)
    {
        if (store.Read() is { } value && !string.Equals(value, expected, StringComparison.Ordinal))
            throw new InvalidOperationException("登录启动项不属于本组件，未覆盖");
    }
    public static void Install(IStartupStore store, string expected)
    {
        AssertOwnedOrAbsent(store, expected);
        if (store.Read() == null) store.Write(expected);
        if (store.Read() != expected) throw new InvalidOperationException("登录启动项读回不一致");
    }
    public static void Remove(IStartupStore store, string expected)
    {
        AssertOwnedOrAbsent(store, expected);
        if (store.Read() != null) store.Remove();
        if (store.Read() != null) throw new InvalidOperationException("登录启动项移除失败");
    }
}
public static class OptionalIconDecoration
{
    public static bool TryApply(Action verifyIdentity, Func<bool> writeAndReadBack)
    {
        verifyIdentity(); // Security validation must propagate, including access failures.
        try { return writeAndReadBack(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { return false; } // Only this optional decoration operation may fail softly.
    }
}
public static class NoticeIdentity
{
    // Windows history identity only; never a conversation locator or source proof.
    public static string Tag(string? conversationName, string? submissionId) =>
        ConversationParser.ValidName(conversationName) && submissionId?.Length == 32 &&
        Guid.TryParseExact(submissionId, "N", out var id) ? "named-" + id.ToString("N") : "attention";
}
public static class PrivacyPayload
{
    public static string Build(string nonce, string? logoPath, bool silent) => Build(nonce, logoPath, silent, null);
    // A validated UIA conversation label is not proof of a group's individual sender.
    // Generic Shell attention supplies null. No preview/body parameter exists.
    public static string Build(string nonce, string? logoPath, bool silent, string? verifiedSenderName, string? routeTicket = null)
    {
        if (!Guid.TryParseExact(nonce, "N", out _)) throw new ArgumentException("Invalid local payload");
        Uri? uri = null;
        if (logoPath != null)
        {
            if (!Path.IsPathFullyQualified(logoPath) || !logoPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid local payload");
            uri = new Uri(logoPath);
            if (!uri.IsFile || uri.IsUnc) throw new ArgumentException("Logo must be a local file");
        }
        string title = SenderTitle(verifiedSenderName);
        bool named = ConversationParser.ValidName(verifiedSenderName);
        var toast = new XElement("toast", new XAttribute("launch", "action=open-wechat&submission=" + nonce),
            new XElement("visual", new XElement("binding", new XAttribute("template", "ToastGeneric"),
                new XElement("text", title), new XElement("text", "消息正文已隐藏，点击打开微信"),
                uri == null ? null : new XElement("image", new XAttribute("placement", "appLogoOverride"), new XAttribute("src", uri.AbsoluteUri)))));
        if (silent) toast.Add(new XElement("audio", new XAttribute("silent", "true")));
        return toast.ToString(SaveOptions.DisableFormatting);
    }
    public static string BuildValidation(string nonce, string? logoPath, bool silent)
    {
        var toast = XElement.Parse(Build(nonce, logoPath, silent));
        var text = toast.Descendants("text").ToArray();
        text[0].Value = "功能验证：点击打开原微信";
        text[1].Value = "这是一条功能验证通知，不代表收到新消息。";
        return toast.ToString(SaveOptions.DisableFormatting);
    }
    static string SenderTitle(string? name)
    {
        return ConversationParser.ValidName(name) ? name! : "微信有新提醒";
    }
}
