using System.Drawing.Drawing2D;
namespace WeChatNotifier;
internal class RoundButton : Button
{
 bool hover,down;internal bool Primary;
 internal RoundButton(){FlatStyle=FlatStyle.Flat;AutoSize=true;Padding=new Padding(15,8,15,8);MinimumSize=new Size(80,40);Cursor=Cursors.Hand;}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){hover=down=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){down=true;Invalidate();base.OnMouseDown(e);}
 protected override void OnMouseUp(MouseEventArgs e){down=false;Invalidate();base.OnMouseUp(e);}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var bg=SystemInformation.HighContrast?BackColor:Primary?(down?Color.FromArgb(5,90,80):hover?Color.FromArgb(9,143,126):Color.FromArgb(8,127,114)):down?Color.FromArgb(221,232,227):hover?Color.FromArgb(237,244,240):BackColor;using var p=Rounded(new RectangleF(0,0,Width-1,Height-1),9*DeviceDpi/96f);using var fill=new SolidBrush(bg);e.Graphics.FillPath(fill,p);using var pen=new Pen(Focused?CenterTheme.Accent:CenterTheme.Border,Focused?2:1);e.Graphics.DrawPath(pen,p);DrawContent(e.Graphics);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(ClientRectangle,-5,-5));}
 protected virtual void DrawContent(Graphics g)=>TextRenderer.DrawText(g,Text,Font,ClientRectangle,Enabled?Primary?CenterTheme.OnAccent:ForeColor:SystemColors.GrayText,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
 internal static GraphicsPath Rounded(RectangleF r,float radius){float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));var p=new GraphicsPath();p.AddArc(r.Left,r.Top,d,d,180,90);p.AddArc(r.Right-d,r.Top,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.Left,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
}
internal sealed class NotificationIllustration : Control
{
 internal NotificationIllustration(){DoubleBuffered=true;SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;Dock=DockStyle.Fill;AccessibleName="通知示意：微信，微信有新提醒，消息正文已隐藏";AccessibleRole=AccessibleRole.Graphic;}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using var ring=new Pen(Color.FromArgb(48,255,255,255),1);for(int i=0;i<3;i++)g.DrawEllipse(ring,Width/2-80-i*24,Height/2-80-i*24,160+i*48,160+i*48);var r=new RectangleF(12,Height/2-65,Width-24,130);using var path=RoundButton.Rounded(r,16);using var bg=new SolidBrush(CenterTheme.Surface);g.FillPath(bg,path);using var accent=new SolidBrush(CenterTheme.Accent);g.FillEllipse(accent,r.Left+18,r.Top+20,27,27);using var bell=new Pen(Color.White,1.7f);g.DrawArc(bell,r.Left+25,r.Top+26,13,14,180,180);g.DrawLine(bell,r.Left+25,r.Top+33,r.Left+24,r.Top+39);g.DrawLine(bell,r.Left+38,r.Top+33,r.Left+39,r.Top+39);g.DrawLine(bell,r.Left+24,r.Top+39,r.Left+39,r.Top+39);g.DrawArc(bell,r.Left+29,r.Top+40,5,3,0,180);using var name=new Font("Microsoft YaHei UI",9f);using var body=new Font("Microsoft YaHei UI",11f,FontStyle.Bold);TextRenderer.DrawText(g,"微信",name,new Point((int)r.Left+56,(int)r.Top+18),CenterTheme.Text);TextRenderer.DrawText(g,"微信有新提醒",body,new Point((int)r.Left+18,(int)r.Top+59),CenterTheme.Text);TextRenderer.DrawText(g,"消息正文已隐藏",name,new Point((int)r.Left+18,(int)r.Top+91),CenterTheme.Muted);TextRenderer.DrawText(g,"通知示意",name,new Rectangle(0,0,Width,24),Color.White,TextFormatFlags.HorizontalCenter);}
}
internal sealed class HeroPanel : Panel
{
 internal HeroPanel(){DoubleBuffered=true;Dock=DockStyle.Top;Height=222;Padding=new Padding(14);Margin=new Padding(0,0,0,16);}
 protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent?.BackColor??CenterTheme.Canvas);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var path=RoundButton.Rounded(new RectangleF(0,0,Width-1,Height-1),18);using var fill=new LinearGradientBrush(ClientRectangle,SystemInformation.HighContrast?SystemColors.Window:Color.FromArgb(19,49,46),SystemInformation.HighContrast?SystemColors.Window:Color.FromArgb(34,77,68),30);e.Graphics.FillPath(fill,path);}
}
