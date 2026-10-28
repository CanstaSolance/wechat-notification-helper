using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WeChatNotifier;

internal sealed record Preferences(int Schema=1,bool Consented=false,bool Enabled=true,string? WeChatPath=null);
internal static class AppState
{
    internal const string Product="WeChatNotify";
    internal static string Root=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),Product);
    static readonly Lazy<string> userKey=new(()=>{using var identity=WindowsIdentity.GetCurrent();return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity.User!.Value)))[..20];});
    static readonly int session=GetSession();
    static int GetSession(){using var process=Process.GetCurrentProcess();return process.SessionId;}
    internal static string UserKey=>userKey.Value;
    internal static string InstanceName=>@"Local\WeChatNotify."+UserKey;
    internal static string PipeName=>"WeChatNotify."+UserKey+"."+session;
    internal static Preferences Load()
    {
        var result=Read<Preferences>("preferences.json");
        return result is {Schema:1}?result:new();
    }
    internal static T? Read<T>(string name)
    {
        try
        {
            string path=Path.Combine(Root,name);
            if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)return default;
            using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
            if(file.Length>65536)return default;
            return JsonSerializer.Deserialize<T>(file);
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or JsonException){return default;}
    }
    internal static void Save<T>(string name,T value)
    {
        Directory.CreateDirectory(Root);
        if((File.GetAttributes(Root)&FileAttributes.ReparsePoint)!=0)throw new IOException("配置目录不允许重定向。");
        string path=Path.Combine(Root,name),temp=path+".tmp";
        foreach(string candidate in new[]{path,temp})
            if(File.Exists(candidate)&&(File.GetAttributes(candidate)&FileAttributes.ReparsePoint)!=0)throw new IOException("配置文件不允许重定向。");
        File.WriteAllText(temp,JsonSerializer.Serialize(value));File.Move(temp,path,true);
    }
    internal static void Log(string name,object data)
    {try{Save(name,new{utc=DateTimeOffset.UtcNow,data});}catch{}}
    internal sealed record LegacyIncident(bool blocked,string reason);
    internal static bool BlocksPublicShortcut(bool ownBlock,bool legacyExists,LegacyIncident? legacy)=>ownBlock||
        legacyExists&&(legacy==null||legacy.blocked&&legacy.reason!="STABILITY_INCIDENT_CHAT_NONINTERACTIVE_INPUT_MISSING");
    internal static bool IncidentBlocked
    {
        get
        {
            // The known legacy incident forbids old invasive window manipulation/probes,
            // which are absent from this product. It does not grant or revoke user consent
            // for the independently verified public shortcut capability.
            bool own=File.Exists(Path.Combine(Root,"incident-block.json"));
            string legacy=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WeChatNotifier","incident-block.json");
            bool exists=File.Exists(legacy);LegacyIncident? incident=null;
            if(exists)try
            {
                using var file=new FileStream(legacy,FileMode.Open,FileAccess.Read,FileShare.Read);
                if(file.Length<=4096&&(File.GetAttributes(legacy)&FileAttributes.ReparsePoint)==0)incident=JsonSerializer.Deserialize<LegacyIncident>(file);
            }catch{}
            return BlocksPublicShortcut(own,exists,incident);
        }
    }
    // A packaged parent can redirect file I/O even when the logical path looks normal.
    // Verify the actual opened handle before reading or creating user preferences.
    internal static bool IsNativeUserContext()
    {
        try
        {
            Directory.CreateDirectory(Root);
            string probe=Path.Combine(Root,"context-probe.tmp");
            using var file=new FileStream(probe,FileMode.Create,FileAccess.ReadWrite,FileShare.None,1,FileOptions.DeleteOnClose);
            var final=new StringBuilder(32768);
            uint length=GetFinalPathNameByHandle(file.SafeFileHandle,final,(uint)final.Capacity,0);
            return length>0&&length<final.Capacity&&string.Equals(final.ToString(),@"\\?\"+probe,StringComparison.OrdinalIgnoreCase);
        }
        catch{return false;}
    }
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
    static extern uint GetFinalPathNameByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle file,StringBuilder path,uint length,uint flags);
}
internal static class BoundedStatus
{
    internal static void Cleanup(bool? success,int error,bool destroyed)=>AppState.Log("source-cleanup.json",new{success,error,destroyed});
}
