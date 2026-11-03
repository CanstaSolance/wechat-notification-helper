using System.Globalization;
namespace WeChatNotifier;
internal static class UiText
{
 internal sealed record Preference(string Language);
 static readonly Dictionary<string,string> english=Load();
 internal static string Language {get;private set;}=CultureInfo.CurrentUICulture.TwoLetterISOLanguageName=="zh"?"zh":"en";
 internal static Action<string>? Persist {get;set;}
 static Dictionary<string,string> Load(){var map=new Dictionary<string,string>();using var stream=typeof(UiText).Assembly.GetManifestResourceStream("WeChatNotifier.Strings.en.txt")!;using var reader=new StreamReader(stream);while(reader.ReadLine() is {} line){int i=line.IndexOf('|');if(i>0)map[line[..i]]=line[(i+1)..];}map["微信程序|Weixin.exe;WeChat.exe"]="WeChat program|Weixin.exe;WeChat.exe";return map;}
 internal static string T(string text)=>text=="0.3.0-rc.2 · 预发布"?(Language=="en"?"v0.3.0 · Preview":"v0.3.0 · 预发布"):Language=="en"&&english.TryGetValue(text,out var en)?en:text;
 internal static string Resolve(string? saved,bool consented,string system)=>saved is "en" or "zh"?saved:consented?"zh":system.StartsWith("zh",StringComparison.OrdinalIgnoreCase)?"zh":"en";
 internal static void Set(string language){string next=language=="en"?"en":"zh";Persist?.Invoke(next);Language=next;}
 internal static string ConvertText(string text){if(Language=="en")return T(text);var entry=english.FirstOrDefault(p=>p.Value==text);return entry.Key??text;}
}
