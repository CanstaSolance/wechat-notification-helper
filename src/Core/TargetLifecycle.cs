namespace WeChatNotifier.Core;

public readonly record struct TargetDescriptor(int Pid, long Epoch, int Session, string ExecutablePath);
public interface ITargetLease : IDisposable
{
    TargetDescriptor Descriptor { get; }
    bool HasExited { get; }
    void Watch(Action<TargetDescriptor> exited);
}
public interface IOneShotScheduler { IDisposable Schedule(TimeSpan delay, Action callback); }

// Shared by the real Shell source and capability-isolated tests. Caller serializes callbacks.
public sealed class TargetLifecycle(string officialPath, int session,
    Func<IEnumerable<ITargetLease>> snapshot, IOneShotScheduler scheduler, bool resident = false) : IDisposable
{
    public static readonly TimeSpan HandoffGrace = TimeSpan.FromSeconds(4);
    readonly Dictionary<int, ITargetLease> targets = [];
    IDisposable? waiting;
    long generation;
    bool stopped;
    public int Count => targets.Count;
    public bool Waiting => waiting != null;
    public bool IsStopped => stopped;
    public event Action? Ended;
    public event Action? Changed;
    public bool ContainsLive(int pid, long epoch) => !stopped && targets.TryGetValue(pid, out var lease) &&
        lease.Descriptor.Epoch == epoch && !lease.HasExited;

    public void Reconcile()
    {
        if (stopped) return;
        // Only startup, enabling, last-exit and the single grace deadline call this.
        try { foreach (var candidate in snapshot()) Admit(candidate); }
        catch { /* Keep the existing leases; an empty set still has a fixed deadline. */ }
    }
    public bool Admit(ITargetLease candidate)
    {
        var key = candidate.Descriptor;
        if (stopped || key.Pid <= 0 || key.Epoch <= 0 || key.Session != session ||
            !string.Equals(key.ExecutablePath, officialPath, StringComparison.OrdinalIgnoreCase) || candidate.HasExited)
        { candidate.Dispose(); return false; }
        targets.TryGetValue(key.Pid, out var previous);
        if (previous != null && previous.Descriptor.Epoch >= key.Epoch)
        {
            if (!ReferenceEquals(previous, candidate)) candidate.Dispose();
            return previous.Descriptor == key && !previous.HasExited;
        }
        targets[key.Pid] = candidate;
        try { candidate.Watch(Exited); }
        catch
        {
            targets.Remove(key.Pid); candidate.Dispose();
            if (previous != null) targets[key.Pid] = previous;
            return false;
        }
        previous?.Dispose();
        CancelWait();
        Changed?.Invoke();
        return true;
    }
    public bool TryGet(int pid, out TargetDescriptor descriptor)
    {
        descriptor = default;
        if (stopped || !targets.TryGetValue(pid, out var lease)) return false;
        if (lease.HasExited) { Exited(lease.Descriptor); return false; }
        descriptor = lease.Descriptor; return true;
    }
    void Exited(TargetDescriptor key)
    {
        if (stopped || !targets.TryGetValue(key.Pid, out var lease) || lease.Descriptor != key) return;
        targets.Remove(key.Pid); lease.Dispose();
        if (targets.Count != 0) { Changed?.Invoke(); return; }
        Reconcile();
        if (targets.Count == 0 && waiting == null && !stopped)
        {
            long ticket = ++generation;
            waiting = scheduler.Schedule(HandoffGrace, () =>
            {
                if (stopped || ticket != generation) return;
                waiting?.Dispose(); waiting = null;
                Reconcile(); // One final bounded snapshot, never restart an empty wait.
                if (targets.Count != 0 || stopped) return;
                if (resident) { Changed?.Invoke(); return; }
                Dispose(); Ended?.Invoke();
            });
        }
        Changed?.Invoke();
    }
    void CancelWait() { ++generation; waiting?.Dispose(); waiting = null; }
    public void Dispose()
    {
        if (stopped) return;
        stopped = true; CancelWait();
        foreach (var lease in targets.Values) lease.Dispose();
        targets.Clear();
    }
}
