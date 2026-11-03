namespace WeChatNotifier;
// Normal focused-window events only. No hooks, registration, simulation or persistence.
internal sealed class ShortcutEntryState
{
    internal string Value {get;private set;}="";
    internal bool Recording {get;private set;}
    string previous="",pending="";
    internal string Hint {get;private set;}=UiText.T("使用微信现有的“显示/隐藏窗口”快捷键。");
    internal static string? Normalize(Keys key) =>
        (key&Keys.Modifiers)==(Keys.Control|Keys.Alt)&&(key&Keys.KeyCode)>=Keys.A&&(key&Keys.KeyCode)<=Keys.Z
        ?"Ctrl+Alt+"+(char)(key&Keys.KeyCode):null;
    internal void Set(string value){Value=value;Recording=false;pending="";}
    internal void Begin(){previous=Value;Recording=true;pending="";Hint=UiText.T("请按 Ctrl + Alt + 一个字母；Esc 取消。");}
    internal void Down(Keys key)
    {
        if(!Recording)return;
        if((key&Keys.KeyCode) is Keys.Escape or Keys.Tab){Cancel();return;}
        pending=Normalize(key)??"";
        Hint=pending.Length>0?UiText.T("松开字母键完成录入。"):UiText.T("仅支持 Ctrl + Alt + A–Z，不支持 Shift 或功能键。");
    }
    internal bool Up(Keys key)
    {
        if(!Recording||pending.Length==0|| (key&Keys.KeyCode)!=(Keys)pending[^1])return false;
        Value=pending;Recording=false;pending="";Hint=UiText.T("录入完成。请确认它与微信里的设置一致。");return true;
    }
    internal void Cancel(){if(Recording)Value=previous;Recording=false;pending="";Hint=UiText.T("已取消录入，原快捷键未改动。");}
}
