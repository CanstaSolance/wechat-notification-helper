using System.Text.Json;
using WeChatNotifier;
using WeChatNotifier.Core;

if(args.SequenceEqual(new[]{"--local-signature"}))
{
    var image=OfficialApp.Detect();
    Console.WriteLine(JsonSerializer.Serialize(new{verified=image!=null,version=image?.Version,currentDiskMatches=OfficialApp.CurrentDiskMatches,publicShortcutBlocked=AppState.IncidentBlocked,scope="Read-only public installed-image signature and existing incident policy; no UI content or input"}));
    return image==null?1:0;
}
var checks=new Dictionary<string,bool>();
checks["new_users_have_no_implicit_consent"]=new Preferences().Consented==false;
checks["bounded_ipc_rejects_paths"]=!LocalControl.Allowed("settings C:\\anything.exe");
checks["bounded_ipc_rejects_extra_data"]=!LocalControl.Allowed("activate/"+Guid.NewGuid().ToString("N")+"&path=x");
checks["bounded_ipc_accepts_nonce_only"]=LocalControl.Allowed("activate/"+Guid.NewGuid().ToString("N"));
checks["toast_rejects_extra_payload"]=!WeChatNotifier.Program.TrySubmission("action=open-wechat&submission="+Guid.NewGuid().ToString("N")+"&path=x",out _);
checks["current_incident_never_bypassed"]=AppState.BlocksPublicShortcut(true,true,new(true,"STABILITY_INCIDENT_CHAT_NONINTERACTIVE_INPUT_MISSING"));
checks["legacy_window_incident_is_not_shortcut_consent"]=!AppState.BlocksPublicShortcut(false,true,new(true,"STABILITY_INCIDENT_CHAT_NONINTERACTIVE_INPUT_MISSING"));
checks["unknown_incident_fails_closed"]=AppState.BlocksPublicShortcut(false,true,new(true,"UNKNOWN"))&&AppState.BlocksPublicShortcut(false,true,null);
checks["own_image_not_accepted_as_wechat"]=OfficialApp.Verify(Environment.ProcessPath!)==null;
checks["version_evidence_is_exact"]=OfficialApp.IsVerifiedPublicApiVersion("4.1.15.56")&&!OfficialApp.IsVerifiedPublicApiVersion("4.1.99.99");
checks["user_chord_is_explicit"]=ConfiguredShortcutActivation.TryParse("Ctrl+Alt+W",out int key,out bool ctrl,out bool alt,out bool shift)&&key==87&&ctrl&&alt&&!shift;
checks["dangerous_or_incomplete_chords_rejected"]=!ConfiguredShortcutActivation.TryParse("Alt+F4",out _,out _,out _,out _)&&!ConfiguredShortcutActivation.TryParse("Ctrl+W",out _,out _,out _,out _);
checks["partial_input_only_releases_owned_downs"]=ConfiguredShortcutActivation.Outstanding([17,18,87],4).SequenceEqual(new ushort[]{18,17});
checks["privacy_payload_has_no_message_data_slot"]=System.Xml.Linq.XElement.Parse(PrivacyPayload.Build(Guid.NewGuid().ToString("N"),null,false)).Descendants("text").Count()==2;
checks["system_suppression_preserved"]=NotificationCore.SelectPresentation(new(false,true,5))==Presentation.Skip&&NotificationCore.SelectPresentation(new(true,true,2))==Presentation.Silent;
Console.WriteLine(JsonSerializer.Serialize(new{passed=checks.Values.All(x=>x),checks,scope="No WeChat, notifications, registry writes or input; public policy and activation guards only"},new JsonSerializerOptions{WriteIndented=true}));
return checks.Values.All(x=>x)?0:1;
