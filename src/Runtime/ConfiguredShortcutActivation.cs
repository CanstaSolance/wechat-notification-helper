using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
namespace WeChatNotifier;

// Native activation is retained from the locally accepted implementation.
// Only user-confirmed bindings for the current signed image can enter this path.
internal static class ConfiguredShortcutActivation
{
    internal sealed record Binding(int Schema,string Shortcut,int Key,bool Control,bool Alt,bool Shift,
        string ImageVersion,string ImageHash,string UserSid,string Evidence,bool Paused=false);
    static long blockedUntil;
    static int unconfirmed;
    internal static string LastReadCode {get;private set;}="NOT_READ";
    internal static object Health=>new{cooldownMilliseconds=Math.Max(0,Volatile.Read(ref blockedUntil)-Environment.TickCount64),consecutiveUnconfirmed=Volatile.Read(ref unconfirmed)};
    internal static void Write(Binding value)=>AppState.Save("activation-shortcut.json",value);
    internal static Binding? Read()
    {
        var b=AppState.Read<Binding>("activation-shortcut.json");
        if(b==null){LastReadCode="NOT_CONFIGURED_OR_UNREADABLE";return null;}
        if(b.Schema!=1||b.Evidence is not ("USER_CONFIRMED_PENDING_NEXT_CLICK" or "LOCAL_SETTINGS_AND_NATIVE_ACTIVATION_VERIFIED")||
            !TryParse(b.Shortcut,out int key,out bool ctrl,out bool alt,out bool shift)||key!=b.Key||ctrl!=b.Control||alt!=b.Alt||shift!=b.Shift)
        {LastReadCode="INVALID_BINDING";return null;}
        using var user=WindowsIdentity.GetCurrent();
        if(user.User?.Value!=b.UserSid){LastReadCode="SID_MISMATCH";return null;}
        LastReadCode="VALID";return b;
    }
    internal static async Task<bool> Confirm(string shortcut,bool paused)
    {
        if(!TryParse(shortcut,out int key,out bool ctrl,out bool alt,out bool shift)||OfficialApp.Current is not {} image)return false;
        var fresh=await Task.Run(()=>OfficialApp.Verify(image.Path));
        if(fresh==null||fresh.Hash!=image.Hash)return false;
        using var user=WindowsIdentity.GetCurrent();
        var b=new Binding(1,shortcut,key,ctrl,alt,shift,fresh.Version,fresh.Hash,user.User!.Value,"USER_CONFIRMED_PENDING_NEXT_CLICK",paused);
        Write(b);Volatile.Write(ref blockedUntil,0);Interlocked.Exchange(ref unconfirmed,0);
        return Read()==b;
    }
    internal static void RecordFailure(ApplicationActivation.Result result,Binding binding)
    {
        if(result.Foreground||result.Code is "USER_KEYS_BUSY_NO_INPUT" or "INPUT_DESKTOP_NOT_READY" or "SHORTCUT_PAUSED" or "SHORTCUT_FAILURE_COOLDOWN")return;
        if(result.Code.Contains("PROCESS_",StringComparison.Ordinal))return;
        if(result.Code=="SHORTCUT_RESULT_NOT_CONFIRMED"&&Interlocked.Increment(ref unconfirmed)<2)return;
        Write(binding with{Paused=true,Evidence="USER_CONFIRMED_PENDING_NEXT_CLICK"});
    }
    internal static ApplicationActivation.Result TryActivate(IReadOnlyList<VerifiedTargetLease> leases, Binding binding)
    {
        var timer = Stopwatch.StartNew();
        if (binding.Paused) return new("SHORTCUT_PAUSED");
        if (Environment.TickCount64 < Volatile.Read(ref blockedUntil)) return new("SHORTCUT_FAILURE_COOLDOWN");
        if (IntPtr.Size != 8 || !Environment.UserInteractive || !InputDesktopMatches()) return new("INPUT_DESKTOP_NOT_READY");
        if (leases.Count == 0 || leases.Any(p => p.HasExited || !p.IsCurrentUser)) return new("SHORTCUT_PROCESS_IDENTITY_CHANGED");
        if (Foreground(leases)) return new("ALREADY_OFFICIAL_FOREGROUND", Visible: true, Foreground: true, OverflowCleanup: "NEVER_OPENED");
        using var image = new FileStream(OfficialApp.Current!.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (FileVersionInfo.GetVersionInfo(OfficialApp.Current!.Path).FileVersion != binding.ImageVersion ||
            Convert.ToHexString(SHA256.HashData(image)) != binding.ImageHash) return new("SHORTCUT_VERSION_BINDING_CHANGED");
        int[] observedKeys = [16,17,18,91,92,160,161,162,163,164,165,binding.Key];
        for (int attempt = 0; observedKeys.Any(k => (GetAsyncKeyState(k) & 0x8000) != 0); attempt++)
        {
            if (attempt == 3) return new("USER_KEYS_BUSY_NO_INPUT");
            Thread.Sleep(50);
        }
        if (leases.Any(p => p.HasExited)) return new("SHORTCUT_PROCESS_EXITED_BEFORE_INPUT");
        if (Foreground(leases)) return new("ALREADY_OFFICIAL_FOREGROUND", Visible: true, Foreground: true, OverflowCleanup: "NEVER_OPENED");
        string? inputError=InsertChord(binding);
        if(inputError!=null)return new(inputError);
        bool confirmed = false;
        for (int i = 0; i < 20 && !confirmed; i++) { confirmed = Foreground(leases); if (!confirmed) Thread.Sleep(50); }
        if (!confirmed) Volatile.Write(ref blockedUntil, Environment.TickCount64 + 10000);
        else { Interlocked.Exchange(ref unconfirmed,0); if (binding.Evidence == "USER_CONFIRMED_PENDING_NEXT_CLICK") Write(binding with { Evidence = "LOCAL_SETTINGS_AND_NATIVE_ACTIVATION_VERIFIED" }); }
        return new(confirmed ? "CONFIGURED_SHORTCUT_FOREGROUND_CONFIRMED" : "SHORTCUT_RESULT_NOT_CONFIRMED",
            Visible: confirmed, Foreground: confirmed, OverflowCleanup: "NEVER_OPENED",
            Timings: new() { ["shortcutTotalMs"] = timer.ElapsedMilliseconds });
    }
    static string? InsertChord(Binding binding)
    {
        var keys = new List<ushort>();
        if (binding.Control) keys.Add(17); if (binding.Alt) keys.Add(18); if (binding.Shift) keys.Add(16);
        keys.Add((ushort)binding.Key);
        var input = keys.Select(k => Make(k, false)).Concat(keys.AsEnumerable().Reverse().Select(k => Make(k, true))).ToArray();
        uint sent = SendInput((uint)input.Length, input, Marshal.SizeOf<Input>());
        if (sent != input.Length)
        {
            // Only release down events actually inserted by this batch; never blanket
            // release user modifiers. Original held keys were rejected before the batch.
            var remaining = Outstanding(keys.ToArray(), sent);
            if (remaining.Length > 0) _ = SendInput((uint)remaining.Length, remaining.Select(k => Make(k, true)).ToArray(), Marshal.SizeOf<Input>());
            Volatile.Write(ref blockedUntil, Environment.TickCount64 + 60000);
            return "SHORTCUT_INPUT_NOT_FULLY_INSERTED_NO_RETRY";
        }
        return null;
    }
    internal static ushort[] Outstanding(ushort[] keys, uint inserted)
    {
        var down = new List<ushort>();
        for (int i = 0; i < Math.Min(inserted, keys.Length * 2); i++)
        { if (i < keys.Length) down.Add(keys[i]); else down.Remove(keys[keys.Length * 2 - i - 1]); }
        down.Reverse(); return down.ToArray();
    }
    internal static bool TryParse(string text, out int key, out bool control, out bool alt, out bool shift)
    {
        key=0; control=alt=shift=false;
        var parts=text.ToUpperInvariant().Split('+',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length<2 || parts.Distinct().Count()!=parts.Length) return false;
        foreach(string part in parts)
            switch(part) { case "CTRL": case "CONTROL": if(control)return false;control=true;break;case "ALT":if(alt)return false;alt=true;break;case "SHIFT":if(shift)return false;shift=true;break;
                default: if(key!=0)return false; if(part.Length==1 && part[0] is >= 'A' and <= 'Z')key=part[0];
                    else if(part.StartsWith('F')&&int.TryParse(part[1..],out int fn)&&fn is >=1 and <=24)key=111+fn; else return false;break; }
        return key is >=65 and <=90 && control && alt && !shift;
    }
    static bool InputDesktopMatches()
    {
        nint desktop=OpenInputDesktop(0,false,1);
        if(desktop==0)return false;
        try { return DesktopName(desktop) is string input && input==DesktopName(GetThreadDesktop(GetCurrentThreadId())); }
        finally { CloseDesktop(desktop); }
    }
    static string? DesktopName(nint desktop)
    { var name=new System.Text.StringBuilder(256); return GetUserObjectInformation(desktop,2,name,512,out _) ? name.ToString():null; }
    static bool Foreground(IReadOnlyList<VerifiedTargetLease> leases)
    {
        nint h = GetForegroundWindow();
        uint thread = GetWindowThreadProcessId(h, out uint pid);
        var owner = leases.FirstOrDefault(p => p.Descriptor.Pid == pid && !p.HasExited);
        return owner != null && h != 0 && IsWindowVisible(h) && !IsIconic(h) && IsWindowEnabled(h) &&
            GetForegroundWindow() == h && GetWindowThreadProcessId(h, out uint again) == thread && again == pid && !owner.HasExited;
    }
    static Input Make(ushort key, bool up) => new() { Type = 1, Keyboard = new() { Key = key, Flags = up ? 2u : 0u } };
    [StructLayout(LayoutKind.Sequential)] struct Keyboard { internal ushort Key, Scan; internal uint Flags, Time; internal nuint Extra; }
    [StructLayout(LayoutKind.Explicit, Size = 40)] struct Input { [FieldOffset(0)] internal uint Type; [FieldOffset(8)] internal Keyboard Keyboard; }
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    [StructLayout(LayoutKind.Sequential)] struct FileInformation
    {
        internal uint Attributes;internal System.Runtime.InteropServices.ComTypes.FILETIME Creation,Access,Write;
        internal uint VolumeSerial,SizeHigh,SizeLow,Links,FileIndexHigh,FileIndexLow;
    }
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool GetFileInformationByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle file,out FileInformation information);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern uint GetFinalPathNameByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle file,System.Text.StringBuilder path,uint count,uint flags);
    [DllImport("user32.dll", SetLastError = true)] static extern uint SendInput(uint count, Input[] input, int size);
    [DllImport("user32.dll")] static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(nint h, out uint pid);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(nint h);
    [DllImport("user32.dll")] static extern bool IsWindowEnabled(nint h);
    [DllImport("user32.dll")] static extern bool IsIconic(nint h);
    [DllImport("user32.dll")] static extern nint OpenInputDesktop(uint flags,bool inherit,uint access);
    [DllImport("user32.dll")] static extern bool CloseDesktop(nint desktop);
    [DllImport("user32.dll")] static extern nint GetThreadDesktop(uint thread);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool GetUserObjectInformation(nint handle,int index,System.Text.StringBuilder info,uint bytes,out uint needed);
}

