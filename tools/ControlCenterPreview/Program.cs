using WeChatNotifier;
using System.Drawing.Imaging;
using System.Text.Json;

internal static class Program
{
    [STAThread] static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();
        string Option(string name,string fallback){int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        var host=new PreviewHost(Option("--state","waiting"));
        using var form=new SettingsForm(host);host.Form=form;
        string output=Option("--capture","");float scale=float.Parse(Option("--scale","1"),System.Globalization.CultureInfo.InvariantCulture);
        string exercise=Option("--exercise","");
        if(exercise.Length>0)
        {
            form.Shown+=(_,_) =>
            {
                var results=new Dictionary<string,bool>();
                T Field<T>(string name) where T:Control => (T)typeof(SettingsForm).GetField(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(form)!;
                form.ShowPage(1);
                var save=Field<Button>("save");var consent=Field<CheckBox>("consent");var startup=Field<CheckBox>("startup");var cancel=Field<Button>("cancel");var feedback=Field<Label>("feedback");
                if(host.Values.Consented)
                {
                    save.PerformClick();results["error_feedback_is_inline"]=feedback.Text.Contains("错误展示");
                    results["error_does_not_claim_success"]=!feedback.Text.StartsWith("✓");
                }
                else
                {
                    save.PerformClick();results["first_use_requires_consent"]=feedback.Text.Contains("勾选同意")&&!host.Values.Consented;
                    consent.Checked=true;save.PerformClick();results["memory_save_and_consent_lock"]=host.Values.Consented&&!consent.Enabled&&feedback.Text.Contains("内存");
                }
                startup.Checked=!host.Values.Startup;results["editing_enables_restore"]=cancel.Enabled;
                cancel.PerformClick();results["restore_discards_only_unsaved_edits"]=startup.Checked==host.Values.Startup&&!cancel.Enabled;
                File.WriteAllText(exercise,JsonSerializer.Serialize(new{passed=results.Values.All(x=>x),checks=results,scope="Actual shared-form controls and handlers, isolated memory host; no Windows input or user-state writes."},new JsonSerializerOptions{WriteIndented=true}));
                form.Close();
            };
            Application.Run(form);return;
        }
        if(output.Length>0)
        {
            Directory.CreateDirectory(output);int page=0;
            var timer=new System.Windows.Forms.Timer{Interval=600};
            form.Shown+=(_,_)=>{if(scale!=1){var fonts=new Dictionary<Control,Font>();void Fonts(Control c){fonts[c]=c.Font;foreach(Control child in c.Controls)Fonts(child);}Fonts(form);form.Scale(new SizeF(scale,scale));foreach(var pair in fonts)pair.Key.Font=new Font(pair.Value.FontFamily,pair.Value.Size*scale,pair.Value.Style);form.ClientSize=new Size((int)(1000*scale),(int)(850*scale));form.PerformLayout();}string viewport=Option("--viewport","");if(viewport.Length>0){var sizes=viewport.Split('x');var size=new Size(int.Parse(sizes[0]),int.Parse(sizes[1]));form.MinimumSize=new Size(Math.Min(form.MinimumSize.Width,size.Width),Math.Min(form.MinimumSize.Height,size.Height));form.ClientSize=size;}timer.Start();};
            timer.Tick+=(_,_)=>{form.ShowPage(page);form.PerformLayout();if(page==1){var list=new List<object>();void Scan(Control c,int depth){if(depth>3)return;list.Add(new{type=c.GetType().Name,bounds=c.Bounds.ToString(),client=c.ClientSize.ToString(),minimum=c.MinimumSize.ToString(),font=c.Font.Size,dock=c.Dock.ToString(),visible=c.Visible});foreach(Control child in c.Controls)Scan(child,depth+1);}Scan(form,0);File.WriteAllText(Path.Combine(output,"layout.json"),JsonSerializer.Serialize(list));}using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,form.ClientRectangle with{Width=form.Width,Height=form.Height});var origin=form.PointToScreen(Point.Empty);var client=new Rectangle(origin.X-form.Left,origin.Y-form.Top,form.ClientSize.Width,form.ClientSize.Height);using var cropped=bitmap.Clone(client,PixelFormat.Format32bppArgb);cropped.Save(Path.Combine(output,new[]{"overview","settings","privacy"}[page]+".png"),ImageFormat.Png);if(++page==3){timer.Stop();form.Close();}};
            Application.Run(form);timer.Dispose();
        }
        else Application.Run(form);
    }
}
internal sealed class PreviewHost : IControlCenterHost
{
    readonly string scenario;
    CenterValues values;
    public Form? Form;
    internal PreviewHost(string scenario){this.scenario=scenario;values=new(scenario!="setup",scenario!="paused",false,null,"",false,false);}
    public bool IsPreview=>true;
    public CenterValues Values=>values;
    public CenterState State=>new(!values.Consented?CenterPhase.Setup:!values.Enabled?CenterPhase.Paused:scenario=="error"?CenterPhase.Attention:scenario=="active"?CenterPhase.Active:CenterPhase.Waiting,
        values.Consented?"4.1.15.56":null,false,false,false,"界面预览：演示数据，不连接微信，不保存真实配置。");
    public Task<CenterResult> Save(CenterDraft draft)
    {
        if(draft.Shortcut.Length>0&&!System.Text.RegularExpressions.Regex.IsMatch(draft.Shortcut,@"^Ctrl\+Alt\+(?:Shift\+)?[A-Z]$"))
            return Task.FromResult(new CenterResult(false,"请核对快捷键格式；也可以留空。此为界面预览，未写入配置。"));
        if(scenario=="error")return Task.FromResult(new CenterResult(false,"设置文件暂时无法访问。此为错误展示预览，未写入真实配置。"));
        values=new(true,draft.Enabled,draft.Startup,draft.Path,draft.Shortcut,draft.Confirmed,draft.PauseShortcut);
        return Task.FromResult(new CenterResult(true,"预览设置已应用到内存；未保存真实配置，也不会发送通知或快捷键。"));
    }
    public CenterResult SetEnabled(bool enabled){values=values with{Enabled=enabled};return new(true,enabled?"预览：已恢复。未连接微信。":"预览：已暂停。未连接微信。");}
    public CenterResult Test()=>new(true,"预览：未发送 Windows 通知。正式版会检查实际通知通道。");
    public void Quit()=>Form?.Close();
    public void Uninstall()=>throw new IOException("预览没有可卸载的安装。");
}
