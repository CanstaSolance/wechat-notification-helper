using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace WeChatNotifier;

internal static class NotificationIdentity
{
    internal static string Aumid => (Environment.ProcessPath ?? throw new InvalidOperationException("Missing executable")).Replace('\\', '/');
    internal static string LastStage { get; private set; } = "NOT_STARTED";
    static string ShortcutPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),
        "WeChatNotify", "微信提醒助手.lnk");

    internal static void UninstallOwn()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\AppUserModelId\" + Aumid);
        if (key == null) { RemoveOwnShortcut(); return; }
        if (key.GetValue("DisplayName") as string != "微信提醒助手" ||
            !Guid.TryParse(key.GetValue("CustomActivator") as string, out Guid activator))
            throw new InvalidOperationException("Cannot remove mismatched identity");
        using var com = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{" + activator + @"}\LocalServer32");
        if (!(com?.GetValue(null) as string ?? "").StartsWith("\"" + Environment.ProcessPath + "\"", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cannot remove mismatched COM identity");
        RemoveOwnShortcut();
        CommunityToolkit.WinUI.Notifications.ToastNotificationManagerCompat.Uninstall();
    }

    internal static void VerifyExisting()
    {
        string exe = Environment.ProcessPath ?? throw new InvalidOperationException("Missing executable");
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\AppUserModelId\" + Aumid);
        if (key?.GetValue("DisplayName") as string != "微信提醒助手" ||
            !Guid.TryParse(key.GetValue("CustomActivator") as string, out Guid activator))
            throw new InvalidOperationException("Existing notification identity unavailable");
        using var com = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{" + activator + @"}\LocalServer32");
        if (!string.Equals(com?.GetValue(null) as string, "\"" + exe + "\" -ToastActivated", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Existing activator target mismatch");
        object obj = new ShellLinkObject();
        try
        {
            ((IPersistFile)obj).Load(ShortcutPath, 0); // Read only; no shortcut creation/update.
            var target = new StringBuilder(1024);
            ((IShellLink)obj).GetPath(target, target.Capacity, IntPtr.Zero, 0);
            if (!string.Equals(target.ToString(), exe, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Existing shortcut target mismatch");
            var store = (IPropertyStore)obj;
            var idKey = new PropertyKey(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);
            store.GetValue(ref idKey, out var idValue);
            try
            {
                if (idValue.type != 31 || Marshal.PtrToStringUni(idValue.pointer) != Aumid)
                    throw new InvalidOperationException("Existing shortcut identity mismatch");
            }
            finally { PropVariantClear(ref idValue); }
            var activationKey = new PropertyKey(idKey.format, 26);
            store.GetValue(ref activationKey, out var activationValue);
            try
            {
                if (activationValue.type != 72 || Marshal.PtrToStructure<Guid>(activationValue.pointer) != activator)
                    throw new InvalidOperationException("Existing shortcut activator mismatch");
            }
            finally { PropVariantClear(ref activationValue); }
        }
        finally { Marshal.FinalReleaseComObject(obj); }
        Marshal.ThrowExceptionForHR(SetCurrentProcessExplicitAppUserModelID(Aumid));
    }

    internal static bool TrySetOwnIcon(string? pngPath)
    {
        return WeChatNotifier.Core.OptionalIconDecoration.TryApply(VerifyExisting, () =>
        {
            if (pngPath == null) return false;
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\AppUserModelId\" + Aumid, true)
                ?? throw new InvalidOperationException("Own notification identity missing");
            key.SetValue("IconUri", pngPath, RegistryValueKind.String);
            return key.GetValue("IconUri") as string == pngPath;
        });
    }

    internal static void Ensure()
    {
        LastStage = "VERIFY_OWN_REGISTRATION";
        // Preserve the Toolkit sender ID and COM activator; fill in the matching Win32/Shell identity.
        string exe = Environment.ProcessPath ?? throw new InvalidOperationException("Missing executable");
        string appId = Aumid;
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\AppUserModelId\" + appId);
        if (key?.GetValue("DisplayName") as string != "微信提醒助手" ||
            !Guid.TryParse(key.GetValue("CustomActivator") as string, out Guid activator))
            throw new InvalidOperationException("Assistant registration mismatch");
        using var com = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{" + activator + @"}\LocalServer32");
        if (!(com?.GetValue(null) as string ?? "").StartsWith("\"" + exe + "\"", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Assistant COM path mismatch");
        LastStage = "SET_PROCESS_AUMID";
        Marshal.ThrowExceptionForHR(SetCurrentProcessExplicitAppUserModelID(appId));
        Directory.CreateDirectory(Path.GetDirectoryName(ShortcutPath)!);
        object linkObject = new ShellLinkObject();
        try
        {
            var link = (IShellLink)linkObject;
            if (File.Exists(ShortcutPath))
            {
                LastStage = "LOAD_OWN_SHORTCUT_READWRITE";
                ((IPersistFile)linkObject).Load(ShortcutPath, 2);
                var previous = new StringBuilder(1024);
                link.GetPath(previous, previous.Capacity, IntPtr.Zero, 0);
                if (!string.Equals(previous.ToString(), exe, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Existing shortcut is not this assistant");
            }
            LastStage = "SET_OWN_SHORTCUT_TARGET";
            link.SetPath(exe); link.SetArguments(""); link.SetWorkingDirectory(AppContext.BaseDirectory);
            link.SetDescription("微信通用系统提醒 · 独立工具"); link.SetIconLocation(exe, 0);
            var store = (IPropertyStore)linkObject;
            var idKey = new PropertyKey(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);
            var activatorKey = new PropertyKey(idKey.format, 26);
            var idValue = new PropVariant { type = 31, pointer = Marshal.StringToCoTaskMemUni(appId) };
            LastStage = "SET_SHORTCUT_AUMID";
            try { store.SetValue(ref idKey, ref idValue); }
            finally { PropVariantClear(ref idValue); }
            var activatorValue = new PropVariant { type = 72, pointer = Marshal.AllocCoTaskMem(16) };
            Marshal.StructureToPtr(activator, activatorValue.pointer, false);
            LastStage = "SET_SHORTCUT_ACTIVATOR";
            try { store.SetValue(ref activatorKey, ref activatorValue); }
            finally { PropVariantClear(ref activatorValue); }
            LastStage = "COMMIT_SHORTCUT_PROPERTIES";
            store.Commit();
            LastStage = "SAVE_OWN_SHORTCUT";
            ((IPersistFile)linkObject).Save(ShortcutPath, true);
        }
        finally { Marshal.FinalReleaseComObject(linkObject); }
        SHChangeNotify(2, 5, ShortcutPath, IntPtr.Zero);
        AppState.Log("identity.json",new { registration="READY", shortcutExists=File.Exists(ShortcutPath) });
        LastStage = "READY";
    }

    internal static void RemoveOwnShortcut()
    {
        if (!File.Exists(ShortcutPath)) return;
        object obj = new ShellLinkObject();
        try
        {
            ((IPersistFile)obj).Load(ShortcutPath, 0);
            var target = new StringBuilder(1024);
            ((IShellLink)obj).GetPath(target, target.Capacity, IntPtr.Zero, 0);
            if (string.Equals(target.ToString(), Environment.ProcessPath, StringComparison.OrdinalIgnoreCase)) File.Delete(ShortcutPath);
        }
        finally { Marshal.FinalReleaseComObject(obj); }
    }

    [StructLayout(LayoutKind.Sequential)] internal struct PropertyKey
    {
        internal Guid format; internal uint id;
        internal PropertyKey(Guid f, uint n) { format = f; id = n; }
    }
    [StructLayout(LayoutKind.Explicit, Size = 24)] internal struct PropVariant
    {
        [FieldOffset(0)] internal ushort type;
        [FieldOffset(8)] internal IntPtr pointer;
    }
    [ComImport, Guid("00021401-0000-0000-C000-000000000046")] sealed class ShellLinkObject { }
    [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellLink
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int max, IntPtr data, uint flags);
        void GetIDList(out IntPtr value); void SetIDList(IntPtr value);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder value, int max);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string value);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder value, int max);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string value);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder value, int max);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string value);
        void GetHotkey(out short value); void SetHotkey(short value);
        void GetShowCmd(out int value); void SetShowCmd(int value);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder value, int max, out int icon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string value, int icon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string value, uint reserved);
        void Resolve(IntPtr window, uint flags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string value);
    }
    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPropertyStore
    {
        void GetCount(out uint count); void GetAt(uint index, out PropertyKey key);
        void GetValue(ref PropertyKey key, out PropVariant value); void SetValue(ref PropertyKey key, ref PropVariant value);
        void Commit();
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
    [DllImport("ole32.dll")] static extern int PropVariantClear(ref PropVariant value);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] static extern void SHChangeNotify(uint kind, uint flags, string path, IntPtr second);
}

