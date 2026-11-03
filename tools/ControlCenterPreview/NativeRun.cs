using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using WeChatNotifier;
internal static class NativeRun
{
 [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
 internal static void Run(SettingsForm form,string output)
 {
  Directory.CreateDirectory(output);
  var timer=new System.Windows.Forms.Timer{Interval=40};int step=0;string originalLanguage=UiText.Language;bool switched=false;var timings=new List<double>();var outcomes=new List<object>();
  void Capture(string name)
  {
   var rect=new Rectangle(form.PointToScreen(Point.Empty),form.ClientSize);
   bool safe=GetForegroundWindow()==form.Handle&&!form.IsDisposed&&form.Visible&&Screen.AllScreens.Any(s=>s.Bounds.Contains(rect));
   if(safe){using var bmp=new Bitmap(rect.Width,rect.Height);using var g=Graphics.FromImage(bmp);g.CopyFromScreen(rect.Location,Point.Empty,rect.Size);bmp.Save(Path.Combine(output,name+".png"));}
   if(name=="reminders-native"){var labels=new List<object>();void Walk(Control c){if(c is Label)labels.Add(new{text=c.Text,bounds=c.Bounds.ToString(),parent=c.Parent?.Bounds.ToString(),visible=c.Visible});foreach(Control child in c.Controls)Walk(child);}Walk(form);File.WriteAllText(Path.Combine(output,"label-layout.json"),JsonSerializer.Serialize(labels));}outcomes.Add(new{name,saved=safe,dpi=form.DeviceDpi,foreground=GetForegroundWindow()==form.Handle,withinScreen=Screen.AllScreens.Any(s=>s.Bounds.Contains(rect))});
  }
  form.Shown+=(_,_)=>timer.Start();
  timer.Tick+=(_,_)=>{
   if(step<18){var watch=Stopwatch.StartNew();form.ShowPage(step%3);timings.Add(watch.Elapsed.TotalMilliseconds);if(step%10==0)form.ClientSize=new Size(step%20==0?880:920,680);form.SelectNextControl(form.ActiveControl,true,true,true,true);}
   if(step==4||step==7){ComboBox? Find(Control c){if(c is ComboBox b&&b.AccessibleName=="Language / 语言")return b;foreach(Control child in c.Controls){var match=Find(child);if(match!=null)return match;}return null;}var language=Find(form);if(language!=null){language.SelectedIndex=step==4?(originalLanguage=="en"?0:1):(originalLanguage=="en"?1:0);switched=true;}}if(step==18){form.WindowState=FormWindowState.Minimized;}
   if(step==20){form.WindowState=FormWindowState.Normal;form.ClientSize=new Size(920*form.DeviceDpi/96,680*form.DeviceDpi/96);var area=Screen.FromControl(form).WorkingArea;form.Location=new Point(area.Left+Math.Max(0,(area.Width-form.Width)/2),area.Top+Math.Max(0,(area.Height-form.Height)/2));form.Activate();form.ShowPage(0);}
   if(step==23&&GetForegroundWindow()==form.Handle)Cursor.Position=form.PointToScreen(new Point(70,95));if(step==24)Capture("reminders-native");
   if(step==25)form.ShowPage(1);
   if(step==29)Capture("settings-native");
   if(step==30)form.ShowPage(2);
   if(step==34)Capture("help-native");
   if(++step==36){timer.Stop();File.WriteAllText(Path.Combine(output,"native-result.json"),JsonSerializer.Serialize(new{language=UiText.Language,languageSelectorExercised=switched,switches=timings.Count,coldMaxMs=timings.Take(3).Max(),warmMedianMs=timings.Skip(3).Order().ElementAt(7),maxSwitchMs=timings.Max(),medianSwitchMs=timings.Order().ElementAt(timings.Count/2),captures=outcomes,scope="Visible native window, screen pixels only when own HWND is foreground; no WeChat or notification engine."},new JsonSerializerOptions{WriteIndented=true}));form.Close();}
  };
  Application.Run(form);timer.Dispose();
 }
}
