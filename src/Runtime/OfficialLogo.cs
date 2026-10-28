using System.Drawing.Imaging;
using System.Reflection;

namespace WeChatNotifier;

internal static class OfficialLogo
{
    internal static string Prepare()
    {
        string directory=Path.Combine(AppState.Root,"assets");Directory.CreateDirectory(directory);
        if((File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0)throw new IOException("图标目录不允许重定向");
        string fallback=Path.Combine(directory,"helper.png");
        using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("WeChatNotifier.Assets.helper.png")!)
        using(var output=new FileStream(fallback,FileMode.Create,FileAccess.Write,FileShare.Read))input.CopyTo(output);
        // No Tencent asset is bundled. Extraction occurs only from the verified local image.
        try
        {
            if(OfficialApp.Current is not {} image)return fallback;
            using var icon=Icon.ExtractAssociatedIcon(image.Path);
            if(icon==null)return fallback;
            using var bitmap=icon.ToBitmap();
            string logo=Path.Combine(directory,"wechat-"+image.Hash[..16]+".png");
            using var stream=new FileStream(logo,FileMode.Create,FileAccess.Write,FileShare.Read);
            bitmap.Save(stream,ImageFormat.Png);return logo;
        }
        catch{return fallback;}
    }
}
