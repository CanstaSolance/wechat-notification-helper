namespace WeChatNotifier;

// Display-only boundary. The preview host cannot access the notification engine or user state.
internal enum CenterPhase { Setup, Checking, Active, Waiting, Paused, Attention }
internal sealed record CenterState(CenterPhase Phase, string? WeChatVersion, bool Experimental, bool ShortcutReady, bool Protected, string TechnicalDetails);
internal sealed record CenterValues(bool Consented, bool Enabled, bool Startup, string? Path, string Shortcut, bool Confirmed, bool PauseShortcut);
internal sealed record CenterDraft(bool Enabled, bool Startup, string? Path, string Shortcut, bool Confirmed, bool PauseShortcut);
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
    internal static CenterPhase Phase(bool consented,bool checking,bool enabled,bool ready,string source,int boundCount)=>
        !consented?CenterPhase.Setup:checking?CenterPhase.Checking:!enabled?CenterPhase.Paused:!ready?CenterPhase.Attention:source=="active"&&boundCount>0?CenterPhase.Active:CenterPhase.Waiting;
    internal static (string Title, string Description, string Badge) Status(CenterPhase phase) => phase switch
    {
        CenterPhase.Active => ("正在提醒", "微信发出新提醒时，会通过 Windows 通知你。", "已就绪"),
        CenterPhase.Waiting => ("等待微信", "请自行打开微信。助手会在后台等候，不会替你启动或登录。", "等候中"),
        CenterPhase.Paused => ("提醒已暂停", "暂时不接收新的提醒。你可以随时恢复。", "已暂停"),
        CenterPhase.Checking => ("正在检查微信", "正在确认安装位置与官方签名，请稍候。", "检查中"),
        CenterPhase.Attention => ("需要检查设置", "提醒暂时没有准备好。请重新保存设置，或查看下方帮助。", "需处理"),
        _ => ("先设置，再开始提醒", "了解助手如何工作，确认设置后，即可在后台等候提醒。", "尚未启用")
    };
    internal static string FriendlyFailure(Exception exception) => exception switch
    {
        UnauthorizedAccessException => "Windows 没有允许保存。请确认当前用户可以访问助手安装目录，再重试。",
        IOException => "设置或安装文件暂时无法访问。请确认文件没有被移动、占用或改动后重试。",
        _ => "这一步没有完成。请稍后重试；若仍失败，可展开技术信息并参考帮助反馈。"
    };
}
