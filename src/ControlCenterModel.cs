namespace WeChatNotifier;

// Display-only boundary. The preview host cannot access the notification engine or user state.
internal enum CenterPhase { Setup, Checking, Active, Waiting, Paused, Attention }
internal sealed record CenterState(CenterPhase Phase, string? WeChatVersion, bool Experimental, bool ShortcutReady, bool Protected, string TechnicalDetails);
internal sealed record CenterValues(bool Consented, bool Enabled, bool Startup, string? Path, string Shortcut, bool Confirmed, bool PauseShortcut, bool ShortcutChanged=false);
internal sealed record CenterDraft(bool Enabled, bool Startup, string? Path, string Shortcut, bool Confirmed, bool PauseShortcut, bool ShortcutChanged=false);
internal sealed record CenterResult(bool Success, string Message);
internal interface IControlCenterHost
{
    bool IsPreview { get; }
    CenterValues Values { get; }
    CenterState State { get; }
    Task<CenterResult> Save(CenterDraft draft);
    CenterResult SetEnabled(bool enabled);
    CenterResult Test();
    void Quit();
    void Uninstall();
}
internal static class CenterCopy
{
    internal static bool BindingEdited(CenterValues old,string shortcut,bool pause,bool confirmed,string? path)=>shortcut!=old.Shortcut||pause!=old.PauseShortcut||(confirmed&&!old.Confirmed)||path!=old.Path;
    internal static CenterPhase Phase(bool consented,bool checking,bool enabled,bool ready,string source,int boundCount)=>
        !consented?CenterPhase.Setup:checking?CenterPhase.Checking:!enabled?CenterPhase.Paused:!ready?CenterPhase.Attention:source=="active"&&boundCount>0?CenterPhase.Active:CenterPhase.Waiting;
    internal static (string Title, string Description, string Badge) Status(CenterPhase phase) => phase switch
    {
        CenterPhase.Active => (UiText.T("提醒已开启"), UiText.T("微信发出新提醒时，会通过 Windows 通知你。"), UiText.T("已就绪")),
        CenterPhase.Waiting => (UiText.T("等你打开微信"), UiText.T("微信运行后，助手会自动接收提醒。"), UiText.T("等候中")),
        CenterPhase.Paused => (UiText.T("提醒已暂停"), UiText.T("想继续时，点一下恢复。"), UiText.T("已暂停")),
        CenterPhase.Checking => (UiText.T("正在检查微信"), UiText.T("正在确认安装位置与官方签名，请稍候。"), UiText.T("检查中")),
        CenterPhase.Attention => (UiText.T("未能准备好提醒"), UiText.T("请到设置选择官方微信程序，然后保存重试。"), UiText.T("需处理")),
        _ => (UiText.T("开启微信提醒"), UiText.T("先到设置了解并启用助手，快捷键可以稍后设置。"), UiText.T("尚未启用"))
    };
    internal static string FriendlyFailure(Exception exception) => exception switch
    {
        UnauthorizedAccessException => UiText.T("Windows 没有允许保存。请确认当前用户可以访问助手安装目录，再重试。"),
        IOException => UiText.T("设置或安装文件暂时无法访问。请确认文件没有被移动、占用或改动后重试。"),
        _ => UiText.T("这一步没有完成。请稍后重试；若仍失败，可展开技术信息并参考帮助反馈。")
    };
}
