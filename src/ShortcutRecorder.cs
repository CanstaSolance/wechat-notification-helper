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
internal sealed class ShortcutRecorder : RoundButton
{
    internal readonly ShortcutEntryState Entry=new();
    readonly System.Windows.Forms.Timer timeout=new(){Interval=12000};
    internal event Action? Edited;
    internal event Action? HintChanged;
    internal ShortcutRecorder(){AutoSize=true;MinimumSize=new Size(230,42);AccessibleName=UiText.T("录入微信现有快捷键");timeout.Tick+=(_,_)=>CancelCapture();UpdateText();}
    internal void SetValue(string value){timeout.Stop();Entry.Set(value);UpdateText();}
    internal void CancelCapture(){timeout.Stop();if(!Entry.Recording)return;Entry.Cancel();UpdateText();HintChanged?.Invoke();}
    void UpdateText(){Text=Entry.Recording?UiText.T("请按快捷键…"):Entry.Value.Length==0?UiText.T("点此录入快捷键"):Entry.Value.Replace("+","  +  ");AccessibleDescription=Entry.Hint;Invalidate();}
    protected override void OnClick(EventArgs e){base.OnClick(e);Entry.Begin();timeout.Start();UpdateText();HintChanged?.Invoke();}
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
    {
        if(!Entry.Recording)return base.ProcessCmdKey(ref msg,keyData);
        if((keyData&Keys.KeyCode)==Keys.Tab){CancelCapture();return base.ProcessCmdKey(ref msg,keyData);}
        Entry.Down(keyData);if(!Entry.Recording)timeout.Stop();UpdateText();HintChanged?.Invoke();return true;
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if(!Entry.Recording){base.OnKeyDown(e);return;}
        Entry.Down(e.KeyData);e.Handled=true;UpdateText();HintChanged?.Invoke();
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        bool wasRecording=Entry.Recording;
        if(Entry.Up(e.KeyData)){timeout.Stop();UpdateText();Edited?.Invoke();HintChanged?.Invoke();}
        if(!wasRecording)base.OnKeyUp(e);
    }
    protected override void DrawContent(Graphics g){if(Entry.Recording||Entry.Value.Length==0){base.DrawContent(g);return;}var keys=Entry.Value.Split('+');var widths=keys.Select(k=>TextRenderer.MeasureText(k,Font).Width+16).ToArray();int x=(Width-widths.Sum()-16)/2;for(int i=0;i<keys.Length;i++){var r=new Rectangle(x,(Height-30)/2,widths[i],30);using var fill=new SolidBrush(CenterTheme.Tint);g.FillRectangle(fill,r);g.DrawRectangle(SystemPens.ControlDark,r);TextRenderer.DrawText(g,keys[i],Font,r,CenterTheme.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);x+=widths[i]+8;}}
    protected override void Dispose(bool d){if(d)timeout.Dispose();base.Dispose(d);}
    protected override void OnLostFocus(EventArgs e){CancelCapture();base.OnLostFocus(e);}
}
