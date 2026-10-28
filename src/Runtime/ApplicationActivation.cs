using System.Diagnostics;
using WeChatNotifier.Core;

namespace WeChatNotifier;

internal static class ApplicationActivation
{
    internal sealed record Result(string Code,bool Started=false,bool Visible=false,bool Foreground=false,
        string? OverflowCleanup=null,Dictionary<string,long>? Timings=null);
    static int busy;
    static long lastClick;
    internal static string LastCode {get;private set;}="尚未点击通知";
    internal static async Task Click(string submission)
    {
        if(!Guid.TryParseExact(submission,"N",out _)||Interlocked.CompareExchange(ref busy,1,0)!=0)return;
        var timer=Stopwatch.StartNew();
        try
        {
            if(lastClick!=0&&Environment.TickCount64-lastClick<2000)return;
            lastClick=Environment.TickCount64;
            var result=await Task.Run(Execute);
            LastCode=result.Code;
            AppState.Log("activation.json",new{submission,result,callbackProcessingMs=timer.ElapsedMilliseconds,health=ConfiguredShortcutActivation.Health});
        }
        catch(Exception ex){LastCode="ACTIVATION_"+ex.GetType().Name;AppState.Log("activation.json",new{submission,code=LastCode});}
        finally{Volatile.Write(ref busy,0);}
    }
    static Result Execute()
    {
        using var transaction=new SingleInstance(AppState.InstanceName+".Activation");
        if(!transaction.Acquired)return new("ACTIVATION_IN_PROGRESS");
        if(AppState.IncidentBlocked)return new("INCIDENT_PROTECTION_ACTIVE");
        if(OfficialApp.Current is not {} image)return new("OFFICIAL_IMAGE_UNAVAILABLE");
        using var self=Process.GetCurrentProcess();
        var leases=new List<VerifiedTargetLease>();bool uncertain=false;
        try
        {
            foreach(var process in Process.GetProcessesByName(image.Name))using(process)
            {
                if(process.SessionId!=self.SessionId)continue;
                var lease=VerifiedTargetLease.Open(process.Id,self.SessionId,image.Path,a=>a());
                if(lease==null||!lease.IsCurrentUser){lease?.Dispose();uncertain=true;continue;}
                leases.Add(lease);
            }
            if(uncertain)return new("PROCESS_IDENTITY_UNCERTAIN");
            // No zero-process launch is included in this release.
            if(leases.Count==0)return new("WECHAT_NOT_RUNNING_NO_LAUNCH");
            var binding=ConfiguredShortcutActivation.Read();
            if(binding==null)return new("SHORTCUT_"+ConfiguredShortcutActivation.LastReadCode);
            var result=ConfiguredShortcutActivation.TryActivate(leases,binding);
            ConfiguredShortcutActivation.RecordFailure(result,binding);return result;
        }
        finally{foreach(var lease in leases)lease.Dispose();}
    }
}
