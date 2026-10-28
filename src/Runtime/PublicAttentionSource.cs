using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using WeChatNotifier.Core;

namespace WeChatNotifier;

// Public out-of-process attention requests, not a per-message source.
// Native calls here only observe/subscribe; no activation, input or window mutation.
internal sealed class PublicAttentionSource : NativeWindow, IAttentionSource
{
    static string OfficialPath => OfficialApp.Current?.Path ?? "";
    readonly string targetPath = OfficialPath;
    readonly string targetName = OfficialApp.Current?.Name ?? "Weixin";
    TargetLifecycle targets;
    readonly Action<Action> post;
    readonly bool resident;
    uint message;
    bool registered, stopped;
    System.Threading.Timer? waitingScan;
    long scanGeneration;
    internal bool Registered => registered;
    internal bool ContainsLive(int pid, long birthFileTime) => registered && !stopped && targets.ContainsLive(pid, birthFileTime);
    internal int BoundCount => targets.Count;
    internal bool? LastDeregisterSucceeded { get; private set; }
    readonly int session = CurrentSession();
    static int CurrentSession() { using var process = Process.GetCurrentProcess(); return process.SessionId; }
    public event Action<Attention>? Attention;
    public event Action? Ended;
    internal Func<bool>? SuppressGeneric { get; set; }
    internal void EmitConversation(Attention attention)
    {
        if (registered && !stopped) Attention?.Invoke(attention);
    }
    internal event Action? StateChanged;
    internal string Phase => stopped ? "stopped" : !registered ? "paused" : targets.Count == 0 ? "waiting" : "active";
    internal PublicAttentionSource(Action<Action> post, bool resident = false)
    {
        this.post = post; this.resident = resident;
        targets = NewTargets();
    }
#if CAPABILITY_TEST
    // This constructor is compiled only into the isolated capability harness.
    internal PublicAttentionSource(Action<Action> post, string testPath, string testName)
    {
        this.post = post; resident = true; targetPath = testPath; targetName = testName;
        targets = NewTargets();
    }
#endif
    TargetLifecycle NewTargets()
    {
        var created = new TargetLifecycle(targetPath, session, Snapshot, new PostedOneShotScheduler(post), resident);
        created.Ended += () => { if (ReferenceEquals(targets, created) && !stopped) Ended?.Invoke(); };
        created.Changed += () => { if (ReferenceEquals(targets, created) && !stopped) NotifyState(); };
        return created;
    }
    IEnumerable<ITargetLease> Snapshot() => ProcessCandidates.Read(targetName, targetPath, session, post);
    void CancelWaitingScan()
    {
        ++scanGeneration;
        waitingScan?.Dispose(); waitingScan = null;
    }
    void NotifyState()
    {
        CancelWaitingScan();
        if (resident && registered && !stopped && targets.Count == 0)
        {
            long ticket = scanGeneration;
            waitingScan = new System.Threading.Timer(_ => post(() =>
            {
                if (stopped || !registered || ticket != scanGeneration || targets.Count != 0) return;
                waitingScan?.Dispose(); waitingScan = null;
                targets.Reconcile();
                if (ticket == scanGeneration) NotifyState();
            }), null, ProcessCandidates.Interval, Timeout.InfiniteTimeSpan);
        }
        StateChanged?.Invoke();
    }
    public void Start(bool enabled)
    {
        if (stopped) throw new ObjectDisposedException(nameof(PublicAttentionSource));
        CreateHandle(new CreateParams { Caption = "WeChatNotifier.PublicAttention", ExStyle = 0x80 });
        message = RegisterWindowMessage("SHELLHOOK");
        if (message == 0) throw new InvalidOperationException("Shell 消息不可用");
        SetEnabled(enabled);
        if (!resident && enabled && targets.Count == 0) throw new InvalidOperationException("没有可绑定的官方微信进程");
        NotifyState();
    }
    public void SetEnabled(bool enabled)
    {
        if (stopped || enabled == registered) return;
        if (enabled)
        {
            if (targets.IsStopped) targets = NewTargets();
            targets.Reconcile();
        }
        bool ok = enabled ? RegisterShellHookWindow(Handle) : DeregisterShellHookWindow(Handle);
        if (!enabled) LastDeregisterSucceeded = ok;
        if (!ok) throw new InvalidOperationException("Shell 监听切换失败");
        registered = enabled;
        if (!enabled) targets.Dispose();
        NotifyState();
    }
    bool Validate(IntPtr window, out string identity)
    {
        identity = "";
        if (!OfficialApp.CurrentDiskMatches) return false;
        if (!IsWindow(window)) return false;
        GetWindowThreadProcessId(window, out uint pid);
        if (!targets.TryGet((int)pid, out var descriptor))
        {
            var candidate = VerifiedTargetLease.Open((int)pid, session, targetPath, post);
            if (candidate == null || !targets.Admit(candidate) || !targets.TryGet((int)pid, out descriptor)) return false;
        }
        try
        {
            GetWindowThreadProcessId(window, out uint again);
            if (again != pid || !IsWindow(window) || !targets.TryGet((int)pid, out var confirm) || confirm != descriptor) return false;
            identity = $"{window.ToInt64()}:{pid}:{descriptor.Epoch}";
            return true;
        }
        catch { return false; }
    }
    static bool OfficialForeground()
    {
        GetWindowThreadProcessId(GetForegroundWindow(), out uint pid);
        try
        {
            using var process = Process.GetProcessById((int)pid);
            return process.SessionId == CurrentSession() &&
                string.Equals(ProcessPath(process.Id), OfficialPath, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }
    protected override void WndProc(ref Message m)
    {
        if (registered && !stopped && message != 0 && (uint)m.Msg == message)
        {
            uint kind = unchecked((uint)m.WParam.ToInt64());
            if (kind is 1 or 0x8006)
            {
                bool valid = Validate(m.LParam, out string identity);
                if (kind == 0x8006 && SuppressGeneric?.Invoke() != true) Attention?.Invoke(new(identity, valid, valid && OfficialForeground()));
            }
        }
        base.WndProc(ref m);
    }
    public void Stop()
    {
        if (stopped) return;
        stopped = true;
        CancelWaitingScan();
        int error = 0;
        try
        {
            if (registered)
            {
                LastDeregisterSucceeded = DeregisterShellHookWindow(Handle);
                if (LastDeregisterSucceeded == false) error = Marshal.GetLastWin32Error();
            }
        }
        finally
        {
            try { if (Handle != IntPtr.Zero) DestroyHandle(); }
            finally
            {
                registered = false;
                targets.Dispose();
                BoundedStatus.Cleanup(LastDeregisterSucceeded, error, Handle == IntPtr.Zero);
            }
        }
    }
    public void Dispose() => Stop();
    internal static string? ProcessPath(int pid)
    {
        using var handle = OpenProcess(0x1000, false, pid); // QUERY_LIMITED_INFORMATION only.
        if (handle.IsInvalid) return null;
        var path = new StringBuilder(32768); uint size = (uint)path.Capacity;
        return QueryFullProcessImageName(handle, 0, path, ref size) ? path.ToString() : null;
    }
    [DllImport("kernel32.dll", SetLastError = true)] static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, int pid);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] static extern bool QueryFullProcessImageName(SafeProcessHandle process, uint flags, StringBuilder name, ref uint size);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] static extern bool RegisterShellHookWindow(IntPtr window);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] static extern bool DeregisterShellHookWindow(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern uint RegisterWindowMessage(string name);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
}

