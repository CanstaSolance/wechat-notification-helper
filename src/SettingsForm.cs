namespace WeChatNotifier;

internal sealed class SettingsForm : Form
{
    readonly BackgroundHost host;
    readonly Label status=new(){AutoSize=true,MaximumSize=new Size(510,0),AccessibleName="运行状态"};
    readonly CheckBox consent=new(){Text="我已了解并同意启用本工具",AutoSize=true};
    readonly CheckBox enabled=new(){Text="启用提醒",AutoSize=true};
    readonly CheckBox startup=new(){Text="登录 Windows 后自动运行",AutoSize=true};
    readonly TextBox selected=new(){ReadOnly=true,Dock=DockStyle.Fill,AccessibleName="微信程序位置"};
    readonly TextBox shortcut=new(){Dock=DockStyle.Fill,PlaceholderText="以微信“显示/隐藏窗口”设置为准",AccessibleName="微信唤回快捷键"};
    readonly CheckBox confirmed=new(){Text="我已在微信设置中核对上述快捷键",AutoSize=true};
    readonly CheckBox pauseShortcut=new(){Text="暂停通知点击唤回（提醒继续）",AutoSize=true};
    readonly Button save=new(){Text="保存并应用",AutoSize=true};
    readonly System.Windows.Forms.Timer refresh=new(){Interval=1000};
    bool saving;
    string? selectedPath;
    internal SettingsForm(BackgroundHost host)
    {
        this.host=host;Text="微信提醒助手 · 设置";Font=new Font("Microsoft YaHei UI",9);
        AutoScaleMode=AutoScaleMode.Dpi;StartPosition=FormStartPosition.CenterScreen;
        ClientSize=new Size(560,680);MinimumSize=new Size(510,610);MaximizeBox=false;
        var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(20)};
        var table=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1};
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));scroll.Controls.Add(table);Controls.Add(scroll);
        void Add(Control control){control.Margin=new Padding(0,0,0,12);control.Anchor=AnchorStyles.Left|AnchorStyles.Right;table.Controls.Add(control);}
        Add(new Label{Text="微信提醒助手",Font=new Font(Font.FontFamily,17,FontStyle.Bold),AutoSize=true});
        Add(new Label{Text="独立工具 · 0.2.0-rc.2 · 默认无托盘图标",AutoSize=true,ForeColor=SystemColors.GrayText});
        Add(status);
        Add(new Label{Text="仅观察 Windows 公开的微信关注事件，不读取聊天、联系人或账号。不是逐消息通知，可能漏报或与微信原提醒重复。系统勿扰设置仍生效。点击通知仅尝试用你确认的快捷键唤回正在运行的微信，不负责启动或登录。",AutoSize=true,MaximumSize=new Size(510,0)});
        var prefs=host.Preferences;consent.Checked=prefs.Consented;enabled.Checked=prefs.Enabled;startup.Checked=StartupManagement.Enabled;
        Add(consent);
        var options=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};options.Controls.AddRange([enabled,startup]);Add(options);
        Add(new Label{Text="微信安装位置（自动识别失败时选择）",AutoSize=true});
        selectedPath=prefs.WeChatPath;selected.Text=selectedPath??OfficialApp.Current?.Path??"自动识别";
        var pathRow=new TableLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,ColumnCount=2};pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var browse=new Button{Text="选择…",AutoSize=true};pathRow.Controls.Add(selected);pathRow.Controls.Add(browse);Add(pathRow);
        browse.Click+=(_,_)=>
        {
            using var picker=new OpenFileDialog{Title="选择官方微信程序",Filter="微信程序|Weixin.exe;WeChat.exe",CheckFileExists=true};
            if(picker.ShowDialog(this)==DialogResult.OK){selectedPath=picker.FileName;selected.Text=selectedPath;confirmed.Checked=false;}
        };
        Add(new Label{Text="通知点击唤回 · Ctrl+Alt+字母",AutoSize=true});
        var binding=ConfiguredShortcutActivation.Read();shortcut.Text=binding?.Shortcut??"";pauseShortcut.Checked=binding?.Paused??false;
        confirmed.Checked=binding!=null&&OfficialApp.Current is {} current&&binding.ImageHash==current.Hash&&binding.ImageVersion==current.Version;
        shortcut.TextChanged+=(_,_)=>confirmed.Checked=false;
        Add(shortcut);Add(confirmed);Add(pauseShortcut);
        Add(new Label{Text="留空时仍可接收提醒。微信更新或快捷键更改后需重新核验；连续失败会自动暂停。不会注册全局快捷键或展开隐藏托盘。",AutoSize=true,MaximumSize=new Size(510,0),ForeColor=SystemColors.GrayText});
        var actions=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,WrapContents=true,Padding=new Padding(20,8,20,12)};
        var test=new Button{Text="发送测试通知",AutoSize=true};var quit=new Button{Text="退出助手",AutoSize=true};var uninstall=new Button{Text="卸载…",AutoSize=true};
        actions.Controls.AddRange([save,test,quit,uninstall]);
        Controls.Remove(scroll);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty,Padding=Padding.Empty};
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(scroll,0,0);layout.Controls.Add(actions,0,1);Controls.Add(layout);AcceptButton=save;
        save.Click+=async(_,_)=>
        {
            if(saving)return;
            if(!consent.Checked){MessageBox.Show(this,"请先了解并同意工具的行为；也可以直接退出。","尚未启用");return;}
            if(shortcut.Text.Trim().Length>0&&!confirmed.Checked){MessageBox.Show(this,"请在微信设置中核对组合键后勾选确认，或将快捷键留空。","需要核对快捷键");return;}
            saving=true;save.Enabled=false;
            try
            {
                await host.SaveSettings(enabled.Checked,startup.Checked,selectedPath);
                if(IsDisposed)return;
                if(shortcut.Text.Trim().Length>0)
                {
                    if(AppState.IncidentBlocked)throw new InvalidOperationException("本机保留事故保护，未启用快捷键唤回。");
                    if(!await ConfiguredShortcutActivation.Confirm(shortcut.Text.Trim(),pauseShortcut.Checked))throw new InvalidOperationException("快捷键格式或官方微信签名核验失败，未保存唤回配置。");
                }
                else
                {
                    var old=ConfiguredShortcutActivation.Read();if(old!=null)ConfiguredShortcutActivation.Write(old with{Paused=true});
                }
                if(!IsDisposed)MessageBox.Show(this,"设置已保存。下次点击通知才执行唤回核验。关闭此窗口后助手继续在后台运行。","已保存");
            }
            catch(Exception ex){if(!IsDisposed)MessageBox.Show(this,ex.Message,"未能完成设置");}
            finally{saving=false;if(!IsDisposed)save.Enabled=true;}
        };
        test.Click+=(_,_)=>MessageBox.Show(this,host.Test().Reason,"测试通知");
        quit.Click+=(_,_)=>host.Quit();
        uninstall.Click+=(_,_)=>
        {
            if(MessageBox.Show(this,"将移除本工具的通知注册、自启、配置和已安装文件，不修改微信。确定卸载？","卸载微信提醒助手",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK)return;
            try{host.Uninstall();}catch(Exception ex){MessageBox.Show(this,ex.Message,"无法卸载");}
        };
        refresh.Tick+=(_,_)=>RefreshStatus();refresh.Start();RefreshStatus();
    }
    internal void RefreshStatus(){if(!IsDisposed){status.Text=host.StatusText;status.AccessibleName="运行状态："+status.Text;}}
    protected override void Dispose(bool disposing){if(disposing)refresh.Dispose();base.Dispose(disposing);}
}
