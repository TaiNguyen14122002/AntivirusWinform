using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScanAndRemoveVirus
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

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

            Application.Run(new FrmMain());
        }

        static void Shot(string path, string view)
        {
            var f = new FrmMain();
            f.Show();
            if (!string.IsNullOrEmpty(view)) f.ShowForShot(view);
            for (int i = 0; i < 40; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(25); }
            using (var bmp = new System.Drawing.Bitmap(f.ClientSize.Width, f.ClientSize.Height))
            {
                f.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            f.Close();
        }
    }
}
