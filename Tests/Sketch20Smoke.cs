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
        Console.OutputEncoding = new System.Text.UTF8Encoding(false);
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        string output = Path.GetFullPath(args.Length > 0 ? args[0] : "Sketch20Verification"); Directory.CreateDirectory(output);
        if (args.Skip(1).Contains("--bridge-only"))
        {
            VerifyBridge(output);
            Console.WriteLine("RESULT: " + (checks - failures) + "/" + checks + " checks passed; " + failures + " failures.");
            return failures == 0 ? 0 : 1;
        }
        int[] heights = { 800, 747, 740, 738, 740, 738, 740, 740, 770, 740, 791, 770, 740, 740, 738, 738, 738, 848, 894, 800 };
        if (args.Skip(1).Contains("--default-size")) for (int i = 0; i < heights.Length; i++) heights[i] = 900;
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
                var viewport = (Panel)typeof(Sketch20View).GetField("viewport", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(view);
                if (i == 8) Check("VirusTotal mở từ đầu trang", viewport.AutoScrollPosition.Y == 0);
                int expectedWidth = form.ClientSize.Width - 2 - 208 - 52 - (viewport.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0);
                Check(i + ": dùng đủ chiều rộng và không cần cuộn ngang", Math.Abs(view.Canvas.Width - expectedWidth) <= 1 && !viewport.HorizontalScroll.Visible);
                foreach (var node in nodes) allNodes.Add(i + "\t" + node.Key + "\t" + node.Value.X + "\t" + node.Value.Y + "\t" + node.Value.Width + "\t" + node.Value.Height);
                using (var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height, PixelFormat.Format24bppRgb))
                {
                    if (Environment.OSVersion.Platform == PlatformID.Unix) using (var g = Graphics.FromImage(bitmap)) g.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, bitmap.Size);
                    else form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(Path.Combine(output, i.ToString("00") + ".png"), ImageFormat.Png);
                }
            }
            form.View.SelectScreen(2); Pump();
            Check("Bảo vệ có đúng 8 thẻ của phác thảo", Sketch20View.FeatureKeys.All(k => form.View.Canvas.Nodes.ContainsKey("feature-" + k)));
            var aq = (Sketch20Switch)form.View.Canvas.Controls["switch-aq"]; aq.Checked = false; Pump();
            Check("Công tắc phác thảo cập nhật model", !form.View.Model.Flag("aq"));
            Check("Cửa sổ dùng thanh tiêu đề WinForms thuần", form.FormBorderStyle == FormBorderStyle.Sizable && form.ControlBox && form.MaximizeBox && form.MinimizeBox);
            form.View.SelectScreen(10); Pump();
            ((TextBox)form.View.Canvas.Controls["string-search"]).Text = "Windows"; Pump();
            Check("Tìm kiếm chuỗi lọc dữ liệu và giữ ô nhập", form.View.Canvas.TextRuns.Count(t => t.Mono && form.View.Model.Strings.Contains(t.Value)) == form.View.Model.Strings.Count(v => v.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) >= 0) && ((TextBox)form.View.Canvas.Controls["string-search"]).Text == "Windows");
            form.View.SelectScreen(8); Pump();
            ((TextBox)form.View.Canvas.Controls["engine-search"]).Text = "Engine B"; Pump();
            Check("Tìm kiếm công cụ bảo mật lọc đúng kết quả", form.View.Canvas.Nodes.Keys.Count(n => n.StartsWith("vt-engine-row-")) == 1);
            form.View.SelectScreen(10); Pump();
            ((TextBox)form.View.Canvas.Controls["string-search"]).Text = ""; Pump();
            form.View.Model.Strings.AddRange(Enumerable.Range(0, 125).Select(i => "extra-string-" + i)); form.View.Render(); Pump();
            var listScroll = form.View.Canvas.Controls["strings-scroll"] as VScrollBar;
            Check("Danh sách chuỗi dài cuộn trong bảng", listScroll != null && form.View.Canvas.TextRuns.Count(t => t.Mono && form.View.Model.Strings.Contains(t.Value)) < form.View.Model.Strings.Count);
            var originalSize = form.ClientSize; form.ClientSize = new Size(1240, 900);
            form.View.SelectScreen(3); Pump();
            form.View.Model.History.AddRange(Enumerable.Range(0, 32).Select(i => new Sketch20History { Index = 100 + i, Type = "Phiên " + i, Date = "05/11/2024", Time = "14:12", Result = "An toàn", Files = "1", Threats = "0", Duration = "1 giây" }));
            form.View.Render(); Pump();
            var historyViewport = (Panel)typeof(Sketch20View).GetField("viewport", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(form.View);
            Check("Lịch sử dài cuộn trong bảng và giữ phân trang trong cửa sổ", form.View.Canvas.Controls["history-scroll"] is VScrollBar && !historyViewport.VerticalScroll.Visible && form.View.Canvas.Controls["history-next"].Bottom < historyViewport.Height - 29);
            form.View.SelectScreen(4); Pump();
            form.View.Model.Quarantine.AddRange(Enumerable.Range(0, 20).Select(i => new Sketch20File { Index = 100 + i, Id = "LONG-" + i, Name = "file-" + i, Path = @"C:\Test\file-" + i, Date = "01/01/2020", Time = "00:00", Severity = "Cao" }));
            form.View.Render(); Pump();
            var quarantineScroll = form.View.Canvas.Controls["quarantine-scroll"] as VScrollBar;
            Check("Cách ly dài giữ thanh xử lý trong cửa sổ", quarantineScroll != null && !historyViewport.VerticalScroll.Visible && form.View.Canvas.Nodes["quarantine-actions"].Bottom < historyViewport.Height - 29);
            if (quarantineScroll != null)
            {
                int lastOffset = quarantineScroll.Maximum - quarantineScroll.LargeChange + 1;
                quarantineScroll.Value = lastOffset;
                typeof(VScrollBar).GetMethod("OnScroll", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(quarantineScroll, new object[] { new ScrollEventArgs(ScrollEventType.ThumbPosition, lastOffset) }); Pump();
                int pickedIndex = -1; form.View.Actions.SelectQuarantine = (index, picked) => { if (picked) pickedIndex = index; };
                ((Sketch20Check)form.View.Canvas.Controls["quarantine-pick-0"]).Checked = true; Pump();
                Check("Chọn tệp sau khi cuộn giữ đúng chỉ số dữ liệu", pickedIndex == form.View.Model.Quarantine[lastOffset].Index);
            }
            form.ClientSize = originalSize;
            form.View.SelectScreen(1); Pump();
            int normalDeviceHeight = form.View.Canvas.Nodes["device-card"].Height;
            ((Button)form.View.Canvas.Controls["advanced-scan"]).PerformClick(); Pump();
            Check("Quét nâng cao mở đúng trang, không nhảy sang Bảo vệ", form.View.Page == "advanced" && form.View.Canvas.TextRuns.Any(t => t.Value == "Quét virus"));
            Check("Quét nâng cao chỉ có 3 chế độ", form.View.Canvas.Controls.OfType<Sketch20Mode>().Select(m => m.Text).SequenceEqual(new[] { "Quét toàn bộ hệ thống", "Quét thư mục", "Quét tệp" }));
            var advancedBack = form.View.Canvas.Controls["page-back"] as Button;
            Check("Quét nâng cao có icon quay lại", advancedBack != null && advancedBack.AccessibleName == "Quay lại màn trước");
            if (advancedBack != null) { advancedBack.PerformClick(); Pump(); Check("Icon quay lại đưa về Tổng quan", form.View.Page == "overview"); }
            form.View.SelectScreen(18); Pump();
            Check("Đang quét không làm thẻ Thiết bị này bị thu nhỏ", form.View.Canvas.Nodes["device-card"].Height == normalDeviceHeight);
            var fixedScanViewport = form.View.Controls.Find("Sketch20Viewport", true).FirstOrDefault() as Panel;
            Check("Màn quét mẫu khóa viewport, không có scrollbar", fixedScanViewport != null && !fixedScanViewport.AutoScroll && !fixedScanViewport.VerticalScroll.Visible);
            for (int scanRender = 0; scanRender < 4; scanRender++) form.View.Render();
            Check("Render lặp khi quét không làm scrollbar bật/tắt", fixedScanViewport != null && !fixedScanViewport.AutoScroll && !fixedScanViewport.VerticalScroll.Visible);
            form.View.SelectScreen(6); Pump();
            ((Sketch20Check)form.View.Canvas.Controls["all-drives"]).Checked = false; Pump();
            Check("Chưa chọn ổ đĩa thì không thể bắt đầu quét", !form.View.Canvas.Controls["start-advanced"].Enabled);
            form.View.SelectScreen(16); Pump();
            Check("Quét tùy chỉnh đã được loại khỏi ba thẻ", !form.View.Canvas.Controls.OfType<Sketch20Mode>().Any(m => m.Text == "Quét tùy chỉnh"));
            Check("Quét tệp có đúng ba thẻ lựa chọn", form.View.Canvas.Controls.OfType<Sketch20Mode>().Count() == 3 && form.View.AdvancedMode == 2);
            Check("Bắt đầu quét nằm trong thẻ phạm vi", form.View.Canvas.Nodes["scan-scope"].Contains(form.View.Canvas.Nodes["start-advanced"]));
            form.View.SelectScreen(17); Pump();
            Check("Màn Quét tùy chỉnh cũ chuyển về Quét toàn bộ", form.View.AdvancedMode == 0 && form.View.Canvas.Controls.OfType<Sketch20Mode>().Count() == 3);
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
        VerifyInteractions(output);
        if (!args.Skip(1).Contains("--visual-only")) VerifyBridge(output);
        File.WriteAllLines(Path.Combine(output, "layout.tsv"), allNodes);
        Console.WriteLine("RESULT: " + (checks - failures) + "/" + checks + " checks passed; " + failures + " failures.");
        return failures == 0 ? 0 : 1;
    }
}
