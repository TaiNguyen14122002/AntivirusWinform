// Chạy control WinForms thật; SQL bị vô hiệu CHỈ trong tiến trình test.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ScanAndRemoveVirus;
using ScanAndRemoveVirus.Control;
using ScanAndRemoveVirus.Database;
using ScanAndRemoveVirus.Services;

static partial class DashboardSmoke
{
    static int failures;
    static int checks;
    static readonly BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr handle, StringBuilder value, int length);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

    static T Field<T>(object o, string name)
    {
        return (T)o.GetType().GetField(name, Hidden).GetValue(o);
    }

    static object Call(object o, string name, params object[] args)
    {
        return o.GetType().GetMethod(name, Hidden).Invoke(o, args);
    }

    static void Check(string name, bool passed)
    {
        checks++;
        if (!passed) failures++;
        Console.WriteLine((passed ? "PASS: " : "FAIL: ") + name);
    }

    static void Pump(int milliseconds = 180)
    {
        var timer = Stopwatch.StartNew();
        while (timer.ElapsedMilliseconds < milliseconds)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }

    static bool Within(System.Windows.Forms.Control child, System.Windows.Forms.Control parent)
    {
        var top = parent.PointToClient(child.PointToScreen(Point.Empty));
        return parent.ClientRectangle.Contains(new Rectangle(top, child.Size));
    }

    static void Shot(Form form, string path)
    {
        using (var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height, PixelFormat.Format24bppRgb))
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // Mono không render đệ quy Form.DrawToBitmap; chụp bề mặt Xvfb thật.
                using (var graphics = Graphics.FromImage(bitmap))
                    graphics.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, bitmap.Size);
            }
            else form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(path, ImageFormat.Png);
        }
    }

    static void CloseTestForm(Form form)
    {
        // Mono 6.8 tạo DoubleBuffer cho một số control ẩn nhưng chưa cấp native drawable.
        // Finalizer của nó gọi DestroyOffscreenDrawable(null). Chỉ bỏ finalizer cho buffer
        // chưa có tài nguyên; các buffer đã render vẫn được Dispose bình thường.
        // Đây là tương thích của runner Linux, không thay đổi mã/chức năng ứng dụng.
        var controls = new HashSet<System.Windows.Forms.Control>();
        Action<System.Windows.Forms.Control> collect = null;
        collect = delegate(System.Windows.Forms.Control control)
        {
            if (control == null || !controls.Add(control)) return;
            foreach (System.Windows.Forms.Control child in control.Controls) collect(child);
            var strip = control as ToolStrip;
            if (strip != null)
                foreach (ToolStripItem item in strip.Items)
                {
                    var host = item as ToolStripControlHost;
                    if (host != null) collect(host.Control);
                    var dropdown = item as ToolStripDropDownItem;
                    if (dropdown != null && dropdown.HasDropDownItems) collect(dropdown.DropDown);
                }
            // Các field của màn có thể giữ control cũ đã được chuyển ra khỏi cây hiển thị.
            for (Type type = control.GetType(); type != null && type != typeof(object); type = type.BaseType)
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (typeof(System.Windows.Forms.Control).IsAssignableFrom(field.FieldType))
                        collect(field.GetValue(control) as System.Windows.Forms.Control);
                    else if (typeof(System.Windows.Forms.Control[]).IsAssignableFrom(field.FieldType))
                    {
                        var array = field.GetValue(control) as System.Windows.Forms.Control[];
                        if (array != null) foreach (var item in array) collect(item);
                    }
                }
        };
        collect(form);
        foreach (FieldInfo field in form.GetType().GetFields(Hidden))
            if (typeof(UserControl).IsAssignableFrom(field.FieldType))
                collect(field.GetValue(form) as System.Windows.Forms.Control);
        Action suppressEmptyMonoBuffers = delegate
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix) return;
            FieldInfo bufferField = typeof(System.Windows.Forms.Control).GetField("backbuffer", Hidden);
            if (bufferField == null) return;
            foreach (System.Windows.Forms.Control control in controls)
            {
                object buffer = bufferField.GetValue(control);
                if (buffer == null) continue;
                FieldInfo drawable = buffer.GetType().GetField("back_buffer", Hidden);
                if (drawable != null && drawable.GetValue(buffer) == null)
                {
                    GC.SuppressFinalize(buffer);
                    bufferField.SetValue(control, null);
                }
            }
        };
        suppressEmptyMonoBuffers();
        form.Close();
        suppressEmptyMonoBuffers();
    }

    [STAThread]
    static int Main(string[] args)
    {
        int result = Run(args);
        // Hoàn tất finalizer GDI của Mono khi X display vẫn còn hoạt động.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return result;
    }

    static int Run(string[] args)
    {
        string output = Path.GetFullPath(args.Length > 0 ? args[0] : "DashboardVerification");
        Directory.CreateDirectory(output);
        string data = Path.Combine(output, "isolated-data");
        Directory.CreateDirectory(data);
        Environment.SetEnvironmentVariable("XVIRUS_DATA_DIR", data);
        // Sai keyword khiến SqlClient fail ngay, không gọi SQL Server hay sửa dữ liệu của người dùng.
        typeof(DbConnection).GetField("ConnectionString", BindingFlags.NonPublic | BindingFlags.Static)
            .SetValue(null, "DashboardSmokeIsolation=true");
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
        {
            Check("Không có lỗi UI ngoài dự kiến: " + e.Exception.Message, false);
        };
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // MessageBox hoàn tất phiên quét được đóng bằng UI timer, không cần user32.dll.
        var closeDialogs = new System.Windows.Forms.Timer { Interval = 100 };
        closeDialogs.Tick += delegate
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                IntPtr handle = GetForegroundWindow();
                uint processId;
                GetWindowThreadProcessId(handle, out processId);
                var name = new StringBuilder(64);
                GetClassName(handle, name, name.Capacity);
                if (processId == (uint)Process.GetCurrentProcess().Id && name.ToString() == "#32770")
                    PostMessage(handle, 0x0111, new IntPtr(1), IntPtr.Zero);
                return;
            }
            foreach (Form dialog in Application.OpenForms.Cast<Form>().ToArray())
            {
                if (!dialog.Modal || dialog is FrmMain) continue;
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            }
        };
        closeDialogs.Start();

        try
        {
            using (var form = new FrmMain())
            {
                form.Show();
                Pump();
                var uc = Field<UcTongQuan>(form, "ucTongQuan");
                var head = Field<Panel>(uc, "pnlPageHead");
                var hero = Field<Panel>(uc, "pnlHeroHost");
                var stats = Field<TableLayoutPanel>(uc, "pnlThongKe");
                var scan = Field<UiButton>(uc, "btnScanNow");
                var advanced = Field<UiButton>(uc, "btnQuetNangCao");
                var activity = Field<UiCard>(uc, "grpActivity");
                Check("Header dock Top, không phủ nội dung", head.Dock == DockStyle.Top && head.Height == 84);
                Check("Ba thẻ thống kê hiển thị", stats.ColumnCount == 3 && stats.Visible);
                Check("Empty state hiển thị khi chưa có lịch sử", Field<Label>(uc, "lblEmptyActivity").Visible);
                Check("Không có dải loading khi chưa quét", !Field<Panel>(uc, "pnlLoadingQuet").Visible);

                foreach (Size size in new[] { new Size(1414, 829), new Size(1366, 768),
                    new Size(1120, 720), new Size(1040, 660), new Size(1600, 900) })
                {
                    form.ClientSize = size;
                    Pump();
                    string tag = size.Width + "x" + size.Height;
                    Check(tag + ": không cuộn ngang", !uc.HorizontalScroll.Visible);
                    Check(tag + ": nút quét nằm trọn trong hero", Within(scan, hero) && Within(advanced, hero));
                    Check(tag + ": các thẻ không chồng nhau", stats.Controls.Cast<System.Windows.Forms.Control>()
                        .SelectMany((a, i) => stats.Controls.Cast<System.Windows.Forms.Control>().Skip(i + 1)
                            .Select(b => !a.Bounds.IntersectsWith(b.Bounds))).All(x => x));
                    Check(tag + ": có chỗ cho danh sách hoạt động", activity.Height >= 145);
                    Shot(form, Path.Combine(output, "tong-quan-" + tag + ".png"));
                }

                form.ClientSize = new Size(1366, 768);
                Call(uc, "ApplyRealTimeStatus", false);
                Pump();
                Check("Bảo vệ tắt hiển thị cảnh báo đúng", Field<Label>(uc, "lblProtectionStatus").ForeColor == Theme.Amber);
                Shot(form, Path.Combine(output, "tong-quan-bao-ve-tat.png"));
                Call(uc, "ApplyRealTimeStatus", true);
                Pump();
                Check("Bảo vệ bật hiển thị trạng thái xanh", Field<Label>(uc, "lblProtectionStatus").ForeColor == Theme.Green);
                Shot(form, Path.Combine(output, "tong-quan-an-toan.png"));

                advanced.PerformClick();
                Pump();
                var menu = Field<ContextMenuStrip>(uc, "ctxQuetNangCao");
                Check("Nút nâng cao mở đủ bốn chế độ", menu.Visible && menu.Items.Count == 4);
                menu.Close();
                foreach (CheDoQuetNangCao mode in Enum.GetValues(typeof(CheDoQuetNangCao)))
                {
                    Call(uc, "MoQuetNangCao", mode);
                    Pump();
                    Check(mode + ": mở màn nâng cao và ẩn thống kê", Field<Panel>(uc, "pnlQuetNangCao").Visible && !stats.Visible);
                }
                Call(uc, "HienThi", TongQuanView.AnToan, false);
                Pump();
                Check("Quay lại tổng quan khôi phục ba thẻ", stats.Visible && head.Visible);

                string samples = Path.Combine(output, "scan-samples");
                Directory.CreateDirectory(samples);
                File.WriteAllText(Path.Combine(samples, "clean.txt"), "hello world");
                File.WriteAllText(Path.Combine(samples, "signature.txt"), ScanEngine.TestSignature + "sample");
                File.WriteAllText(Path.Combine(samples, "demo_eicar.bin"), "harmless sample");
                File.WriteAllText(Path.Combine(samples, "invoice.pdf.exe"), "harmless double-extension sample");
                var result = ScanEngine.Scan(ScanType.Custom, samples, CancellationToken.None);
                Check("Engine quét đủ bốn tệp test", result.FilesScanned == 4);
                Check("Engine phát hiện hai chữ ký và một heuristic", result.Threats.Count == 3
                    && result.Threats.Count(t => t.Kind == "Chữ ký") == 2
                    && result.Threats.Count(t => t.Kind == "Heuristic") == 1);
                Check("Tệp sạch không bị báo nhầm", !result.Threats.Any(t => Path.GetFileName(t.FilePath) == "clean.txt"));
                Call(uc, "SetCustomPath", samples);
                scan.PerformClick();
                Check("Nút Quét ngay bật loading ngay lập tức", Field<bool>(uc, "isScanning") && Field<Panel>(uc, "pnlLoadingQuet").Visible);
                Shot(form, Path.Combine(output, "tong-quan-dang-quet.png"));
                var waiting = Stopwatch.StartNew();
                while (Field<bool>(uc, "isScanning") && waiting.ElapsedMilliseconds < 15000) Pump(50);
                Pump();
                Check("Phiên quét hoàn tất và tắt loading", !Field<bool>(uc, "isScanning") && !Field<Panel>(uc, "pnlLoadingQuet").Visible);
                Check("Kết quả cập nhật đủ tệp", Field<Label>(uc, "lblScannedCount").Text == "4");
                Check("Tổng quan chuyển sang cảnh báo ba đe dọa", Field<Panel>(uc, "pnlPhatHienDeDoa").Visible
                    && Field<DataGridView>(uc, "dgvActions").Rows.Count == 3 && Field<Label>(uc, "lblThreatCount").Text == "3");
                Check("Kết quả phiên hiện tại còn hiển thị khi SQL không khả dụng", Field<Label>(uc, "lblLastScanDate").Text != "Chưa quét");
                Check("Hoạt động phiên hiện tại hiển thị", Field<DataGridView>(uc, "dgvActivity").Rows.Count == 1);
                Shot(form, Path.Combine(output, "tong-quan-phat-hien.png"));
                var grid = Field<DataGridView>(uc, "dgvActions");
                grid.Rows[0].Cells[0].Value = true;
                Call(uc, "UpdateThreatUi");
                Check("Chọn đe dọa bật nút xử lý", Field<Button>(uc, "btnQuarantineSelected").Enabled && Field<Button>(uc, "btnDeleteSelected").Enabled);
                var headerClick = new DataGridViewCellMouseEventArgs(0, -1, 7, 7,
                    new MouseEventArgs(MouseButtons.Left, 1, 7, 7, 0));
                typeof(DataGridView).GetMethod("OnColumnHeaderMouseClick", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(grid, new object[] { headerClick });
                Check("Header checkbox bỏ chọn tất cả", grid.Rows.Cast<DataGridViewRow>().All(r => !Convert.ToBoolean(r.Cells[0].Value)));
                typeof(DataGridView).GetMethod("OnColumnHeaderMouseClick", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(grid, new object[] { headerClick });
                Check("Header checkbox chọn tất cả", grid.Rows.Cast<DataGridViewRow>().All(r => Convert.ToBoolean(r.Cells[0].Value)));
                Call(uc, "MoChiTietKetQua", 0);
                Pump();
                Check("Trang chi tiết ẩn hero/thống kê", Field<Panel>(uc, "pnlChiTietKetQua").Visible && !stats.Visible && !head.Visible);
                Shot(form, Path.Combine(output, "chi-tiet-ket-qua.png"));
                VerifySketchDetails(form, uc, output);
                Call(uc, "HienThi", TongQuanView.PhatHienDeDoa, false);
                Pump();
                Check("Quay về giữ ba đe dọa", stats.Visible && grid.Rows.Count == 3);

                // Một phiên quét sạch đi qua cùng nút thật, trở về trạng thái an toàn.
                string cleanFolder = Path.Combine(output, "clean-samples");
                Directory.CreateDirectory(cleanFolder);
                File.WriteAllText(Path.Combine(cleanFolder, "notes.txt"), "A clean document.");
                Call(uc, "HienThi", TongQuanView.AnToan, false);
                Call(uc, "SetCustomPath", cleanFolder);
                scan.PerformClick();
                waiting.Restart();
                while (Field<bool>(uc, "isScanning") && waiting.ElapsedMilliseconds < 15000) Pump(50);
                Pump();
                Check("Phiên sạch trở về an toàn và xóa kết quả cũ", grid.Rows.Count == 0
                    && Field<Panel>(uc, "pnlAnToan").Visible && Field<Label>(uc, "lblThreatCount").Text == "0");
                Call(uc, "ApplyRealTimeStatus", true);
                Pump();
                Shot(form, Path.Combine(output, "tong-quan-final.png"));

                string cancellationFolder = Path.Combine(output, "cancel-samples");
                Directory.CreateDirectory(cancellationFolder);
                for (int i = 0; i < 600; i++)
                    File.WriteAllText(Path.Combine(cancellationFolder, "notes-" + i + ".txt"), new string('x', 4096));
                Call(uc, "SetCustomPath", cancellationFolder);
                scan.PerformClick();
                Field<Button>(uc, "btnHuyQuetLoading").PerformClick();
                waiting.Restart();
                while (Field<bool>(uc, "isScanning") && waiting.ElapsedMilliseconds < 15000) Pump(50);
                Pump();
                Check("Nút hủy dừng phiên quét và tắt loading", !Field<bool>(uc, "isScanning")
                    && !Field<Panel>(uc, "pnlLoadingQuet").Visible
                    && Field<Label>(uc, "lblScanProgress").Text == "Đã hủy phiên quét."
                    && Field<Label>(uc, "lblScanProgress").ForeColor == Theme.Amber);
                Check("Sau khi hủy có thể chọn quét nâng cao", advanced.Enabled && scan.Text == "Quét ngay");
                VerifySketchScreens(form, output);
                CloseTestForm(form);
            }
        }
        catch (Exception ex)
        {
            failures++;
            Console.WriteLine("FAIL: exception\n" + ex);
        }
        finally
        {
            closeDialogs.Stop();
            closeDialogs.Dispose();
            foreach (string key in new[] { "usb", "download", "startup", "behavior" })
                GuardService.Configure(key, false);
            RealTimeProtection.Stop();
        }
        Console.WriteLine("RESULT: " + (checks - failures) + "/" + checks + " checks passed; " + failures + " failures.");
        return failures == 0 ? 0 : 1;
    }
}
