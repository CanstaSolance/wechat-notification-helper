using System.Runtime.InteropServices;
using System.Text;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using WeChatNotifier.Core;

namespace WeChatNotifier;

// Path, creation epoch and liveness come from the SAME held handle. No module access.
internal sealed class VerifiedTargetLease : ITargetLease
{
    readonly SafeProcessHandle handle;
    readonly Action<Action> post;
    ProcessWait? wait;
    RegisteredWaitHandle? registration;
    bool disposed;
    public TargetDescriptor Descriptor { get; }
    VerifiedTargetLease(SafeProcessHandle handle, TargetDescriptor descriptor, Action<Action> post)
    { this.handle = handle; Descriptor = descriptor; this.post = post; }
    public bool HasExited => disposed || WaitForSingleObject(handle, 0) != 0x102;
    internal bool IsCurrentUser
    {
        get
        {
            if (disposed || !OpenProcessToken(handle, 8, out var token)) return false;
            using (token)
            using (var target = new WindowsIdentity(token.DangerousGetHandle()))
            using (var current = WindowsIdentity.GetCurrent())
                return target.User != null && current.User != null && target.User.Equals(current.User) && !HasExited;
        }
    }
    internal static VerifiedTargetLease? Open(int pid, int session, string officialPath, Action<Action> post)
    {
        // SYNCHRONIZE is solely for the exit wait; metadata needs QUERY_LIMITED_INFORMATION.
        var handle = OpenProcess(0x1000 | 0x00100000, false, pid);
        bool transferred = false;
        try
        {
            if (handle.IsInvalid || GetProcessId(handle) != (uint)pid ||
                !ProcessIdToSessionId((uint)pid, out uint actualSession) || actualSession != session) return null;
            var path = new StringBuilder(32768); uint size = (uint)path.Capacity;
            if (!QueryFullProcessImageName(handle, 0, path, ref size) ||
                !string.Equals(path.ToString(), officialPath, StringComparison.OrdinalIgnoreCase) ||
                !GetProcessTimes(handle, out long created, out _, out _, out _) ||
                created <= 0 || WaitForSingleObject(handle, 0) != 0x102) return null;
            var lease = new VerifiedTargetLease(handle, new(pid, created, session, path.ToString()), post);
            transferred = true; return lease;
        }
        finally { if (!transferred) handle.Dispose(); }
    }
    public void Watch(Action<TargetDescriptor> exited)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (registration != null) return;
        wait = new ProcessWait(handle);
        registration = ThreadPool.RegisterWaitForSingleObject(wait, (_, _) => post(() => exited(Descriptor)), null, Timeout.Infinite, true);
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        registration?.Unregister(null); registration = null;
        wait?.Dispose(); wait = null;
        handle.Dispose();
    }
    sealed class ProcessWait : WaitHandle
    {
        // A separately owned duplicate lets the runtime retain the wait handle until
        // native unregistration completes, even if our metadata handle is closed first.
        internal ProcessWait(SafeProcessHandle process)
        {
            if (!DuplicateHandle(new IntPtr(-1), process, new IntPtr(-1), out var duplicate, 0, false, 2))
                throw new InvalidOperationException("Cannot retain exit wait handle");
            SafeWaitHandle = duplicate;
        }
    }
    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] static extern bool DuplicateHandle(IntPtr sourceProcess, SafeProcessHandle source, IntPtr targetProcess, out SafeWaitHandle target, uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint options);
    [DllImport("kernel32.dll", SetLastError = true)] static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, int pid);
    [DllImport("kernel32.dll")] static extern uint GetProcessId(SafeProcessHandle process);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] static extern bool ProcessIdToSessionId(uint pid, out uint session);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] static extern bool QueryFullProcessImageName(SafeProcessHandle process, uint flags, StringBuilder name, ref uint size);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] static extern bool GetProcessTimes(SafeProcessHandle process, out long created, out long exited, out long kernel, out long user);
    [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(SafeProcessHandle process, uint milliseconds);
    [DllImport("advapi32.dll", SetLastError = true)] static extern bool OpenProcessToken(SafeProcessHandle process, uint access, out SafeAccessTokenHandle token);
}

internal sealed class PostedOneShotScheduler(Action<Action> post) : IOneShotScheduler
{
    public IDisposable Schedule(TimeSpan delay, Action callback) =>
        new System.Threading.Timer(_ => post(callback), null, delay, Timeout.InfiniteTimeSpan);
}
