using System.Diagnostics;
using System.Text.Json;
using CommunityToolkit.WinUI.Notifications;
using WeChatNotifier.Core;

namespace WeChatNotifier;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        try{return Run(args);}catch(Exception ex){AppState.Log("fatal-error.json",new{type=ex.GetType().Name});return 1;}
    }
    static int Run(string[] args)
    {
        if(!OperatingSystem.IsWindowsVersionAtLeast(10,0,19041)||System.Runtime.InteropServices.RuntimeInformation.OSArchitecture!=System.Runtime.InteropServices.Architecture.X64)return 3;
        string command=args.Length==0?"--settings":args.Length==1?args[0]:"";
        if(command=="--version"){Console.WriteLine("0.2.0-rc.1 win-x64 framework-dependent .NET Desktop 10");return 0;}
        if(command is not ("--settings" or "--background" or "--desktop-settings" or "--desktop-background" or "--status" or "--quit" or "--pause" or "--resume" or "--test-notification" or "--unregister" or "-ToastActivated"))return 2;
        if(!AppState.IsNativeUserContext())
        {
            if(command.StartsWith("--desktop",StringComparison.Ordinal)||command=="-ToastActivated")return 5;
            if(command is "--settings" or "--background")return LaunchFromDesktop(command=="--background"?"--desktop-background":"--desktop-settings");
            // IPC-only commands do not read/write configuration and can safely forward.
            if(command is not ("--status" or "--quit" or "--pause" or "--resume" or "--test-notification"))return 5;
        }
        command=command.Replace("--desktop-","--",StringComparison.Ordinal);
        if(command=="--unregister")
        {
            using var owned=new SingleInstance(AppState.InstanceName);
            if(!owned.Acquired)return 4;
            try{StartupManagement.Set(false);NotificationIdentity.UninstallOwn();return 0;}
            catch(Exception ex){AppState.Log("uninstall-error.json",new{type=ex.GetType().Name});return 1;}
        }
        using var instance=new SingleInstance(AppState.InstanceName);
        if(!instance.Acquired)
        {
            if(command=="-ToastActivated")return ColdToast(false);
            string action=command switch{"--background"=>"status","--test-notification"=>"test",_=>command[2..]};
            string? reply=LocalControl.Send(action).GetAwaiter().GetResult();Console.WriteLine(reply);return reply==null?4:0;
        }
        if(command=="-ToastActivated")return ColdToast(true);
        if(command is "--status" or "--quit" or "--pause" or "--resume" or "--test-notification"){Console.WriteLine("NOT_RUNNING");return command=="--quit"?0:4;}
        if(command=="--background"&&!AppState.Load().Consented)return 4;
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();
        try
        {
            using var host=new BackgroundHost();
            Application.Run(host);return 0;
        }
        catch(Exception ex){AppState.Log("fatal-error.json",new{type=ex.GetType().Name});return 1;}
    }
    internal static bool TrySubmission(string arguments,out string submission)
    {
        const string prefix="action=open-wechat&submission=";
        submission=arguments.StartsWith(prefix,StringComparison.Ordinal)?arguments[prefix.Length..]:"";
        return Guid.TryParseExact(submission,"N",out _);
    }
    static int ColdToast(bool ownsInstance)
    {
        if(!AppState.Load().Consented)return 4;
        // A bounded COM forwarding process is transient, never a second monitoring host.
        Application.EnableVisualStyles();
        using var dispatcher=new Control();_=dispatcher.Handle;
        using var context=new ApplicationContext();
        int seen=0;
        OnActivated callback=activation=>
        {
            if(!TrySubmission(activation.Argument,out string submission)||Interlocked.Exchange(ref seen,1)!=0)return;
            dispatcher.BeginInvoke(async()=>
            {
                try
                {
                    // No arbitrary payload crosses IPC. Cold forwarding uses a one-use local receipt.
                    AppState.Log("cold-callback.json",new{submission,code="COLD_CALLBACK_RECEIVED"});
                    if(ownsInstance)await ApplicationActivation.Click(submission);
                    else await LocalControl.Send("activate/"+submission); // No direct fallback after an uncertain forward.
                }
                finally{context.ExitThread();}
            });
        };
        // If the resident exists it owns hot callbacks; Toolkit forwards through its COM registration.
        ToastNotificationManagerCompat.OnActivated+=callback;
        try
        {
            OfficialApp.Detect(AppState.Load().WeChatPath);
            NotificationIdentity.VerifyExisting();
            using var timer=new System.Windows.Forms.Timer{Interval=12000};timer.Tick+=(_,_)=>context.ExitThread();timer.Start();
            _=ToastNotificationManagerCompat.WasCurrentProcessToastActivated();Application.Run(context);return seen==1?0:4;
        }
        finally{ToastNotificationManagerCompat.OnActivated-=callback;}
    }
    static int LaunchFromDesktop(string arguments)
    {
        try
        {
            dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application")!)!;
            object location=null!,root=null!;int hwnd=0;
            dynamic desktop=shell.Windows().FindWindowSW(ref location,ref root,8,ref hwnd,1);
            if(desktop==null||hwnd==0)return 5;
            desktop!.Document.Application.ShellExecute(Environment.ProcessPath!,arguments,AppContext.BaseDirectory,"open",arguments=="--desktop-settings"?1:0);return 0;
        }
        catch{return 5;}
    }
}

