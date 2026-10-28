using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;

namespace WeChatNotifier;

internal sealed record OfficialImage(string Path,string Version,string Hash,string Name,long Length,DateTime LastWriteUtc);
internal static class OfficialApp
{
    internal static OfficialImage? Current {get;private set;}
    internal static bool CurrentDiskMatches
    {
        get{try{var image=Current;return image!=null&&new FileInfo(image.Path) is {} file&&file.Length==image.Length&&file.LastWriteTimeUtc==image.LastWriteUtc;}catch{return false;}}
    }
    internal static OfficialImage? Detect(string? selected=null)
    {
        var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var self=Process.GetCurrentProcess();
        if(selected!=null)paths.Add(selected);
        foreach(string name in new[]{"Weixin","WeChat"})
        {
            foreach(var process in Process.GetProcessesByName(name))
                using(process)try
                {
                    if(process.SessionId!=self.SessionId)continue;
                    string? path=PublicAttentionSource.ProcessPath(process.Id);if(path!=null)paths.Add(path);
                }catch{}
            foreach(var hive in new[]{Registry.CurrentUser,Registry.LocalMachine})
            {
                using var key=hive.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\"+name+".exe");
                if(key?.GetValue("") is string path)paths.Add(path.Trim('"'));
            }
            foreach(string root in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)})
                paths.Add(System.IO.Path.Combine(root,"Tencent",name,name+".exe"));
        }
        // A user-selected installation is authoritative only after the same signature check.
        if(selected!=null){Current=Verify(selected);return Current;}
        var images=paths.Select(Verify).Where(x=>x!=null).Cast<OfficialImage>().ToArray();
        Current=images.Length==1?images[0]:null;return Current;
    }
    internal static OfficialImage? Verify(string path)
    {
        try
        {
            path=System.IO.Path.GetFullPath(path);
            if(!new[]{"Weixin.exe","WeChat.exe"}.Contains(System.IO.Path.GetFileName(path),StringComparer.OrdinalIgnoreCase))return null;
            if(path.StartsWith(@"\\",StringComparison.Ordinal))return null;
            for(string? dir=System.IO.Path.GetDirectoryName(path);dir!=null;dir=System.IO.Path.GetDirectoryName(dir))
                if((File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0)return null;
            if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)return null;
            using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
            if(file.Length<=0||file.Length>512L*1024*1024||!TrustedPublisher(path))return null;
            return new(path,FileVersionInfo.GetVersionInfo(path).FileVersion??"unknown",Convert.ToHexString(SHA256.HashData(file)),System.IO.Path.GetFileNameWithoutExtension(path),file.Length,File.GetLastWriteTimeUtc(path));
        }
        catch{return null;}
    }
    internal static bool IsVerifiedPublicApiVersion(string version)=>version=="4.1.15.56";
    static bool TrustedPublisher(string path)
    {
        var file=new WinTrustFile{Size=(uint)Marshal.SizeOf<WinTrustFile>(),Path=path};
        nint pointer=Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustFile>());
        var data=new WinTrustData{Size=(uint)Marshal.SizeOf<WinTrustData>(),UiChoice=2,UnionChoice=1,File=pointer,ProviderFlags=0x1000,StateAction=1};
        Guid action=new("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");
        try
        {
            Marshal.StructureToPtr(file,pointer,false);
            if(WinVerifyTrust(new nint(-1),ref action,ref data)!=0)return false;
            nint provider=WTHelperProvDataFromStateData(data.State);
            nint signer=provider==0?0:WTHelperGetProvSignerFromChain(provider,0,false,0);
            nint chain=signer==0?0:WTHelperGetProvCertFromChain(signer,0);
            if(chain==0)return false;
            var certificate=Marshal.PtrToStructure<CertificateContext>(Marshal.PtrToStructure<ProviderCertificate>(chain).Certificate);
            if(certificate.Length==0||certificate.Length>65536||certificate.Encoded==0)return false;
            var encoded=new byte[certificate.Length];Marshal.Copy(certificate.Encoded,encoded,0,encoded.Length);
            using var cert=X509CertificateLoader.LoadCertificate(encoded);
            // Identity is constrained in addition to Windows Authenticode chain validation.
            string subject=cert.Subject;
            return subject.Contains("CN=Tencent Technology (Shenzhen) Company Limited",StringComparison.Ordinal)||
                subject.Contains("CN=Shenzhen Tencent Computer Systems Company Limited",StringComparison.Ordinal);
        }
        finally{data.StateAction=2;_=WinVerifyTrust(new nint(-1),ref action,ref data);Marshal.DestroyStructure<WinTrustFile>(pointer);Marshal.FreeHGlobal(pointer);}
    }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct WinTrustFile
    {internal uint Size;[MarshalAs(UnmanagedType.LPWStr)]internal string Path;internal nint Handle,KnownSubject;}
    [StructLayout(LayoutKind.Sequential)] struct WinTrustData
    {internal uint Size;internal nint Policy,Sip;internal uint UiChoice,Revocation,UnionChoice;internal nint File;internal uint StateAction;internal nint State;internal nint Url;internal uint ProviderFlags,UiContext;internal nint SignatureSettings;}
    [DllImport("wintrust.dll",ExactSpelling=true)]static extern int WinVerifyTrust(nint owner,ref Guid action,ref WinTrustData data);
    [StructLayout(LayoutKind.Sequential)]struct ProviderCertificate{internal uint Size;internal nint Certificate;}
    [StructLayout(LayoutKind.Sequential)]struct CertificateContext{internal uint Encoding;internal nint Encoded;internal uint Length;internal nint Info,Store;}
    [DllImport("wintrust.dll",ExactSpelling=true)]static extern nint WTHelperProvDataFromStateData(nint state);
    [DllImport("wintrust.dll",ExactSpelling=true)]static extern nint WTHelperGetProvSignerFromChain(nint provider,uint signer,[MarshalAs(UnmanagedType.Bool)]bool counter,uint index);
    [DllImport("wintrust.dll",ExactSpelling=true)]static extern nint WTHelperGetProvCertFromChain(nint signer,uint index);
}
