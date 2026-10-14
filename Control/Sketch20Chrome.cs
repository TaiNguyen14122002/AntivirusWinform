using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    internal static class Sketch20Chrome
    {
        internal static void Attach(Form form, Sketch20View view)
        {
            // Dùng chrome WinForms/Windows thuần: title bar, minimize, maximize,
            // close và resize đều do hệ điều hành xử lý.
            form.FormBorderStyle = FormBorderStyle.Sizable;
            form.MaximizeBox = true;
            form.MinimizeBox = true;
            form.ControlBox = true;
            form.ShowIcon = true;
            form.DoubleBufferedWindow();
            view.WindowForm = form;
        }

        // Giữ API tương thích với renderer cũ. Window controls giờ do Windows vẽ.
        internal static void AddControls(Form form, Sketch20View view, int width)
        {
        }

        private static void DoubleBufferedWindow(this Form form)
        {
            Theme.BatDoubleBuffer(form);
        }
    }
}
