using System.Diagnostics;
namespace WeChatNotifier;

internal sealed class SettingsForm : Form
{
    readonly IControlCenterHost host;
    readonly Panel content=new(){Dock=DockStyle.Fill,AutoScroll=true};
    readonly TableLayoutPanel[] pages=new TableLayoutPanel[3];
    readonly Button[] navigation=new Button[3];
    readonly WrapLabel heading=new("概览",22,true),subtitle=new("简单一点，及时知道微信有动静。",9.5f,false,true);
    readonly WrapLabel statusTitle=new("",24,true),statusDescription=new(""),statusBadge=new("",9,true);
    readonly WrapLabel alert=new(""),shortcutSummary=new(""),versionSummary=new(""),feedback=new("");
    readonly TextBox details=new(){Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Top,Height=130,Visible=false,AccessibleName="技术信息，请勿直接公开原始配置"};
    readonly CheckBox consent=new(){Text="我已了解，并同意启用助手",AutoSize=true};
    readonly CheckBox enabled=new(){Text="显示系统提醒",AutoSize=true},startup=new(){Text="登录电脑后自动准备 (&A)",AutoSize=true};
    readonly TextBox selected=new(){ReadOnly=true,Dock=DockStyle.Top,AccessibleName="微信程序位置"};
    readonly TextBox shortcut=new(){Dock=DockStyle.Top,PlaceholderText="未填写：只接收提醒",AccessibleName="微信显示或隐藏窗口快捷键"};
    readonly CheckBox confirmed=new(){Text="我已在微信中核对，这就是当前的快捷键",AutoSize=true};
    readonly CheckBox pauseShortcut=new(){Text="暂时停用点击通知打开微信",AutoSize=true};
    readonly Button save=new(){Text="保存设置 (&S)"},cancel=new(){Text="还原未保存的改动 (&R)"},pause=new(){Text="暂停提醒"},test=new(){Text="测试提醒"};
    readonly FlowLayoutPanel saveActions=new(){AutoSize=true,Dock=DockStyle.Top,WrapContents=true,Margin=Padding.Empty};
    readonly System.Windows.Forms.Timer refresh=new(){Interval=1000};
    readonly Panel sidebar=new(){Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(22,28,18,20)};
    string? selectedPath;
    bool loading,saving,dirty;
    int pageIndex;
    internal SettingsForm(IControlCenterHost host)
    {
        this.host=host;Text=host.IsPreview?"微信提醒助手 · 界面预览":"微信提醒助手";
        Font=new Font("Microsoft YaHei UI",9.5f);AutoScaleMode=AutoScaleMode.Dpi;
        StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(1000,850);MinimumSize=new Size(860,650);
        BackColor=CenterTheme.Canvas;ForeColor=CenterTheme.Text;
        using(var icon=typeof(SettingsForm).Assembly.GetManifestResourceStream("WeChatNotifier.Assets.helper.ico"))if(icon!=null)Icon=new Icon(icon);
        var shell=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
        shell.RowStyles.Add(new RowStyle(SizeType.Percent,100));shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,206));shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        shell.Controls.Add(sidebar,0,0);
        var main=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(28,24,28,0),Margin=Padding.Empty};
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));main.RowStyles.Add(new RowStyle(SizeType.Percent,100));main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.Controls.Add(main,1,0);Controls.Add(shell);
        var top=Stack();top.Padding=new Padding(0,0,0,10);top.Controls.Add(heading);top.Controls.Add(subtitle);main.Controls.Add(top,0,0);
        main.Controls.Add(content,0,1);
        var footer=Stack();footer.Padding=new Padding(0,12,0,14);
        feedback.Visible=false;feedback.AccessibleRole=AccessibleRole.Alert;footer.Controls.Add(feedback);
        CenterTheme.Button(save,true);CenterTheme.Button(cancel);saveActions.Controls.AddRange([save,cancel]);footer.Controls.Add(saveActions);
        footer.Controls.Add(new WrapLabel("关闭窗口后仍在后台运行。再次打开助手即可回到这里；退出请前往“隐私与帮助”。",8.5f,false,true){Margin=new Padding(0,12,0,0)});
        main.Controls.Add(footer,0,2);
        BuildSidebar();for(int i=0;i<3;i++)pages[i]=Stack();BuildOverview();BuildSettings();BuildHelp();foreach(var page in pages){page.Visible=false;content.Controls.Add(page);}
        ReadValues();TrackChanges();ShowPage(0);RefreshStatus();
        save.Click+=async(_,_)=>await Save();
        cancel.Click+=(_,_)=>{ReadValues();Tell(new(true,"已还原为上次保存的设置。"));};
        pause.Click+=(_,_)=>{var r=host.SetEnabled(host.State.Phase==CenterPhase.Paused);if(r.Success){loading=true;enabled.Checked=host.Values.Enabled;loading=false;}Tell(r);RefreshStatus();};
        test.Click+=(_,_)=>Tell(host.Test());refresh.Tick+=(_,_)=>RefreshStatus();refresh.Start();
        Shown+=(_,_)=>{var area=Screen.FromControl(this).WorkingArea;MinimumSize=new Size(Math.Min(MinimumSize.Width,Math.Max(320,area.Width-32)),Math.Min(MinimumSize.Height,Math.Max(300,area.Height-32)));if(Width>area.Width||Height>area.Height)Bounds=new Rectangle(area.X+16,area.Y+16,Math.Min(Width,area.Width-32),Math.Min(Height,area.Height-32));};
        FormClosing+=(_,e)=>
        {
            if(saving&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Tell(new(false,"正在保存，请稍候再关闭。"));return;}
            if(dirty&&e.CloseReason==CloseReason.UserClosing&&MessageBox.Show(this,"有尚未保存的改动。关闭后将放弃这些改动。","放弃改动？",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK)e.Cancel=true;
        };
    }
    static TableLayoutPanel Stack()=>new(){AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Dock=DockStyle.Top,ColumnCount=1,ColumnStyles={new ColumnStyle(SizeType.Percent,100)},Margin=Padding.Empty};
    static FlowLayoutPanel Actions(params Button[] buttons)
    {
        var flow=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,WrapContents=true,Margin=new Padding(0,6,0,4)};
        foreach(var b in buttons){CenterTheme.Button(b);flow.Controls.Add(b);}return flow;
    }
    SurfaceCard Card(int page,string title,string? description=null)
    {
        var card=new SurfaceCard();pages[page].Controls.Add(card);card.Add(new WrapLabel(title,12,true));
        if(description!=null)card.Add(new WrapLabel(description,9.5f,false,true));return card;
    }
    void BuildSidebar()
    {
        sidebar.BackColor=CenterTheme.Surface;var brand=Stack();brand.Controls.Add(new BrandMark{Margin=new Padding(0,0,0,14)});
        brand.Controls.Add(new WrapLabel("微信提醒助手",13,true));
        brand.Controls.Add(new WrapLabel("少一点错过\n多一点从容",9,false,true){Margin=new Padding(0,0,0,30)});
        string[] labels=["概览","提醒设置","隐私与帮助"];
        for(int i=0;i<labels.Length;i++)
        {
            int index=i;var b=new Button{Text=labels[i],TextAlign=ContentAlignment.MiddleLeft,Dock=DockStyle.Top,AccessibleName=labels[i],Margin=new Padding(0,0,0,10)};
            CenterTheme.Button(b);b.Padding=new Padding(15,12,10,12);b.Click+=(_,_)=>ShowPage(index);navigation[i]=b;brand.Controls.Add(b);
        }
        brand.Controls.Add(new WrapLabel("独立工具 · 非微信官方\n0.3.0-rc.1  预发布",8.5f,false,true){Margin=new Padding(0,32,0,8)});
        if(host.IsPreview)brand.Controls.Add(new WrapLabel("界面预览\n未连接微信；操作仅在内存中演示。",8.5f,true));sidebar.Controls.Add(brand);
    }
    void BuildOverview()
    {
        var hero=new SurfaceCard();pages[0].Controls.Add(hero);statusBadge.ForeColor=CenterTheme.Accent;hero.Add(statusBadge);hero.Add(statusTitle);hero.Add(statusDescription);
        var row=Actions(test,pause);CenterTheme.Button(test,true);hero.Add(row);
        var essentials=Card(0,"按你的方式提醒");essentials.Add(shortcutSummary);essentials.Add(versionSummary);
        var settings=new Button{Text="调整提醒设置 →"};settings.Click+=(_,_)=>ShowPage(1);essentials.Add(Actions(settings));
        var limits=Card(0,"有提醒，也有边界");
        limits.Add(new WrapLabel("收到提醒时显示 Windows 通知，连续提醒可能合并，部分消息可能没有提示。微信正在前台、系统勿扰或通知被关闭时，也可能不弹出。"));
        limits.Add(new WrapLabel("通知不包含发件人和正文；点击后只尝试打开已运行的微信，不会直达某个聊天。",9.5f,false,true));alert.Visible=false;limits.Add(alert);
    }
    void BuildSettings()
    {
        var basic=Card(1,"日常提醒","保存后生效。你可以随时暂停，也可以只使用提醒、不设置快捷键。");
        basic.Add(enabled);basic.Add(new WrapLabel("微信发出新提醒时，通过 Windows 通知你。",9,false,true){Margin=new Padding(0,6,0,16)});
        basic.Add(startup);basic.Add(new WrapLabel("登录 Windows 后，助手静静在后台等候；不会替你启动或登录微信。",9,false,true){Margin=new Padding(0,6,0,8)});
        var activation=Card(1,"点击通知，打开微信","可选。需要你亲自核对微信里正在使用的快捷键。");
        activation.Add(new WrapLabel("在微信的“设置 → 快捷键”中找到“显示/隐藏窗口”，把实际组合键填到下方。没有这项或不确定时，留空即可。",9,false,true));
        shortcut.Margin=new Padding(0,0,0,12);activation.Add(shortcut);activation.Add(confirmed);pauseShortcut.Margin=new Padding(0,12,0,8);activation.Add(pauseShortcut);
        activation.Add(new WrapLabel("支持 Ctrl + Alt + 字母，也可加 Shift。微信更新或修改快捷键后，请重新核对；连续打开失败时会自动停用。",8.5f,false,true));
        var advanced=Card(1,"微信安装位置","通常会自动识别；只有找不到微信或装了多个版本时，才需要手动选择。");
        var pathBody=Stack();pathBody.Visible=false;selected.Text="自动识别";selected.Margin=new Padding(0,0,0,12);pathBody.Controls.Add(selected);
        var browse=new Button{Text="选择微信程序…"};var reset=new Button{Text="恢复自动识别"};
        browse.Click+=(_,_)=>{using var picker=new OpenFileDialog{Title="选择官方微信程序",Filter="微信程序|Weixin.exe;WeChat.exe",CheckFileExists=true};if(picker.ShowDialog(this)==DialogResult.OK){selectedPath=picker.FileName;selected.Text=selectedPath;confirmed.Checked=false;MarkDirty();}};
        reset.Click+=(_,_)=>{selectedPath=null;selected.Text="自动识别";confirmed.Checked=false;MarkDirty();};pathBody.Controls.Add(Actions(browse,reset));var expand=new Button{Text="查看或选择安装位置"};
        expand.Click+=(_,_)=>{pathBody.Visible=!pathBody.Visible;expand.Text=pathBody.Visible?"收起安装位置":"查看或选择安装位置";};advanced.Add(Actions(expand));advanced.Add(pathBody);
        var agreement=Card(1,"由你决定是否启用");
        agreement.Add(new WrapLabel("助手只观察微信发出的提醒，不读取聊天或账号。点击通知时，只有在你确认快捷键、微信身份检查通过后，才尝试发送一次组合键。",9,false,true));
        agreement.Add(consent);var privacy=new Button{Text="了解隐私与使用边界 →"};privacy.Click+=(_,_)=>ShowPage(2);agreement.Add(Actions(privacy));
    }
    void BuildHelp()
    {
        var privacy=Card(2,"你的聊天，仍然只在微信里","助手不读取聊天、联系人、聊天数据库、密码或登录凭据。");
        privacy.Add(new WrapLabel("它会检查公开的进程与窗口信息、微信安装位置和官方签名；本地保存你的设置、快捷键绑定及有限的运行状态。没有自建上传、遥测或更新服务器。"));
        var policy=new Button{Text="在线隐私说明 ↗"};policy.Click+=(_,_)=>OpenDocument("PRIVACY.md","https://github.com/CanstaSolance/wechat-notification-helper/blob/v0.3.0-rc.1/PRIVACY.md");privacy.Add(Actions(policy));
        var help=Card(2,"没有看到提醒？");
        help.Add(new WrapLabel("1  先在“概览”发送测试提醒，检查 Windows 通知中心。\n2  在 Windows 设置中允许“微信提醒助手”通知，并检查勿扰。\n3  确认已打开微信、助手没有暂停；微信在前台时通常不会额外提醒。\n4  点击通知没反应时，重新核对微信快捷键；不确定时请手动打开微信。"));
        help.Add(new WrapLabel("测试提醒只能检查助手到 Windows 的通知通道，不能证明每条微信消息都能收到。",9,false,true));
        var guide=new Button{Text="在线使用帮助 ↗"};var issues=new Button{Text="反馈问题 ↗"};guide.Click+=(_,_)=>OpenDocument("README.zh-CN.md","https://github.com/CanstaSolance/wechat-notification-helper/blob/v0.3.0-rc.1/README.zh-CN.md");
        issues.Click+=(_,_)=>OpenLink("https://github.com/CanstaSolance/wechat-notification-helper/issues");help.Add(Actions(guide,issues));
        help.Add(new WrapLabel("反馈请只提供系统版本、助手版本和操作步骤，不要上传聊天截图或原始配置。在线链接需联网，由浏览器访问。",8.5f,false,true));
        var support=Card(2,"支持范围与技术信息");support.Add(new WrapLabel("现有通知与打开微信行为仅在一台 Windows 11 x64、微信 4.1.15.56 上验证。其他官方签名版本属于实验性支持；本次界面更新不代表新增消息兼容性验证。",9,false,true));
        var diagnostic=new Button{Text="展开技术信息"};diagnostic.Click+=(_,_)=>{details.Visible=!details.Visible;diagnostic.Text=details.Visible?"收起技术信息":"展开技术信息";RefreshStatus();};support.Add(Actions(diagnostic));support.Add(details);
        var management=Card(2,"管理助手","没有托盘图标。关闭窗口会继续在后台运行；要完全停止，请退出助手。");
        var quit=new Button{Text="退出助手…"};var uninstall=new Button{Text="卸载助手…"};
        quit.Click+=(_,_)=>{if(MessageBox.Show(this,"退出后将停止提醒。下次打开助手即可重新运行。","退出微信提醒助手？",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)==DialogResult.OK){dirty=false;host.Quit();}};
        uninstall.Click+=(_,_)=>{if(MessageBox.Show(this,"将移除助手自己的安装、启动项和已知配置，不修改微信。安全保护记录及未知文件会保留。确定继续？","卸载微信提醒助手？",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK)return;try{dirty=false;host.Uninstall();}catch(Exception ex){Tell(new(false,CenterCopy.FriendlyFailure(ex)));}};management.Add(Actions(quit,uninstall));
    }
    void TrackChanges(){foreach(var c in new[]{consent,enabled,startup,confirmed,pauseShortcut})c.CheckedChanged+=(_,_)=>MarkDirty();shortcut.TextChanged+=(_,_)=>{if(!loading)confirmed.Checked=false;MarkDirty();};}
    void ReadValues(){loading=true;var v=host.Values;consent.Checked=v.Consented;consent.Enabled=!v.Consented;consent.Text=v.Consented?"已确认启用（可在概览暂停）":"我已了解，并同意启用助手";enabled.Checked=v.Enabled;startup.Checked=v.Startup;selectedPath=v.Path;selected.Text=v.Path??"自动识别";shortcut.Text=v.Shortcut;confirmed.Checked=v.Confirmed;pauseShortcut.Checked=v.PauseShortcut;loading=false;dirty=false;UpdateSave();}
    void MarkDirty(){if(loading)return;dirty=true;UpdateSave();}
    void UpdateSave(){save.Enabled=!saving;cancel.Enabled=dirty&&!saving;save.Text=saving?"正在保存…":dirty?"保存改动 (&S)":"保存设置 (&S)";}
    internal void ShowPage(int index)
    {
        pageIndex=index;content.SuspendLayout();foreach(var page in pages)page.Visible=false;pages[index].Visible=true;pages[index].BringToFront();content.AutoScrollPosition=Point.Empty;content.ResumeLayout();
        string[] titles=["概览","提醒设置","隐私与帮助"],subtitles=["简单一点，及时知道微信有动静。","按你的使用习惯，选择怎样提醒。","知道它做什么，也知道它的边界。"];
        heading.Text=titles[index];subtitle.Text=subtitles[index];saveActions.Visible=index==1;AcceptButton=index==1?save:null;
        for(int i=0;i<3;i++){navigation[i].BackColor=i==index?CenterTheme.Tint:CenterTheme.Surface;navigation[i].ForeColor=i==index?CenterTheme.Accent:CenterTheme.Text;navigation[i].AccessibleDescription=i==index?"当前页面":"打开"+titles[i];}
    }
    async Task Save()
    {
        if(saving)return;if(!consent.Checked){Tell(new(false,"请先了解助手行为并勾选同意；也可以关闭窗口，不启用助手。"));consent.Focus();return;}
        if(shortcut.Text.Trim().Length>0&&!confirmed.Checked){Tell(new(false,"请先亲自核对微信快捷键并勾选确认，或清空快捷键只使用提醒。"));shortcut.Focus();return;}
        saving=true;sidebar.Enabled=false;pages[1].Enabled=false;pages[2].Enabled=false;UpdateSave();
        try{var r=await host.Save(new(enabled.Checked,startup.Checked,selectedPath,shortcut.Text.Trim(),confirmed.Checked,pauseShortcut.Checked));if(IsDisposed)return;if(r.Success)ReadValues();else if(host.Values.Consented){loading=true;consent.Checked=true;consent.Enabled=false;consent.Text="已确认启用（可在概览暂停）";loading=false;}Tell(r);}
        finally{saving=false;if(!IsDisposed){sidebar.Enabled=true;pages[1].Enabled=true;pages[2].Enabled=true;UpdateSave();RefreshStatus();}}
    }
    void Tell(CenterResult r){feedback.Text=(r.Success?"✓  ":"提示：")+r.Message;feedback.ForeColor=r.Success?CenterTheme.Accent:CenterTheme.Text;feedback.Visible=true;feedback.AccessibleName=feedback.Text;}
    internal void RefreshStatus()
    {
        if(IsDisposed)return;var state=host.State;var copy=CenterCopy.Status(state.Phase);statusTitle.Text=copy.Title;statusDescription.Text=copy.Description;statusBadge.Text="●  "+copy.Badge;statusTitle.AccessibleName="运行状态："+copy.Title;
        pause.Text=state.Phase==CenterPhase.Paused?"恢复提醒":"暂停提醒";pause.Enabled=state.Phase is CenterPhase.Paused or CenterPhase.Active or CenterPhase.Waiting;test.Enabled=state.Phase is CenterPhase.Active or CenterPhase.Waiting or CenterPhase.Paused;
        shortcutSummary.Text=state.ShortcutReady?"点击通知  ·  已配置，尝试打开正在运行的微信":"点击通知  ·  尚未启用，收到提醒后请手动打开微信";
        versionSummary.Text=state.WeChatVersion is {} v?"微信版本  ·  "+v+(state.Experimental?"（实验性支持）":""):"微信版本  ·  尚未识别";
        alert.Text=state.Protected?"本机保留了安全保护，点击通知打开微信暂不可用。提醒功能可独立设置。":state.Experimental?"这个微信版本尚未实测，可能没有提醒。":"";alert.Visible=alert.Text.Length>0;
        if(details.Visible&&details.Text!=state.TechnicalDetails)details.Text=state.TechnicalDetails;
    }
    void OpenDocument(string filename,string fallback)=>OpenLink(fallback);
    void OpenLink(string link)
    {
        if(host.IsPreview){Tell(new(true,"界面预览不会打开外部链接。正式版会用浏览器打开对应版本的说明，需要联网。"));return;}
        try{Process.Start(new ProcessStartInfo(link){UseShellExecute=true})?.Dispose();}catch{Tell(new(false,"没有找到可打开此链接或文档的应用。请在发行包中查看随附说明。"));}
    }
    protected override void OnSystemColorsChanged(EventArgs e)
    {
        base.OnSystemColorsChanged(e);if(navigation.Any(b=>b==null)||pages.Any(p=>p==null))return;
        void Apply(Control parent){foreach(Control c in parent.Controls){c.BackColor=c is TextBox?SystemColors.Window:c is SurfaceCard||c==sidebar?CenterTheme.Surface:CenterTheme.Canvas;c.ForeColor=CenterTheme.Text;Apply(c);}}
        BackColor=CenterTheme.Canvas;Apply(this);for(int i=0;i<3;i++)if(pages[i]!=null)Apply(pages[i]);CenterTheme.Button(save,true);CenterTheme.Button(cancel);CenterTheme.Button(test,true);CenterTheme.Button(pause);ShowPage(pageIndex);
    }
    protected override void Dispose(bool disposing){if(disposing){refresh.Dispose();foreach(var page in pages)if(page!=null&&!page.IsDisposed)page.Dispose();}base.Dispose(disposing);}
}
