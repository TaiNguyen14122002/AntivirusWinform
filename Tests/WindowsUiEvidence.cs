// Bộ chẩn đoán Windows thật; không dùng kết quả Mono/Wine thay cho Windows.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ScanAndRemoveVirus;
using ScanAndRemoveVirus.Control;
using ScanAndRemoveVirus.Database;
using ScanAndRemoveVirus.Services;

internal static class WindowsUiEvidence
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly int[] ReferenceHeights = { 800, 747, 740, 738, 740, 738, 740, 740, 770, 740, 791, 770, 740, 740, 738, 738, 738, 848, 894, 800 };
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string name);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)] private static extern IntPtr GetProcAddress(IntPtr module, string name);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint from, uint to, bool attach);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr handle, StringBuilder name, int length);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr handle, uint message, IntPtr wparam, IntPtr lparam);
    private static void Pump() { for (int i = 0; i < 12; i++) { Application.DoEvents(); Thread.Sleep(15); } }
    private static string Safe(string value) { return (value ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' '); }
    private static IEnumerable<System.Windows.Forms.Control> Controls(System.Windows.Forms.Control parent)
    { foreach (System.Windows.Forms.Control child in parent.Controls) { yield return child; foreach (var nested in Controls(child)) yield return nested; } }
    private static Rectangle Glyph(Graphics graphics, Sketch20TextRun run, out Size size)
    {
        size = TextRenderer.MeasureText(graphics, run.Value, Sketch20Style.Font(run.Pixels, run.Bold, run.Mono),
            new Size(run.Wrap ? Math.Max(1, run.Bounds.Width) : 32767, 32767), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | (run.Wrap ? TextFormatFlags.WordBreak : TextFormatFlags.SingleLine));
        int width = run.Wrap ? run.Bounds.Width : Math.Min(size.Width, run.Bounds.Width);
        return new Rectangle(run.Right ? run.Bounds.Right - width : run.Bounds.Left,
            run.Bounds.Top + Math.Max(0, (run.Bounds.Height - size.Height) / 2), width, size.Height);
    }
    private static List<string> Audit(Form form, Sketch20View view, string label, List<string> metrics)
    {
        var issues = new List<string>();
        var positioned = new List<Tuple<string, Rectangle>>();
        if (view.Canvas.Nodes.ContainsKey("title")) issues.Add("redundant-header\tTitle\tTiêu đề đầu màn vẫn còn");
        var gear = view.Canvas.Controls["nav-settings"] as Sketch20Button;
        if (gear == null || gear.Text.Length != 0 || gear.AccessibleName != "Cài đặt" || gear.IconName != "settings")
            issues.Add("settings-icon\tCài đặt\tThiếu nút bánh răng có nhãn truy cập");
        else if (gear.Top != 0 || gear.Right != view.Canvas.Width)
            issues.Add("settings-position\tCài đặt\tNút không nằm ở góc trên bên phải");
        var children = view.Canvas.Controls.Cast<System.Windows.Forms.Control>().Where(c => c.Visible).ToArray();
        for (int i = 0; i < children.Length; i++) for (int j = i + 1; j < children.Length; j++)
        {
            var overlap = Rectangle.Intersect(children[i].Bounds, children[j].Bounds);
            if (overlap.Width > 1 && overlap.Height > 1) issues.Add("control-overlap\t" + children[i].Name + "\t" + children[j].Name);
        }
        var viewport = (Panel)typeof(Sketch20View).GetField("viewport", Private).GetValue(view);
        if (viewport.HorizontalScroll.Visible) issues.Add("horizontal-scrollbar\tViewport\tKhông có nội dung cần cuộn ngang");
        using (var graphics = view.Canvas.CreateGraphics())
        {
            var canvasOffset = form.PointToClient(view.Canvas.PointToScreen(Point.Empty));
            foreach (var run in view.Canvas.TextRuns)
            {
                Size size; var rect = Glyph(graphics, run, out size);
                if (size.Height > run.Bounds.Height + 1) issues.Add("text-height\t" + Safe(run.Value) + "\t" + size.Height + ">" + run.Bounds.Height);
                foreach (System.Windows.Forms.Control child in view.Canvas.Controls)
                    if (child.Bounds.IntersectsWith(rect)) issues.Add("text-control-overlap\t" + Safe(run.Value) + "\t" + child.Name);
                rect.Offset(canvasOffset); positioned.Add(Tuple.Create(run.Value, rect));
            }
        }
        foreach (var control in Controls(view).Where(c => c.Visible && c.Width > 0 && c.Height > 0))
        {
            var origin = form.PointToClient(control.PointToScreen(Point.Empty));
            using (var graphics = control.CreateGraphics())
            {
                metrics.Add(label + "\t" + Safe(control.Name) + "\t" + control.GetType().Name + "\t" + origin.X + "\t" + origin.Y + "\t" + control.Width + "\t" + control.Height + "\t" + Safe(control.Font.Name) + "\t" + control.Font.Size + "\t" + graphics.DpiX + "\t" + graphics.DpiY);
                var runs = new List<Sketch20TextRun>();
                Sketch20Style.TraceText = (paint, text) => runs.Add(text);
                try
                {
                    if (control is Sketch20Button || control is Sketch20Mode || control is Sketch20Check || control is Sketch20Choice || control is Sketch20Date)
                        using (var bitmap = new Bitmap(control.Width, control.Height))
                        { bitmap.SetResolution(graphics.DpiX, graphics.DpiY); control.DrawToBitmap(bitmap, control.ClientRectangle); }
                }
                finally { Sketch20Style.TraceText = null; }
                foreach (var run in runs)
                {
                    Size size; var rect = Glyph(graphics, run, out size);
                    if (size.Height > run.Bounds.Height + 1) issues.Add("control-text-height\t" + Safe(control.Name + ": " + run.Value) + "\t" + size.Height + ">" + run.Bounds.Height);
                    if (!control.ClientRectangle.Contains(run.Bounds)) issues.Add("control-text-bounds\t" + Safe(control.Name + ": " + run.Value) + "\t" + run.Bounds);
                    rect.Offset(origin); positioned.Add(Tuple.Create(control.Name + ": " + run.Value, rect));
                }
                var entry = control as TextBox;
                if (entry != null && entry.PreferredHeight > entry.Height) issues.Add("native-input-height\t" + Safe(entry.AccessibleName) + "\t" + entry.PreferredHeight + ">" + entry.Height);
                var hint = control as Label;
                if (hint != null && hint.Parent is Sketch20Password)
                {
                    var size = TextRenderer.MeasureText(graphics, hint.Text, hint.Font, new Size(32767, 32767), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                    if (size.Height > hint.Height) issues.Add("native-placeholder-height\t" + hint.Text + "\t" + size.Height + ">" + hint.Height);
                }
            }
        }
        for (int i = 0; i < positioned.Count; i++) for (int j = i + 1; j < positioned.Count; j++)
        {
            var intersection = Rectangle.Intersect(positioned[i].Item2, positioned[j].Item2);
            if (intersection.Width > 1 && intersection.Height > 1) issues.Add("glyph-overlap\t" + Safe(positioned[i].Item1) + "\t" + Safe(positioned[j].Item1));
        }
        return issues.Distinct().ToList();
    }
    private static bool VerifyTranslatedText(string output)
    {
        // Nền trong suốt của WinForms gọi paint của parent trên Graphics đã dịch tọa độ.
        // So sánh pixel với cùng chữ vẽ trực tiếp, độc lập với metadata của vùng chữ.
        using (var expected = new Bitmap(240, 90, PixelFormat.Format24bppRgb))
        using (var actual = new Bitmap(240, 90, PixelFormat.Format24bppRgb))
        {
            using (var graphics = Graphics.FromImage(expected))
            { graphics.Clear(Color.White); Sketch20Style.Text(graphics, "Bảo vệ & tệp", new Rectangle(10, 15, 110, 28), 13, Color.Black); }
            using (var graphics = Graphics.FromImage(actual))
            { graphics.Clear(Color.White); graphics.TranslateTransform(-120, -30); Sketch20Style.Text(graphics, "Bảo vệ & tệp", new Rectangle(130, 45, 110, 28), 13, Color.Black); }
            bool visible = false, equal = true;
            for (int y = 0; y < expected.Height; y++) for (int x = 0; x < expected.Width; x++)
            {
                int a = expected.GetPixel(x, y).ToArgb(), b = actual.GetPixel(x, y).ToArgb();
                if (a != Color.White.ToArgb()) visible = true;
                if (a != b) equal = false;
            }
            expected.Save(Path.Combine(output, "text-transform-expected.png"), ImageFormat.Png);
            actual.Save(Path.Combine(output, "text-transform-actual.png"), ImageFormat.Png);
            Console.WriteLine((visible && equal ? "PASS " : "FAIL ") + "Windows GDI translated parent text / pixel equality");
            return visible && equal;
        }
    }
    private static void Shot(Form form, string filename)
    {
        uint process;
        uint foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out process);
        uint thread = GetCurrentThreadId();
        bool attached = foregroundThread != 0 && foregroundThread != thread && AttachThreadInput(thread, foregroundThread, true);
        try { form.BringToFront(); form.Activate(); SetForegroundWindow(form.Handle); }
        finally { if (attached) AttachThreadInput(thread, foregroundThread, false); }
        form.Refresh(); Pump();
        if (GetForegroundWindow() != form.Handle) throw new InvalidOperationException("App không ở foreground; không nhận ảnh có thể bị cửa sổ khác che làm evidence.");
        var region = new Rectangle(form.PointToScreen(Point.Empty), form.ClientSize);
        if (!Screen.FromControl(form).Bounds.Contains(region)) throw new InvalidOperationException("Cửa sổ không nằm trọn trên màn hình; cần độ phân giải lớn hơn trước khi chụp.");
        using (var image = new Bitmap(region.Width, region.Height, PixelFormat.Format24bppRgb))
        { using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(region.Location, Point.Empty, region.Size); image.Save(filename, ImageFormat.Png); }
    }
    private static void Pair(string reference, string screenshot, string output)
    {
        using (var original = new Bitmap(reference)) using (var actual = new Bitmap(screenshot))
        using (var pair = new Bitmap(1280, 560)) using (var graphics = Graphics.FromImage(pair))
        using (var font = new Font("Segoe UI", 13, FontStyle.Bold, GraphicsUnit.Pixel))
        {
            graphics.Clear(Color.White);
            graphics.DrawString("Phác thảo gốc", font, Brushes.Black, 12, 8);
            graphics.DrawString("FrmMain / Windows — dữ liệu mẫu đối chiếu", font, Brushes.Black, 650, 8);
            var crop = new Rectangle(20, 84, Math.Min(1240, original.Width - 20), original.Height - 104);
            float ratio = Math.Min(620f / crop.Width, 515f / crop.Height);
            graphics.DrawImage(original, new RectangleF(12, 35, crop.Width * ratio, crop.Height * ratio), crop, GraphicsUnit.Pixel);
            ratio = Math.Min(620f / actual.Width, 515f / actual.Height);
            graphics.DrawImage(actual, 650, 35, actual.Width * ratio, actual.Height * ratio);
            pair.Save(output, ImageFormat.Png);
        }
    }
    [STAThread]
    private static int Main(string[] args)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT || Type.GetType("Mono.Runtime") != null)
        { Console.Error.WriteLine("WINDOWS_NOT_TESTED: cần Windows thật và .NET Framework, không nhận Mono/Linux làm evidence Windows."); return 2; }
        if (GetProcAddress(GetModuleHandle("ntdll.dll"), "wine_get_version") != IntPtr.Zero)
        { Console.Error.WriteLine("WINDOWS_NOT_TESTED: không nhận Wine thay cho Windows thật."); return 2; }
        if (!Environment.UserInteractive) { Console.Error.WriteLine("WINDOWS_NOT_TESTED: cần desktop Windows đang đăng nhập, không phải service/session headless."); return 2; }
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        string output = Path.GetFullPath(args.Length > 0 ? args[0] : "WindowsEvidence"); Directory.CreateDirectory(output);
        string references = Path.GetFullPath(args.Length > 1 ? args[1] : "UI_Preview/PhacThao_Goc");
        string folder = Path.Combine(output, "isolated-app-data"); Directory.CreateDirectory(folder);
        Environment.SetEnvironmentVariable("XVIRUS_DATA_DIR", folder); Environment.SetEnvironmentVariable("XVIRUS_LEGACY_UI_TEST", "0");
        typeof(DbConnection).GetField("ConnectionString", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, "WindowsUiEvidenceIsolation=true");
        var settings = AppSettings.Load();
        settings.RealTimeOnPersist = settings.UsbProtection = settings.DownloadProtection = settings.BehaviorWatch = settings.StartupFoldersWatch = settings.AutoUpdate = settings.VtAutoQuery = false;
        AppSettings.Save(settings);
        string exe = typeof(FrmMain).Assembly.Location, hash;
        using (var algorithm = SHA256.Create()) using (var stream = File.OpenRead(exe)) hash = BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "");
        var runtime = new List<string> { "captured_utc=" + DateTime.UtcNow.ToString("O"), "os=" + Environment.OSVersion, "clr=" + Environment.Version, "exe=" + exe, "exe_sha256=" + hash,
            "window_class=ScanAndRemoveVirus.FrmMain", "capture=Graphics.CopyFromScreen / visible foreground Windows desktop before bitmap audit", "snapshot_cases=real FrmMain with isolated sample model; not live SQL/API data", "display_scale=unchanged; only the current effective DPI is tested" };
        var metrics = new List<string> { "case\tcontrol\ttype\tx\ty\twidth\theight\tfont\tsize\tdpi_x\tdpi_y" };
        var report = new List<string> { "case\tissue\ttext\tdetail" }; int cases = 0, failed = 0;
        bool translatedText = VerifyTranslatedText(output);
        runtime.Add("translated_text_pixel_test=" + translatedText);
        if (!translatedText) report.Add("text-transform\tpixel-mismatch\tParent text\tGraphics translation changed rendered text");
        using (var dialogs = new System.Windows.Forms.Timer { Interval = 100 })
        {
            dialogs.Tick += delegate
            {
                IntPtr foreground = GetForegroundWindow(); uint process; GetWindowThreadProcessId(foreground, out process);
                var name = new StringBuilder(64); GetClassName(foreground, name, name.Capacity);
                if (process == (uint)Process.GetCurrentProcess().Id && name.ToString() == "#32770") PostMessage(foreground, 0x0111, new IntPtr(1), IntPtr.Zero);
            };
            dialogs.Start();
            try
            {
                using (var form = new FrmMain())
                {
                    form.StartPosition = FormStartPosition.Manual; form.TopMost = true; form.Show(); Pump();
                    var area = Screen.FromControl(form).WorkingArea;
                    form.Location = new Point(area.Left + 4, area.Top + 4);
                    using (var graphics = form.CreateGraphics()) runtime.Add("window_dpi=" + graphics.DpiX + "," + graphics.DpiY);
                    runtime.Add("working_area=" + area); runtime.Add("body_font=" + Sketch20Style.Font(13).Name); runtime.Add("mono_font=" + Sketch20Style.Font(12, false, true).Name);
                    form.ClientSize = new Size(Math.Min(1240, area.Width - 24), Math.Min(800, area.Height - 48)); Pump();
                    Shot(form, Path.Combine(output, "00-real-startup.png"));
                    var view = form.exactView;
                    if (view == null || view.Model.Sample) throw new InvalidOperationException("Startup không dùng renderer mặc định với model thực.");
                    ((System.Windows.Forms.Timer)typeof(Sketch20View).GetField("update", Private).GetValue(view)).Stop();
                    foreach (var requested in new[] { new Size(1040, 720), new Size(1240, 800), new Size(1600, 900), new Size(1240, 0) })
                    {
                        if (requested.Height > 0)
                        {
                            form.ClientSize = new Size(Math.Min(requested.Width, area.Width - 24), Math.Min(requested.Height, area.Height - 48));
                            runtime.Add("requested_client=" + requested + "; actual_client=" + form.ClientSize);
                        }
                        for (int screen = 1; screen <= 20; screen++)
                        {
                            if (requested.Height == 0)
                            {
                                form.ClientSize = new Size(Math.Min(1240, area.Width - 24), Math.Min(ReferenceHeights[screen - 1], area.Height - 48));
                                runtime.Add("reference_screen=" + screen + "; actual_client=" + form.ClientSize);
                            }
                            view.Model = Sketch20Samples.Create(screen); view.SelectScreen(screen); Pump();
                            string label = screen.ToString("00") + (requested.Height == 0 ? "-reference" : "-requested-" + requested.Width + "x" + requested.Height);
                            string screenshot = Path.Combine(output, label + ".png");
                            Shot(form, screenshot); // Chụp desktop trước mọi WM_PRINT/DrawToBitmap dùng để đọc metadata.
                            var issues = Audit(form, view, label, metrics);
                            using (var captured = new Bitmap(screenshot))
                            foreach (var mode in Controls(view).OfType<Sketch20Mode>())
                            {
                                var origin = form.PointToClient(mode.PointToScreen(Point.Empty));
                                foreach (var corner in new[] { origin, new Point(origin.X + mode.Width - 1, origin.Y), new Point(origin.X, origin.Y + mode.Height - 1), new Point(origin.X + mode.Width - 1, origin.Y + mode.Height - 1) })
                                {
                                    if (!new Rectangle(Point.Empty, captured.Size).Contains(corner)) continue;
                                    var pixel = captured.GetPixel(corner.X, corner.Y);
                                    if (pixel.R < 40 && pixel.G < 40 && pixel.B < 40) issues.Add("native-background-corner\t" + mode.Name + "\tGóc control bị đen");
                                }
                            }
                            if (view.Bounds != new Rectangle(1, 1, form.ClientSize.Width - 2, form.ClientSize.Height - 2)) issues.Add("root-bounds\tFrmMain\tRenderer không phủ đúng client");
                            cases++; if (issues.Count > 0) failed++;
                            foreach (string issue in issues) report.Add(label + "\t" + issue);
                            var reference = Directory.GetFiles(references, screen.ToString("00") + "-*.png").Single();
                            Pair(reference, screenshot, Path.Combine(output, label + "-compare.png"));
                            Console.WriteLine((issues.Count == 0 ? "PASS " : "FAIL ") + label + ": " + issues.Count + " issues; actual client " + form.ClientSize);
                        }
                    }
                    form.ClientSize = new Size(1040, 720);
                    foreach (string state in new[] { "missing-key", "not-looked-up", "zero-detections", "many-engines", "no-file" })
                    {
                        view.SelectScreen(8);
                        var model = Sketch20Samples.Create(8);
                        model.Sample = false; model.KeyConfigured = state != "missing-key";
                        model.VtDetections = model.VtEngineCount = null; model.VtTime = "—"; model.Engines.Clear();
                        if (state == "zero-detections")
                        { model.VtDetections = 0; model.VtEngineCount = 72; model.Engines.Add(new[] { "Engine A", "undetected" }); }
                        if (state == "many-engines")
                        {
                            model.VtDetections = 2; model.VtEngineCount = 72;
                            for (int i = 0; i < 12; i++) model.Engines.Add(new[] { "Engine with a long vendor name " + i, "HEUR: Suspicious.Win32.LongResultName" });
                        }
                        if (state == "no-file") model.Detail = null;
                        view.Model = model; view.Render(); Pump();
                        string label = "vt-" + state;
                        Shot(form, Path.Combine(output, label + ".png"));
                        var issues = Audit(form, view, label, metrics);
                        var query = (Button)view.Canvas.Controls["query-virustotal"];
                        if (state == "missing-key")
                        {
                            query.PerformClick(); Pump();
                            if (view.Page != "settings" || view.SettingsTab != 2) issues.Add("vt-config-action\tVirusTotal\tMissing key does not open API settings");
                        }
                        if (state == "no-file" && query.Enabled) issues.Add("vt-no-file-action\tVirusTotal\tLookup is enabled without a selected file");
                        cases++; if (issues.Count > 0) failed++;
                        foreach (string issue in issues) report.Add(label + "\t" + issue);
                        Console.WriteLine((issues.Count == 0 ? "PASS " : "FAIL ") + label + ": " + issues.Count + " issues; actual client " + form.ClientSize);
                    }
                    form.Close();
                }
                Console.WriteLine("RESULT: " + (cases - failed) + "/" + cases + " Windows FrmMain cases passed; " + failed + " failed. Visual comparison still requires review.");
                return failed == 0 && translatedText ? 0 : 1;
            }
            catch (Exception error) { runtime.Add("fatal=" + error); Console.Error.WriteLine(error); return 2; }
            finally
            {
                Sketch20Style.TraceText = null; dialogs.Stop();
                File.WriteAllLines(Path.Combine(output, "windows-runtime.txt"), runtime, new UTF8Encoding(false));
                File.WriteAllLines(Path.Combine(output, "windows-control-metrics.tsv"), metrics, new UTF8Encoding(false));
                File.WriteAllLines(Path.Combine(output, "windows-issues.tsv"), report, new UTF8Encoding(false));
            }
        }
    }
}
