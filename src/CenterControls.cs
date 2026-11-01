using System.Drawing.Drawing2D;
namespace WeChatNotifier;

internal static class CenterTheme
{
    internal static bool Contrast => SystemInformation.HighContrast;
    internal static Color Canvas => Contrast ? SystemColors.Window : Color.FromArgb(245,247,249);
    internal static Color Surface => Contrast ? SystemColors.Window : Color.White;
    internal static Color Text => Contrast ? SystemColors.WindowText : Color.FromArgb(29,45,53);
    internal static Color Muted => Contrast ? SystemColors.WindowText : Color.FromArgb(86,105,116);
    internal static Color Border => Contrast ? SystemColors.WindowText : Color.FromArgb(221,229,233);
    internal static Color Accent => Contrast ? SystemColors.Highlight : Color.FromArgb(0,109,101);
    internal static Color Tint => Contrast ? SystemColors.Control : Color.FromArgb(231,245,241);
    internal static Color OnAccent => Contrast ? SystemColors.HighlightText : Color.White;
    internal static void Button(Button b, bool primary = false)
    {
        b.FlatStyle=FlatStyle.Flat; b.FlatAppearance.BorderSize=1;
        b.FlatAppearance.BorderColor=primary?Accent:Border;
        b.BackColor=primary?Accent:Surface;b.ForeColor=primary?OnAccent:Text;
        b.AutoSize=true;b.AutoSizeMode=AutoSizeMode.GrowAndShrink;
        b.Padding=new Padding(14,6,14,6);b.Margin=new Padding(0,0,10,0);
        b.Cursor=Cursors.Hand;b.UseVisualStyleBackColor=false;
    }
}
internal sealed class WrapLabel : Label
{
    internal WrapLabel(string text, float size=9.5f, bool bold=false, bool muted=false)
    {
        Text=text;AutoSize=true;Dock=DockStyle.Top;Margin=new Padding(0,0,0,10);
        ForeColor=muted?CenterTheme.Muted:CenterTheme.Text;
        Font=new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular);
    }
    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if(Parent!=null)Parent.SizeChanged+=ParentResized;
        Constrain();
    }
    void ParentResized(object? s,EventArgs e)=>Constrain();
    void Constrain()
    {
        if(Parent==null)return;
        int width=Math.Max(80,Parent.ClientSize.Width-Parent.Padding.Horizontal-Margin.Horizontal);
        if(MaximumSize.Width!=width)MaximumSize=new Size(width,0);
    }
    protected override void Dispose(bool disposing){if(disposing&&Parent!=null)Parent.SizeChanged-=ParentResized;base.Dispose(disposing);}
}
internal sealed class SurfaceCard : TableLayoutPanel
{
    internal SurfaceCard()
    {
        DoubleBuffered=true;AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;
        ColumnCount=1;ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        Dock=DockStyle.Top;Padding=new Padding(22,18,22,16);Margin=new Padding(0,0,0,16);
        BackColor=CenterTheme.Surface;
    }
    GraphicsPath Outline()
    {
        float radius=Math.Max(4,8*DeviceDpi/96f),d=radius*2;
        var p=new GraphicsPath();p.AddArc(0,0,d,d,180,90);p.AddArc(Width-1-d,0,d,d,270,90);p.AddArc(Width-1-d,Height-1-d,d,d,0,90);p.AddArc(0,Height-1-d,d,d,90,90);p.CloseFigure();return p;
    }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor??CenterTheme.Canvas);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using var path=Outline();using var fill=new SolidBrush(CenterTheme.Surface);e.Graphics.FillPath(fill,path);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var path=Outline();using var pen=new Pen(CenterTheme.Border);e.Graphics.DrawPath(pen,path);
    }
    internal void Add(Control c){c.Anchor=AnchorStyles.Left|AnchorStyles.Right;Controls.Add(c);}
}
internal sealed class BrandMark : Control
{
    internal BrandMark(){Size=new Size(38,38);AccessibleName="微信提醒助手图标";TabStop=false;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        float s=Math.Min(Width,Height)/40f;e.Graphics.ScaleTransform(s,s);
        using var fill=new SolidBrush(CenterTheme.Accent);e.Graphics.FillEllipse(fill,1,1,38,38);
        using var p=new Pen(CenterTheme.OnAccent,2.1f){StartCap=LineCap.Round,EndCap=LineCap.Round};
        using var shape=new GraphicsPath();
        shape.AddBezier(11,26,14,23,13,19,14,16);shape.AddBezier(14,16,15,9,25,9,26,16);shape.AddBezier(26,16,27,19,26,23,29,26);shape.AddLine(29,26,11,26);
        e.Graphics.DrawPath(p,shape);e.Graphics.DrawArc(p,17,27,6,4,0,180);
    }
}