internal sealed class BackgroundHost : ApplicationContext
{
    readonly Control dispatcher=new();
    readonly LocalControl control;
    readonly System.Windows.Forms.Timer discovery=new(){Interval=30000};
    readonly Preferences initial=AppState.Load();
    Preferences preferences;
    SettingsForm? settings;
    NotificationCore? core;
    PublicAttentionSource? source;
    WindowsNoticeSink? sink;
    bool detecting,closing,activationSubscribed;
    string status="尚未启用";
    CoreState? lastCore;
    internal BackgroundHost()
    {
        preferences=initial;_=dispatcher.Handle;_=ConfiguredShortcutActivation.Read();
        control=new LocalControl(OnCommand);control.Start();
        discovery.Tick+=async(_,_)=>{if(preferences.Consented&&!OfficialApp.CurrentDiskMatches)await DetectAndStart();};discovery.Start();
        dispatcher.BeginInvoke(async()=>
        {
            if(preferences.Consented)await DetectAndStart();
            if(Environment.GetCommandLineArgs().Any(a=>a is "--settings" or "--desktop-settings")||Environment.GetCommandLineArgs().Length==1)OpenSettings();
            WriteStatus();
        });
    }
    void Activated(ToastNotificationActivatedEventArgsCompat activation)
    {
        if(!Program.TrySubmission(activation.Argument,out string submission))return;
        dispatcher.BeginInvoke(async()=>{AppState.Log("callback.json",new{submission,origin="hot-host"});await ApplicationActivation.Click(submission);WriteStatus();});
    }
    internal async Task DetectAndStart()
    {
        if(detecting||closing)return;detecting=true;
        try
        {
            status="正在核验微信安装与签名…";RefreshSettings();
            var image=await Task.Run(()=>OfficialApp.Detect(preferences.WeChatPath));
            if(closing)return;
            if(image==null){status="未找到唯一可信微信安装；请在设置中选择官方程序。";return;}
            if(!activationSubscribed){ToastNotificationManagerCompat.OnActivated+=Activated;activationSubscribed=true;}
            core?.Dispose();core=null;source=null;sink=null;
            source=new PublicAttentionSource(Post,true);sink=new WindowsNoticeSink();sink.Initialize();
            core=new NotificationCore(source,sink,new MonotonicClock(),new UnavailableActivation());
            core.Changed+=state=>{lastCore=state;RefreshSettings();};
            core.Start(preferences.Enabled);
            status=OfficialApp.IsVerifiedPublicApiVersion(image.Version)?"公开关注事件提醒已就绪":"当前微信版本未实测：仅尝试公开关注事件，可能没有提醒。";
        }
        catch(Exception ex)
        {
            core?.Dispose();core=null;source?.Dispose();source=null;sink?.Dispose();sink=null;
            status="通知初始化未完成（"+NotificationIdentity.LastStage+"）。请重新保存设置；如仍失败，请提供阶段代码。";
            AppState.Log("startup-error.json",new{type=ex.GetType().Name,stage=NotificationIdentity.LastStage,hresult=ex.HResult,message=ex.Message});
        }
        finally{detecting=false;WriteStatus();RefreshSettings();}
    }
    void Post(Action action){if(!closing)try{dispatcher.BeginInvoke(action);}catch(InvalidOperationException){}}
    Task<string> OnCommand(string command)
    {
        var done=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(async()=>
        {
            try
            {
                if(command=="settings")OpenSettings();
                if(command is "pause" or "resume"){if(!preferences.Consented)throw new InvalidOperationException("尚未同意启用");SetEnabled(command=="resume");}
                if(command=="test"){if(!preferences.Consented||sink==null)throw new InvalidOperationException("尚未就绪");sink.Test();}
                if(command.StartsWith("activate/",StringComparison.Ordinal)&&preferences.Consented)await ApplicationActivation.Click(command[9..]);
                string reply=JsonSerializer.Serialize(Snapshot());done.TrySetResult(reply);
                if(command=="shutdown")ExitThread();
            }
            catch(Exception ex){done.TrySetResult(JsonSerializer.Serialize(new{error=ex.GetType().Name}));}
        });return done.Task;
    }
    internal object Snapshot()=>new{product=AppState.Product,version="0.2.0-rc.1",pid=Environment.ProcessId,
        consented=preferences.Consented,enabled=preferences.Enabled,sourcePhase=source?.Phase??"not-started",boundCount=source?.BoundCount??0,
        status,core=lastCore,shortcut=ConfiguredShortcutActivation.LastReadCode,lastActivation=ApplicationActivation.LastCode,
        incidentProtected=AppState.IncidentBlocked,startupEnabled=StartupManagement.Enabled,notification=sink?.LastDelivery,stateDirectory=AppState.Root,noTrayIcon=true};
    internal string StatusText=>status+"\n"+(!preferences.Consented?"尚未同意启用，监听未启动":preferences.Enabled?"提醒已启用":"提醒已暂停")+"；已绑定进程："+(source?.BoundCount??0)+
        "\n微信版本："+(OfficialApp.Current?.Version??"未识别")+"\n最近唤回："+ApplicationActivation.LastCode+
        (AppState.IncidentBlocked?"\n检测到保留的事故保护，自动唤回已禁用。":"");
    internal Preferences Preferences=>preferences;
    internal void OpenSettings()
    {
        if(settings is {IsDisposed:false}){ShowOwnSettings();return;}
        settings=new SettingsForm(this);
        settings.FormClosed+=(_,_)=>{settings=null;if(!preferences.Consented)ExitThread();};ShowOwnSettings();
    }
    void ShowOwnSettings()
    {
        if(settings==null)return;
        settings.Show();
        // A hidden background launch can apply SW_HIDE to the first ordinary Show.
        // This second show targets only this process's own settings HWND.
        _=ShowWindow(settings.Handle,1);settings.Activate();
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool ShowWindow(nint ownSettings,int command);
    internal async Task SaveSettings(bool enabled,bool autoStart,string? selected)
    {
        preferences=new(1,true,enabled,selected);AppState.Save("preferences.json",preferences);
        if(AppState.Load()!=preferences)throw new IOException("配置保存后读回失败");
        StartupManagement.Set(autoStart);await DetectAndStart();
    }
    internal void SetEnabled(bool enabled)
    {preferences=preferences with{Enabled=enabled};AppState.Save("preferences.json",preferences);core?.SetEnabled(enabled);WriteStatus();RefreshSettings();}
    internal Delivery Test()=>sink?.Test()??new(false,"请先启用助手并核验微信安装");
    internal void Quit()=>ExitThread();
    internal void Uninstall()
    {
        string script=Path.Combine(AppContext.BaseDirectory,"Uninstall.ps1");
        if(!File.Exists(script))throw new IOException("卸载脚本缺失，请从完整发行包恢复。");
        var start=new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,@"WindowsPowerShell\v1.0\powershell.exe")){UseShellExecute=false,CreateNoWindow=true};
        foreach(string arg in new[]{"-NoProfile","-ExecutionPolicy","Bypass","-File",script,"-AppPath",Environment.ProcessPath!,"-Confirmed"})start.ArgumentList.Add(arg);
        Process.Start(start)?.Dispose();ExitThread();
    }
    void RefreshSettings(){settings?.RefreshStatus();}
    void WriteStatus()=>AppState.Log("status.json",Snapshot());
    protected override void ExitThreadCore()
    {
        if(closing)return;closing=true;discovery.Stop();control.Dispose();core?.Dispose();settings?.Close();WriteStatus();base.ExitThreadCore();
    }
    protected override void Dispose(bool disposing)
    {if(disposing){if(activationSubscribed)ToastNotificationManagerCompat.OnActivated-=Activated;discovery.Dispose();control.Dispose();core?.Dispose();dispatcher.Dispose();}base.Dispose(disposing);}
}
internal sealed class MonotonicClock : IClock
{readonly Stopwatch watch=Stopwatch.StartNew();public long Milliseconds=>watch.ElapsedMilliseconds;}
