using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScanAndRemoveVirus
{
    internal static class Program
    {
        private static System.Threading.Timer offlineSyncTimer;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            int taskIndex = args == null ? -1 : Array.IndexOf(args, "--schedule-id");
            if (taskIndex >= 0)
            {
                long scheduleId;
                if (taskIndex + 1 >= args.Length || !long.TryParse(args[taskIndex + 1], out scheduleId))
                    Environment.ExitCode = 2;
                else
                {
                    try { Environment.ExitCode = Services.ScanScheduleService.RunSchedule(scheduleId); }
                    catch (Exception ex)
                    {
                        Services.ScheduledScanNoticeStore.Publish(scheduleId, "Lịch #" + scheduleId,
                            "failed", "Không thực hiện được lịch quét: " + ex.Message);
                        Environment.ExitCode = 5;
                    }
                }
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            int sketchAt = args == null ? -1 : Array.IndexOf(args, "--phac-thao");
            if (sketchAt >= 0)
            {
                int screen = 1;
                if (sketchAt + 1 < args.Length) int.TryParse(args[sketchAt + 1], out screen);
                if (screen < 1 || screen > 20) screen = 1;
                Application.Run(new Control.Sketch20PreviewForm(screen));
                return;
            }

            // --shot <file.png>: render giao diện ra ảnh bằng DrawToBitmap rồi thoát.
            // Dùng khi cần ảnh "thật" của app: mọi cách chụp màn hình (CopyFromScreen,
            // PrintWindow, BitBlt từ DC cửa sổ) đều trả ảnh chồng hình trên cửa sổ
            // DWM + double-buffered, không dùng để nghiệm thu giao diện được.
            int shotAt = args == null ? -1 : Array.IndexOf(args, "--shot");
            if (shotAt >= 0 && shotAt + 1 < args.Length)
            {
                // --shot-view <trang>: chụp một trang khác trang chủ (nangcao, chitiet, baove, ...)
                int viewAt = Array.IndexOf(args, "--shot-view");
                string view = viewAt >= 0 && viewAt + 1 < args.Length ? args[viewAt + 1] : null;
                Shot(args[shotAt + 1], view);
                return;
            }

            // Chế độ do HKCU Run kích hoạt: không dừng ở hộp thoại kiểm tra SQL.
            bool launchedFromWindows = args != null && args.Any(arg =>
                string.Equals(arg, "--autostart", StringComparison.OrdinalIgnoreCase));
            offlineSyncTimer = new System.Threading.Timer(_ =>
            {
                try { Services.OfflineHistoryQueue.Replay(50); }
                catch (Exception) { }
            }, null, TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(5));
            try { Application.Run(new FrmMain(launchedFromWindows)); }
            finally
            {
                Control.AdminSecurity.Shutdown();
                if (offlineSyncTimer != null) offlineSyncTimer.Dispose();
            }
        }

        static void Shot(string path, string view)
        {
            var f = new FrmMain();
            f.Show();
            if (!string.IsNullOrEmpty(view)) f.ShowForShot(view);
            for (int i = 0; i < 40; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(25); }
            // ẢNH NGHIỆM THU PHẢI LÀ 24bpp (KHÔNG có kênh alpha):
            //   • Nền của trang do GDI vẽ (WM_ERASEBKGND) KHÔNG ghi kênh alpha;
            //   • Thẻ/nút bo góc do GDI+ FillPath vẽ thì ghi alpha=255.
            // Với bitmap mặc định (32bppArgb) ảnh lưu ra có alpha=0 ở mọi pixel nền nhưng alpha=255
            // ở trong thẻ -> trình xem ảnh ghép trên nền đen (hoặc công cụ chuyển ảnh) sẽ thấy "vùng
            // đen" đúng ở 4 góc thẻ bo góc, dù trên màn hình 4 góc đó chỉ là nền trắng/xanh nhạt.
            // 24bpp ghi đúng màu người dùng nhìn thấy.
            using (var bmp = new System.Drawing.Bitmap(f.ClientSize.Width, f.ClientSize.Height,
                       System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            {
                f.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            f.Close();
        }
    }
}
