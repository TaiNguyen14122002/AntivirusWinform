using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ScanAndRemoveVirus.Control;

internal static class Sketch20TypographySmoke
{
    private static void Pump() { for (int i = 0; i < 8; i++) { Application.DoEvents(); Thread.Sleep(10); } }
    private static string Safe(string text) { return (text ?? "").Replace("\t", " ").Replace("\r", " ").Replace("\n", " / "); }
    private static Rectangle Ink(Graphics graphics, Sketch20TextRun text, out Size required)
    {
        var flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | (text.Wrap ? TextFormatFlags.WordBreak : TextFormatFlags.SingleLine);
        required = TextRenderer.MeasureText(graphics, text.Value, Sketch20Style.Font(text.Pixels, text.Bold, text.Mono),
            new Size(text.Wrap ? Math.Max(1, text.Bounds.Width) : 32767, 32767), flags);
        int width = text.Wrap ? text.Bounds.Width : Math.Min(required.Width, text.Bounds.Width);
        int x = text.Right ? text.Bounds.Right - width : text.Bounds.X;
        int y = text.Wrap ? text.Bounds.Y : text.Bounds.Y + (text.Bounds.Height - required.Height) / 2;
        return new Rectangle(x, y, width, required.Height);
    }
    private static List<string> Audit(Sketch20View view)
    {
        var errors = new List<string>();
        var text = view.Canvas.TextRuns.Where(t => !string.IsNullOrWhiteSpace(t.Value)).ToArray();
        using (var graphics = view.Canvas.CreateGraphics())
        {
            var ink = new Rectangle[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                Size measured; ink[i] = Ink(graphics, text[i], out measured);
                if (measured.Height > text[i].Bounds.Height + 1) errors.Add("height\t" + Safe(text[i].Value) + "\t" + measured.Height + ">" + text[i].Bounds.Height);
                foreach (System.Windows.Forms.Control input in view.Canvas.Controls)
                    if (input.Bounds.IntersectsWith(ink[i])) errors.Add("input-overlap\t" + Safe(text[i].Value) + "\t" + input.Name);
            }
            for (int i = 0; i < text.Length; i++) for (int j = i + 1; j < text.Length; j++)
            {
                var overlap = Rectangle.Intersect(ink[i], ink[j]);
                if (overlap.Width > 1 && overlap.Height > 1) errors.Add("text-overlap\t" + Safe(text[i].Value) + "\t" + Safe(text[j].Value));
            }
        }
        return errors;
    }
    private static void Shot(Form form, string path)
    {
        using (var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height, PixelFormat.Format24bppRgb))
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix) using (var graphics = Graphics.FromImage(bitmap)) graphics.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, bitmap.Size);
            else form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(path, ImageFormat.Png);
        }
    }
    private static int VerifyTextRenderer(string output)
    {
        int failures = 0;
        foreach (bool wrap in new[] { false, true })
        {
            var bounds = new Rectangle(20, 20, 155, 24);
            using (var bitmap = new Bitmap(360, 110))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.White);
                    Sketch20Style.Text(graphics, string.Join(" ", Enumerable.Repeat("Đường dẫn & tệp tiếng Việt", 20)), bounds, 13, Color.Black, false, wrap);
                }
                bool outside = false, visible = false;
                for (int y = 0; y < bitmap.Height; y++) for (int x = 0; x < bitmap.Width; x++)
                    if (bitmap.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) { visible = true; if (!bounds.Contains(x, y)) outside = true; }
                bool good = visible && !outside; if (!good) failures++;
                Console.WriteLine((good ? "PASS " : "FAIL ") + "pixel-clipping-" + (wrap ? "wrapped" : "single-line"));
                bitmap.Save(Path.Combine(output, "pixel-clipping-" + wrap + ".png"));
            }
        }
        using (var actual = new Bitmap(420, 55))
        {
            const string literal = "Đường dẫn R&D · Bảo vệ & cách ly";
            using (var graphics = Graphics.FromImage(actual)) { graphics.Clear(Color.White); Sketch20Style.Text(graphics, literal, new Rectangle(8, 8, 400, 35), 14, Color.Black); }
            actual.Save(Path.Combine(output, "Vietnamese-literal.png"));
        }
        using (var ampersand = new Bitmap(55, 55))
        {
            using (var graphics = Graphics.FromImage(ampersand))
            {
                graphics.Clear(Color.White);
                Sketch20Style.Text(graphics, "&", new Rectangle(8, 8, 35, 35), 14, Color.Black);
            }
            int ink = 0;
            for (int y = 0; y < ampersand.Height; y++) for (int x = 0; x < ampersand.Width; x++) if (ampersand.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) ink++;
            bool good = ink > 20;
            if (!good) failures++; Console.WriteLine((good ? "PASS " : "FAIL ") + "literal-ampersand-rendered");
        }
        return failures;
    }
    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        string output = Path.GetFullPath(args.Length == 0 ? "TypographyEvidence" : args[0]); Directory.CreateDirectory(output);
        int cases = 0, failed = 0; var report = new List<string> { "case\tissue\ttext\tdetail" };
        var metrics = new List<string> { "case\tbody_font\tmono_font\tdpi_x\tdpi_y\ttext_runs\tissues" };
        string defaultBody = Sketch20Style.BodyFamily;
        using (var form = new Sketch20PreviewForm())
        {
            form.Show(); form.Location = new Point(20, 20); Pump();
            foreach (string family in new[] { defaultBody, FontFamily.GenericSansSerif.Name }.Distinct())
            {
            Sketch20Style.BodyFamily = family;
            string prefix = family == defaultBody ? "" : "fallback-";
            foreach (var size in new[] { new Size(1040, 720), new Size(1240, 800), new Size(1600, 900) })
            {
                form.ClientSize = size;
                for (int screen = 1; screen <= 20; screen++)
                {
                    form.View.SelectScreen(screen); Pump();
                    string label = prefix + screen.ToString("00") + "-" + size.Width + "x" + size.Height;
                    var issues = Audit(form.View); cases++; if (issues.Count > 0) failed++;
                    foreach (string issue in issues) report.Add(label + "\t" + issue);
                    Console.WriteLine((issues.Count == 0 ? "PASS " : "FAIL ") + label + " : " + issues.Count + " vấn đề vùng chữ");
                    using (var graphics = form.View.Canvas.CreateGraphics()) metrics.Add(label + "\t" + Sketch20Style.Font(13).Name + "\t" + Sketch20Style.Font(12, false, true).Name + "\t" + graphics.DpiX + "\t" + graphics.DpiY + "\t" + form.View.Canvas.TextRuns.Count + "\t" + issues.Count);
                    Shot(form, Path.Combine(output, label + ".png"));
                }
                foreach (int screen in new[] { 3, 7, 8, 11, 18 })
                {
                    form.View.SelectScreen(screen);
                    var model = form.View.Model;
                    model.Detail.Path = "/home/nguoi-dung/Thu-muc-tai-lieu/" + new string('a', 180) + "/invoice_update.exe";
                    model.ScanPath = model.Detail.Path;
                    model.History[0].Result = "Phát hiện 128 mối đe dọa";
                    model.History[0].Risk = true;
                    model.VtSummary = "Kết quả phân tích: " + string.Join(" ", Enumerable.Repeat("mẫu cần kiểm tra", 16));
                    model.Sample = false; form.View.Render(); Pump();
                    string label = prefix + screen.ToString("00") + "-long-data-" + size.Width + "x" + size.Height;
                    var issues = Audit(form.View);
                    if (screen == 11)
                    {
                        var pathRun = form.View.Canvas.TextRuns.Single(t => t.Value == model.Detail.Path);
                        if (pathRun.Wrap || form.View.Canvas.TooltipAt(new Point(pathRun.Bounds.X + 2, pathRun.Bounds.Y + 2)) != model.Detail.Path) issues.Add("full-value\tĐường dẫn gốc\tTooltip phải giữ toàn bộ đường dẫn và hàng phải dùng ellipsis");
                        var copy = form.View.Canvas.ContextMenuStrip.Items.OfType<ToolStripMenuItem>().Single(i => i.Text == "Sao chép đường dẫn gốc");
                        copy.PerformClick();
                        if (Clipboard.GetText() != model.Detail.Path) issues.Add("clipboard\tĐường dẫn gốc\tSao chép phải giữ nguyên giá trị");
                    }
                    cases++; if (issues.Count > 0) failed++;
                    foreach (string issue in issues) report.Add(label + "\t" + issue);
                    Console.WriteLine((issues.Count == 0 ? "PASS " : "FAIL ") + label + " : " + issues.Count + " vấn đề vùng chữ");
                    using (var graphics = form.View.Canvas.CreateGraphics()) metrics.Add(label + "\t" + Sketch20Style.Font(13).Name + "\t" + Sketch20Style.Font(12, false, true).Name + "\t" + graphics.DpiX + "\t" + graphics.DpiY + "\t" + form.View.Canvas.TextRuns.Count + "\t" + issues.Count);
                    Shot(form, Path.Combine(output, label + ".png"));
                    form.View.Model = Sketch20Samples.Create(screen);
                }
            }
            }
            Sketch20Style.BodyFamily = defaultBody;
            form.View.SelectScreen(13); Pump();
            var password = form.View.Canvas.Controls.OfType<Sketch20Password>().Single();
            var hint = password.Controls.OfType<Label>().Single();
            bool placeholder = hint.Visible && password.Controls.GetChildIndex(hint) == 0;
            cases++; if (!placeholder) failed++; Console.WriteLine((placeholder ? "PASS " : "FAIL ") + "API-key-placeholder-visible");
            password.Entry.Text = "sample-only-api-key"; Pump();
            bool masked = password.Entry.UseSystemPasswordChar && !hint.Visible;
            cases++; if (!masked) failed++; Console.WriteLine((masked ? "PASS " : "FAIL ") + "API-key-masked-without-placeholder-overlap");
            form.Close();
        }
        failed += VerifyTextRenderer(output); cases += 3;
        File.WriteAllLines(Path.Combine(output, "typography-issues.tsv"), report, new UTF8Encoding(false));
        File.WriteAllLines(Path.Combine(output, "font-metrics.tsv"), metrics, new UTF8Encoding(false));
        Console.WriteLine("RESULT: " + (cases - failed) + "/" + cases + " typography cases passed; " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }
}
