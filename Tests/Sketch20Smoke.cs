using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ScanAndRemoveVirus.Control;

internal static partial class Sketch20Smoke
{
    private static int checks, failures;
    private static void Check(string caption, bool good) { checks++; if (!good) failures++; Console.WriteLine((good ? "PASS: " : "FAIL: ") + caption); }
    private static void Pump(int duration = 140) { var end = DateTime.UtcNow.AddMilliseconds(duration); do { Application.DoEvents(); Thread.Sleep(10); } while (DateTime.UtcNow < end); }
    private static IEnumerable<System.Windows.Forms.Control> Descendants(System.Windows.Forms.Control root)
    { foreach (System.Windows.Forms.Control c in root.Controls) { yield return c; foreach (var child in Descendants(c)) yield return child; } }
    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e) { Check("Không có lỗi UI: " + e.Exception, false); };
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        string output = args.Length > 0 ? args[0] : "Sketch20Verification"; Directory.CreateDirectory(output);
        int[] heights = { 800, 747, 740, 738, 740, 738, 740, 740, 770, 740, 791, 770, 740, 740, 738, 738, 738, 848, 894, 800 };
        var allNodes = new List<string> { "screen\tname\tx\ty\twidth\theight" };
        using (var form = new Sketch20PreviewForm())
        {
            form.Padding = new Padding(1); form.BackColor = Sketch20Style.Border; form.Show(); Pump();
            for (int i = 1; i <= 20; i++)
            {
                form.ClientSize = new Size(1240, heights[i - 1]); form.View.SelectScreen(i); Pump();
                var view = form.View; var nodes = view.Canvas.Nodes;
                Check(i + ": đúng màn " + Sketch20View.ScreenNames[i - 1], view.ScreenNumber == i);
                Check(i + ": các thành phần nằm trong chiều rộng trang", nodes.All(n => n.Value.Left >= 0 && n.Value.Right <= view.Canvas.Width));
                Check(i + ": các nút thao tác không bị cắt", Descendants(view.Canvas).OfType<Button>().All(b => b.Bounds.Right <= b.Parent.Width && b.Bounds.Bottom <= b.Parent.Height));
                Check(i + ": không có control chồng sang sidebar", view.Canvas.PointToScreen(Point.Empty).X > form.PointToScreen(Point.Empty).X + 208);
                Check(i + ": dùng đủ chiều rộng phác thảo, không sinh thanh cuộn thừa", view.Canvas.Width == 978);
                foreach (var node in nodes) allNodes.Add(i + "\t" + node.Key + "\t" + node.Value.X + "\t" + node.Value.Y + "\t" + node.Value.Width + "\t" + node.Value.Height);
                using (var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height, PixelFormat.Format24bppRgb))
                {
                    if (Environment.OSVersion.Platform == PlatformID.Unix) using (var g = Graphics.FromImage(bitmap)) g.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, bitmap.Size);
                    else form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(Path.Combine(output, i.ToString("00") + ".png"), ImageFormat.Png);
                }
            }
            form.View.SelectScreen(2); Pump();
            Check("Bảo vệ có đúng 8 thẻ của phác thảo", form.View.Canvas.Nodes.Keys.Count(k => k.StartsWith("feature-") && !k.StartsWith("feature-icon")) == 8);
            var aq = (Sketch20Switch)form.View.Canvas.Controls["switch-aq"]; aq.Checked = false; Pump();
            Check("Công tắc phác thảo cập nhật model", !form.View.Model.Flag("aq"));
            form.View.SelectScreen(17); Pump();
            Check("Quét tùy chỉnh đúng 4 vị trí", form.View.Canvas.Controls.OfType<Sketch20Check>().Count() == 4);
            Check("Bắt đầu quét nằm trong thẻ phạm vi", form.View.Canvas.Nodes["scan-scope"].Contains(form.View.Canvas.Nodes["start-advanced"]));
            form.View.SelectScreen(13); Pump();
            Check("Ô API key che ký tự", form.View.Canvas.Controls.OfType<Sketch20Password>().Single().Entry.UseSystemPasswordChar);
            for (int i = 7; i <= 11; i++) { form.View.SelectScreen(i); Pump(); Check(i + ": đúng tab chi tiết", form.View.DetailTab == i - 7 && form.View.Canvas.Controls.OfType<Sketch20Button>().Count(b => b.Tab && b.Selected) == 1); }
            foreach (var size in new[] { new Size(1040, 720), new Size(1600, 900) })
            {
                form.ClientSize = size;
                foreach (int screen in new[] { 2, 3, 4, 7, 12, 13, 17 })
                {
                    form.View.SelectScreen(screen); Pump();
                    Check(screen + " " + size + ": nút giữ trong vùng nội dung", Descendants(form.View.Canvas).OfType<Button>().All(b => b.Right <= b.Parent.Width && b.Bottom <= b.Parent.Height));
                }
            }
            form.Close();
        }
        VerifyBridge(output);
        File.WriteAllLines(Path.Combine(output, "layout.tsv"), allNodes);
        Console.WriteLine("RESULT: " + (checks - failures) + "/" + checks + " checks passed; " + failures + " failures.");
        return failures == 0 ? 0 : 1;
    }
}
