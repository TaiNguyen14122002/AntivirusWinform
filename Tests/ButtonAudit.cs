// Audit: MỌI Button trong app phải cùng "hình hài" (Flat + Hand + không system background)
// và font đúng vai: content = Segoe UI 9.75 Bold, sidebar nav = 10.125.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ScanAndRemoveVirus;
using ScanAndRemoveVirus.Control;

static class ButtonAudit
{
    static int bad;

    static void Walk(Control root, string prefix, bool isSidebar)
    {
        foreach (Control c in root.Controls)
        {
            string path = prefix + "/" + c.Name;
            if (c is Button)
            {
                var b = (Button)c;
                var issues = new List<string>();
                if (b.FlatStyle != FlatStyle.Flat) issues.Add("FlatStyle=" + b.FlatStyle);
                if (b.UseVisualStyleBackColor) issues.Add("UseVisualStyleBackColor=true");
                if (b.Cursor != Cursors.Hand) issues.Add("Cursor=" + b.Cursor);
                string want = isSidebar ? "Segoe UI 10.125" : "Segoe UI 9.75";
                if (!b.Font.Name.StartsWith("Segoe UI") || Math.Abs(b.Font.SizeInPoints - float.Parse(want.Split(' ')[2], System.Globalization.CultureInfo.InvariantCulture)) > 0.05f)
                    issues.Add("Font=" + b.Font.Name + " " + b.Font.SizeInPoints);
                // UiButton tự vẽ cả nền lẫn viền bo góc nên FlatAppearance.BorderSize
                // LUÔN bằng 0 (bật lên sẽ bị tô thêm một khung VUÔNG đè lên hình bo góc).
                // Nút thường còn lại vẫn phải giữ viền 1px.
                // Nút "link" (Tag="link") là ngoại lệ CÓ CHỦ Ý: bản thiết kế vẽ "← Quay lại"
                // như một liên kết trơ, không viền không nền — không phải nút quên style.
                bool selfPainted = b is UiButton || "link".Equals(b.Tag as string);
                if (!isSidebar && !selfPainted && b.FlatAppearance.BorderSize != 1) issues.Add("Border=" + b.FlatAppearance.BorderSize);
                if ((isSidebar || selfPainted) && b.FlatAppearance.BorderSize != 0) issues.Add("Border=" + b.FlatAppearance.BorderSize);
                if (issues.Count > 0) { Console.WriteLine("LECH: " + path + " (" + b.GetType().Name + " \"" + b.Text + "\")  [" + string.Join(", ", issues.ToArray()) + "]"); bad++; }
                else Console.WriteLine("ok:   " + path);
            }
            bool childSidebar = isSidebar || c.Name == "pnlSidebar";
            Walk(c, path, childSidebar);
        }
    }

    // STAThread bắt buộc: FrmMain dựng UcTongQuan -> set AllowDrop (OLE) và WinForms
    // ném ThreadStateException ngay khi khởi tạo nếu luồng chính không ở chế độ STA.
    [STAThread]
    static int Main()
    {
        var form = new FrmMain();
        Walk(form, "FrmMain", false);
        // 3 UC còn lại không nằm trong Controls của form (chỉ UC đang mở mới được attach)
        // -> duyệt trực tiếp instance tạo từ ctor của FrmMain
        foreach (string fieldName in new[] { "ucBaoVe", "ucLichSu", "ucCachLy", "ucCaiDat" })
        {
            var uc = (UserControl)typeof(FrmMain)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
            Walk(uc, fieldName, false);
        }
        Console.WriteLine(bad == 0 ? "== ALL BUTTONS UNIFORM ==" : "== " + bad + " BUTTON(S) LECH CHUAN ==");
        return bad == 0 ? 0 : 1;
    }
}
