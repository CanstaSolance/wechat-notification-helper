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
UiText.Set("zh");
UiText.Persist=_=>throw new UnauthorizedAccessException("isolated");
try{await UiText.SetAsync("en");checks["async_language_failure_keeps_previous"]=false;}catch(UnauthorizedAccessException){checks["async_language_failure_keeps_previous"]=UiText.Language=="zh";}finally{UiText.Persist=null;}
var order=new List<string>();UiText.Persist=value=>{lock(order)order.Add(value);};await Task.WhenAll(UiText.SetAsync("en"),UiText.SetAsync("zh"));checks["async_language_writes_serialized"]=order.SequenceEqual(new[]{"en","zh"})&&UiText.Language=="zh";UiText.Persist=null;
checks["new_users_have_no_implicit_consent"]=new Preferences().Consented==false;
checks["bounded_ipc_rejects_paths"]=!LocalControl.Allowed("settings C:\\anything.exe");
checks["bounded_ipc_rejects_extra_data"]=!LocalControl.Allowed("activate/"+Guid.NewGuid().ToString("N")+"&path=x");
checks["bounded_ipc_accepts_nonce_only"]=LocalControl.Allowed("activate/"+Guid.NewGuid().ToString("N"));
checks["toast_rejects_extra_payload"]=!WeChatNotifier.Program.TrySubmission("action=open-wechat&submission="+Guid.NewGuid().ToString("N")+"&path=x",out _);
checks["current_incident_never_bypassed"]=AppState.BlocksPublicShortcut(true,true,new(true,"STABILITY_INCIDENT_CHAT_NONINTERACTIVE_INPUT_MISSING"));
checks["legacy_window_incident_is_not_shortcut_consent"]=!AppState.BlocksPublicShortcut(false,true,new(true,"STABILITY_INCIDENT_CHAT_NONINTERACTIVE_INPUT_MISSING"));
checks["unknown_incident_fails_closed"]=AppState.BlocksPublicShortcut(false,true,new(true,"UNKNOWN"))&&AppState.BlocksPublicShortcut(false,true,null);
checks["legacy_missing_null_wrong_type_or_invalid_json_fails_closed"]=new[]{"{}","{\"reason\":\"UNKNOWN\"}","{\"blocked\":null}","{\"blocked\":\"false\"}","{\"blocked\":0}","{\"blocked\":true,\"reason\":123}","{","null","[]"}.All(BlocksLegacyJson);
checks["legacy_explicit_false_remains_compatible"]=!BlocksLegacyJson("{\"blocked\":false}")&&!BlocksLegacyJson("{\"blocked\":false,\"reason\":\"UNKNOWN\"}");
checks["own_image_not_accepted_as_wechat"]=OfficialApp.Verify(Environment.ProcessPath!)==null;
checks["version_evidence_is_exact"]=OfficialApp.IsVerifiedPublicApiVersion("4.1.15.56")&&!OfficialApp.IsVerifiedPublicApiVersion("4.1.99.99");
checks["user_chord_is_explicit"]=ConfiguredShortcutActivation.TryParse("Ctrl+Alt+W",out int key,out bool ctrl,out bool alt,out bool shift)&&key==87&&ctrl&&alt&&!shift;
checks["dangerous_or_incomplete_chords_rejected"]=!ConfiguredShortcutActivation.TryParse("Alt+F4",out _,out _,out _,out _)&&!ConfiguredShortcutActivation.TryParse("Ctrl+W",out _,out _,out _,out _);
checks["partial_input_only_releases_owned_downs"]=ConfiguredShortcutActivation.Outstanding([17,18,87],4).SequenceEqual(new ushort[]{18,17});
checks["privacy_payload_has_no_message_data_slot"]=System.Xml.Linq.XElement.Parse(PrivacyPayload.Build(Guid.NewGuid().ToString("N"),null,false)).Descendants("text").Count()==2;
checks["system_suppression_preserved"]=NotificationCore.SelectPresentation(new(false,true,5))==Presentation.Skip&&NotificationCore.SelectPresentation(new(true,true,2))==Presentation.Silent;
checks["installed_and_enabled_without_live_target_is_waiting"]=CenterCopy.Phase(true,false,true,true,"waiting",0)==CenterPhase.Waiting&&CenterCopy.Phase(true,false,true,true,"active",0)==CenterPhase.Waiting;
checks["no_consent_never_advertises_monitoring"]=CenterCopy.Phase(false,false,true,true,"active",3)==CenterPhase.Setup;
checks["unready_source_never_advertises_monitoring"]=CenterCopy.Phase(true,false,true,false,"active",3)==CenterPhase.Attention;
checks["shortcut_normalization_matches_real_parser"]=Enumerable.Range(65,26).All(k=>ShortcutEntryState.Normalize(Keys.Control|Keys.Alt|(Keys)k) is {} text&&ConfiguredShortcutActivation.TryParse(text,out int letter,out _,out _,out _)&&letter==k);
checks["shortcut_rejects_shift_function_and_save_accelerator"]=ShortcutEntryState.Normalize(Keys.Control|Keys.Alt|Keys.Shift|Keys.W)==null&&ShortcutEntryState.Normalize(Keys.Control|Keys.Alt|Keys.F1)==null&&ShortcutEntryState.Normalize(Keys.Alt|Keys.S)==null;
var entry=new ShortcutEntryState();entry.Set("Ctrl+Alt+K");entry.Begin();entry.Down(Keys.Control|Keys.Alt|Keys.W);entry.Down(Keys.Escape);
checks["escape_preserves_previous"]=entry.Value=="Ctrl+Alt+K"&&!entry.Recording;
entry.Begin();entry.Down(Keys.Control|Keys.Alt|Keys.W);entry.Cancel();checks["focus_cancel_preserves_previous"]=entry.Value=="Ctrl+Alt+K"&&!entry.Recording;
entry.Begin();entry.Down(Keys.Control|Keys.Alt|Keys.W);checks["only_release_commits"]=entry.Value=="Ctrl+Alt+K"&&entry.Up(Keys.W)&&entry.Value=="Ctrl+Alt+W";
var prior=new CenterValues(true,true,false,null,"Ctrl+Alt+K",true,false);
checks["ordinary_settings_do_not_rebind"]=!CenterCopy.BindingEdited(prior,prior.Shortcut,prior.PauseShortcut,true,null);
checks["same_shortcut_can_be_reconfirmed_after_update"]=CenterCopy.BindingEdited(prior with{Confirmed=false},prior.Shortcut,false,true,null);
checks["changed_path_requires_reconfirmation"]=CenterCopy.BindingEdited(prior,prior.Shortcut,false,false,"different-install.exe");
checks["non_chinese_system_defaults_to_english"]=UiText.Resolve(null,false,"fr-FR")=="en"&&UiText.Resolve(null,false,"de-DE")=="en";
checks["existing_users_keep_chinese_and_explicit_language_wins"]=UiText.Resolve(null,true,"en-US")=="zh"&&UiText.Resolve("en",true,"zh-CN")=="en";
UiText.Set("en");
checks["english_notification_hides_body"]=PrivacyPayload.Build(Guid.NewGuid().ToString("N"),null,false).Contains("Message text hidden")&&UiText.T("设置")=="Settings";
UiText.Set("zh");checks["language_reversible"]=UiText.ConvertText("Settings")=="设置";
UiText.Set("zh");UiText.Persist=_=>throw new IOException("isolated simulated write failure");
try{UiText.Set("en");checks["language_save_failure_is_atomic"]=false;}catch(IOException){checks["language_save_failure_is_atomic"]=UiText.Language=="zh";}finally{UiText.Persist=null;}
Console.WriteLine(JsonSerializer.Serialize(new{passed=checks.Values.All(x=>x),checks,scope="No WeChat, notifications, registry writes or input; public policy and activation guards only"},new JsonSerializerOptions{WriteIndented=true}));
return checks.Values.All(x=>x)?0:1;

static bool BlocksLegacyJson(string json)
{
    using var stream=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
    return AppState.BlocksPublicShortcut(false,true,AppState.ParseLegacyIncident(stream));
}
