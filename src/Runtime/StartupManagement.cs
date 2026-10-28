using Microsoft.Win32;

namespace WeChatNotifier;

internal static class StartupManagement
{
    const string Key=@"Software\Microsoft\Windows\CurrentVersion\Run";
    internal static string Command=>"\""+Environment.ProcessPath+"\" --background";
    internal static bool Enabled
    {get{using var key=Registry.CurrentUser.OpenSubKey(Key);return key?.GetValue(AppState.Product) as string==Command;}}
    internal static void Set(bool enabled)
    {
        using var key=Registry.CurrentUser.CreateSubKey(Key);
        string? existing=key.GetValue(AppState.Product) as string;
        if(existing!=null&&existing!=Command)throw new IOException("存在另一位置的同名启动项，请先从原位置卸载或修复安装。");
        if(enabled)key.SetValue(AppState.Product,Command);else key.DeleteValue(AppState.Product,false);
        if(Enabled!=enabled)throw new IOException("启动项保存后核验失败。");
    }
}
