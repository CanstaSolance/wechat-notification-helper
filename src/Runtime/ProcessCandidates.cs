using System.Diagnostics;
using WeChatNotifier.Core;

namespace WeChatNotifier;

// Low-frequency metadata snapshots, not a process-start event subscription. The .NET
// name lookup enumerates process metadata internally; no modules/content are read.
internal static class ProcessCandidates
{
    internal static readonly TimeSpan Interval = TimeSpan.FromSeconds(3);
    internal static IEnumerable<ITargetLease> Read(string name, string officialPath, int session, Action<Action> post)
    {
        var candidates = Process.GetProcessesByName(name);
        try
        {
            foreach (var process in candidates)
            {
                VerifiedTargetLease? lease = null;
                try { lease = VerifiedTargetLease.Open(process.Id, session, officialPath, post); }
                catch { }
                if (lease != null) { if (lease.IsCurrentUser) yield return lease; else lease.Dispose(); }
            }
        }
        finally { foreach (var process in candidates) process.Dispose(); }
    }
    internal static bool Any(string name, string path, int session)
    {
        using var iterator = Read(name, path, session, action => action()).GetEnumerator();
        if (!iterator.MoveNext()) return false;
        iterator.Current.Dispose(); return true;
    }
}

