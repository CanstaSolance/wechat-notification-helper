using System.Runtime.InteropServices;
using CommunityToolkit.WinUI.Notifications;
using Windows.UI.Notifications;
using WeChatNotifier.Core;

namespace WeChatNotifier;

internal sealed class WindowsNoticeSink : INoticeSink
{
    ToastNotifierCompat? notifier;
    ToastNotification? last;
    string? logo;
    internal object? LastDelivery {get;private set;}
    internal void Initialize()
    {
        notifier=ToastNotificationManagerCompat.CreateToastNotifier();NotificationIdentity.Ensure();
        logo=OfficialLogo.Prepare();_ = NotificationIdentity.TrySetOwnIcon(logo);
    }
    public PlatformState ReadState()
    {
        int hr=SHQueryUserNotificationState(out int state);
        return new(notifier?.Setting==NotificationSetting.Enabled,hr>=0,state);
    }
    public Delivery Submit(NoticeRequest request)=>Send(request,false);
    internal Delivery Test()
    {
        var mode=NotificationCore.SelectPresentation(ReadState());
        return Send(new(mode,Guid.NewGuid().ToString("N")),true);
    }
    Delivery Send(NoticeRequest request,bool test)
    {
        if(notifier==null||request.Presentation==Presentation.Skip)return new(false,"系统已阻止通知");
        bool silent=request.Presentation==Presentation.Silent;
        var xml=new Windows.Data.Xml.Dom.XmlDocument();
        xml.LoadXml(test?PrivacyPayload.BuildValidation(request.SubmissionId,logo,silent):PrivacyPayload.Build(request.SubmissionId,logo,silent));
        if(last!=null)last.Failed-=Failed;
        DateTimeOffset expires=DateTimeOffset.Now.AddMinutes(test?30:60);
        last=new ToastNotification(xml){Tag=test?"self-test":"attention",Group="wechat-attention",SuppressPopup=silent,ExpirationTime=expires};
        last.Failed+=Failed;notifier.Show(last);
        bool? historyMatched=null;
        try{historyMatched=ToastNotificationManager.History.GetHistory(NotificationIdentity.Aumid).Any(item=>item.Tag==last.Tag&&item.Group==last.Group&&item.Content.DocumentElement.GetAttribute("launch")=="action=open-wechat&submission="+request.SubmissionId);}catch{}
        LastDelivery=new{submission=request.SubmissionId,test,expiresUtc=expires.ToUniversalTime(),historyMatched,status="API_ACCEPTED_VISUAL_RESULT_UNKNOWN"};
        AppState.Log("delivery.json",LastDelivery);
        return new(true,"已交给 Windows；实际显示受系统通知设置影响");
    }
    void Failed(ToastNotification sender,ToastFailedEventArgs args)=>AppState.Log("notification-error.json",new{hresult=args.ErrorCode.HResult});
    public void Dispose(){if(last!=null)last.Failed-=Failed;last=null;notifier=null;}
    [DllImport("shell32.dll")]static extern int SHQueryUserNotificationState(out int state);
}
