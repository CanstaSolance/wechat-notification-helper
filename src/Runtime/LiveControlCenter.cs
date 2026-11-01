namespace WeChatNotifier;
internal sealed class LiveControlCenter(BackgroundHost host) : IControlCenterHost
{
    public bool IsPreview=>false;
    public CenterValues Values
    {
        get
        {
            var p=host.Preferences;var b=ConfiguredShortcutActivation.Read();var image=OfficialApp.Current;
            return new(p.Consented,p.Enabled,StartupManagement.Enabled,p.WeChatPath,b?.Shortcut??"",
                b!=null&&image!=null&&b.ImageHash==image.Hash&&b.ImageVersion==image.Version,b?.Paused??false);
        }
    }
    public CenterState State=>host.CenterState;
    public async Task<CenterResult> Save(CenterDraft draft)
    {
        if(draft.Shortcut.Length>0 && (!draft.Confirmed || !ConfiguredShortcutActivation.TryParse(draft.Shortcut,out _,out _,out _,out _)))
            return new(false,"请在微信的“设置 → 快捷键 → 显示/隐藏窗口”核对。当前只支持 Ctrl + Alt + 字母，可额外加 Shift；也可以留空，只接收提醒。");
        try
        {
            await host.SaveSettings(draft.Enabled,draft.Startup,draft.Path);
            if(draft.Shortcut.Length>0)
            {
                if(AppState.IncidentBlocked)return new(false,"提醒设置已保存；本机的安全保护阻止了通知点击打开微信。请保留保护，暂时手动打开微信。");
                if(!await ConfiguredShortcutActivation.Confirm(draft.Shortcut,draft.PauseShortcut))
                    return new(false,"提醒设置已保存；快捷键尚未保存。请确认选择的是官方微信，并重新核对快捷键。");
            }
            else
            {
                var old=ConfiguredShortcutActivation.Read();
                if(old!=null)ConfiguredShortcutActivation.Write(old with{Paused=true});
            }
            return State.Phase==CenterPhase.Attention
                ?new(false,"设置已保存，但提醒没有准备好。请确认官方微信位置，再重新保存；也可查看隐私与帮助。")
                :new(true,"设置已保存。关闭窗口后，助手会继续在后台运行。");
        }
        catch(Exception ex){return new(false,CenterCopy.FriendlyFailure(ex));}
    }
    public CenterResult SetEnabled(bool enabled)
    {
        if(!host.Preferences.Consented)return new(false,"请先到“提醒设置”了解并启用助手。");
        try{host.SetEnabled(enabled);return new(true,enabled?"已恢复提醒。":"已暂停提醒，助手仍在后台。");}
        catch(Exception ex){return new(false,CenterCopy.FriendlyFailure(ex));}
    }
    public CenterResult Test()
    {
        try
        {
            var r=host.Test();
            return new(r.Submitted,r.Submitted?"测试提醒已交给 Windows。若没有弹出，请查看通知中心，并检查勿扰及通知设置。":"测试提醒暂时无法显示。请先保存设置，并在 Windows 中允许“微信提醒助手”通知。");
        }
        catch(Exception ex){return new(false,CenterCopy.FriendlyFailure(ex));}
    }
    public void Quit()=>host.Quit();
    public void Uninstall()=>host.Uninstall();
}
