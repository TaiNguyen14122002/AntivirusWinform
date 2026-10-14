using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ScanAndRemoveVirus;
using ScanAndRemoveVirus.Control;
using ScanAndRemoveVirus.Services;

static partial class DashboardSmoke
{
    static IEnumerable<System.Windows.Forms.Control> Descendants(System.Windows.Forms.Control root)
    {
        foreach (System.Windows.Forms.Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    static void VerifySketchScreens(FrmMain form, string output)
    {
        foreach (string view in new[] { "baove", "lichsu", "cachly", "caidat" })
        {
            Call(form, "ShowForShot", view);
            Pump();
            var current = Field<UserControl>(form, "currentControl");
            var page = current.Controls.OfType<SketchPage>().Single();
            foreach (var size in new[] { new Size(1040, 660), new Size(1366, 768), new Size(1600, 900) })
            {
                form.ClientSize = size;
                Pump();
                string name = view + " " + size.Width + "x" + size.Height;
                Check(name + ": không cuộn ngang", !page.HorizontalScroll.Visible);
                Check(name + ": nút không bị cắt", Descendants(page.Canvas).OfType<Button>().Where(b => b.Visible).All(b => b.Parent.ClientRectangle.Contains(b.Bounds)));
                Check(name + ": thẻ không tràn chiều ngang", page.Canvas.Controls.OfType<UiCard>().Where(c => c.Visible).All(c => c.Left >= 0 && c.Right <= page.Canvas.Width));
            }
            form.ClientSize = new Size(1366, 829);
            Pump();
            Shot(form, Path.Combine(output, "sketch-" + view + ".png"));
        }
        Call(form, "ShowForShot", "baove");
        var protection = Field<UcBaoVe>(form, "ucBaoVe");
        var switches = Field<Dictionary<string, UiSwitch>>(protection, "sketchSwitches");
        Check("Bảo vệ đủ 10 công tắc chức năng", switches.Count == 10);
        bool original = RealTimeProtection.AutoQuarantine;
        switches["aq"].Checked = !original;
        Pump();
        Check("Công tắc cách ly cập nhật cơ chế thật", RealTimeProtection.AutoQuarantine == !original);
        switches["aq"].Checked = original;
        Pump();
        var settings = Field<UcCaiDat>(form, "ucCaiDat");
        Call(form, "ShowForShot", "caidat");
        var tabs = Field<SketchTabs>(settings, "sketchSettingsTabs");
        var panels = Field<List<UiCard>>(settings, "sketchSettingsCards");
        for (int i = 0; i < panels.Count; i++)
        {
            tabs.SelectedIndex = i;
            Pump();
            Check("Cài đặt tab " + i + ": chỉ hiện đúng nội dung được chọn", panels.Where(p => p.Visible).SequenceEqual(new[] { panels[i] }));
            Shot(form, Path.Combine(output, "sketch-cai-dat-" + i + ".png"));
        }
        tabs.SelectedIndex = 0;
        var startup = Field<CheckBox>(settings, "chkAutoStart");
        var startupSwitch = Descendants(panels[0]).OfType<UiSwitch>().First();
        bool old = startup.Checked;
        startupSwitch.Checked = !old;
        Check("Công tắc cài đặt đồng bộ với dữ liệu lưu", startup.Checked == !old);
        startupSwitch.Checked = old;
        form.MoCaiDatVirusTotal();
        Pump();
        var key = Field<TextBox>(settings, "txtVtKey");
        Check("Điều hướng cấu hình VirusTotal đúng tab", tabs.SelectedIndex == 2 && key.Visible);
        Check("API key được che khi nhập", key.UseSystemPasswordChar);
        var uc = Field<UcTongQuan>(form, "ucTongQuan");
        foreach (CheDoQuetNangCao mode in Enum.GetValues(typeof(CheDoQuetNangCao)))
        {
            Call(form, "ShowForShot", "tongquan");
            Call(uc, "MoQuetNangCao", mode);
            Pump();
            var cards = Field<RadioButton[]>(uc, "theCheDo");
            Check(mode + ": bốn thẻ theo lưới 2x2", cards[0].Top == cards[1].Top && cards[2].Top == cards[3].Top && cards[2].Top > cards[0].Top);
            var scope = Field<Panel>(uc, "pnlNoiDungPhai");
            Check(mode + ": nội dung chế độ không bị che", scope.Controls.Cast<System.Windows.Forms.Control>().Count(c => c.Visible) == 1);
            var scopeGrid = Descendants(scope).OfType<DataGridView>().Single(g => g.Visible);
            Check(mode + ": danh sách phạm vi có đủ chiều cao", scopeGrid.Height >= 120);
            Check(mode + ": lựa chọn không bị cắt", Descendants(scope).OfType<RadioButton>().Where(r => r.Visible).All(r => Within(r, r.Parent)));
            Shot(form, Path.Combine(output, "sketch-quet-" + mode + ".png"));
        }
        Call(form, "ShowForShot", "tongquan");
        Field<Button>(uc, "btnQuayLaiNangCao").PerformClick();
        Pump();
        Shot(form, Path.Combine(output, "sketch-tong-quan.png"));
    }

    static void VerifySketchDetails(FrmMain form, UcTongQuan uc, string output)
    {
        var picker = Field<ComboBox>(uc, "sketchDetailPicker");
        var tabs = Field<TabControl>(uc, "tabChiTietDong");
        var panels = Field<List<System.Windows.Forms.Control>>(uc, "sketchDetailPanels");
        Check("Chi tiết chọn được cả ba tệp phát hiện", picker.Items.Count == 3);
        for (int i = 0; i < 5; i++)
        {
            tabs.SelectedIndex = i;
            Pump();
            var page = Descendants(uc).OfType<SketchPage>().Single();
            Check("Chi tiết tab " + i + ": nội dung hiển thị", Descendants(page).OfType<UiCard>().Any(c => c.Name == "detailPanel" && c.Visible));
            Check("Chi tiết tab " + i + ": nạp đúng dữ liệu", Field<int>(uc, "tabDaNap") == i && panels.Select((p, index) => p.Visible == (index == i)).All(value => value));
            Shot(form, Path.Combine(output, "sketch-chi-tiet-" + i + ".png"));
        }
        tabs.SelectedIndex = 0;
        picker.SelectedIndex = 1;
        Pump();
        Check("Đổi tệp cập nhật thông tin chi tiết", Field<Label>(uc, "lblTtTenTep").Text == picker.SelectedItem.ToString());
    }
}
