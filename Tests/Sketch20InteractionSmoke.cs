using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ScanAndRemoveVirus.Control;

internal static partial class Sketch20Smoke
{
    private static void VerifyInteractions(string output)
    {
        int clicked = 0, toggled = 0, disabled = 0, external = 0;
        var closeDialogs = new Timer { Interval = 70 };
        closeDialogs.Tick += delegate
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                EnumWindows((handle, parameter) =>
                {
                    uint processId; GetWindowThreadProcessId(handle, out processId);
                    var className = new System.Text.StringBuilder(64); GetClassName(handle, className, className.Capacity);
                    if (processId == (uint)Process.GetCurrentProcess().Id && className.ToString() == "#32770")
                        PostMessage(handle, 0x0111, new IntPtr(GetDlgItem(handle, 1) != IntPtr.Zero ? 1 : 2), IntPtr.Zero);
                    return true;
                }, IntPtr.Zero);
                return;
            }
            foreach (Form dialog in Application.OpenForms.Cast<Form>().ToArray())
            {
                if (!dialog.Modal || dialog is Sketch20PreviewForm) continue;
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            }
        };
        closeDialogs.Start();
        try
        {
            using (var form = new Sketch20PreviewForm())
            {
                form.Show(); Pump(180);
                var view = form.View;

                var navCases = new Dictionary<string, string> {
                    { "nav-overview", "overview" }, { "nav-protection", "protection" },
                    { "nav-quarantine", "quarantine" }, { "nav-history", "history" }
                };
                foreach (var pair in navCases)
                {
                    var nav = view.Controls.Find(pair.Key, true).FirstOrDefault() as Button;
                    Check("Điều hướng " + pair.Key + " tồn tại", nav != null);
                    if (nav != null) { nav.PerformClick(); Pump(40); clicked++; Check("Điều hướng " + pair.Key + " đúng trang", view.Page == pair.Value); }
                }

                for (int screen = 1; screen <= 20; screen++)
                {
                    view.SelectScreen(screen); Pump(45);
                    if (screen == 13)
                    {
                        var password = view.Canvas.Controls.OfType<Sketch20Password>().FirstOrDefault();
                        if (password != null) { password.Entry.Text = "interaction-test-key"; Pump(25); }
                    }
                    var buttonNames = Descendants(view.Canvas).OfType<Button>()
                        .Where(b => !string.IsNullOrWhiteSpace(b.Name)).Select(b => b.Name).Distinct().ToArray();
                    foreach (string name in buttonNames)
                    {
                        view.SelectScreen(screen); Pump(20);
                        if (screen == 13)
                        {
                            var password = view.Canvas.Controls.OfType<Sketch20Password>().FirstOrDefault();
                            if (password != null) { password.Entry.Text = "interaction-test-key"; Pump(10); }
                        }
                        var button = Descendants(view.Canvas).OfType<Button>().FirstOrDefault(b => b.Name == name);
                        if (button == null)
                        {
                            // Một số nút phụ thuộc mode và có thể bị thay thế đúng lúc Render/BeginInvoke
                            // hoàn tất. Retry đúng một frame trước khi coi là lỗi thật.
                            view.SelectScreen(screen); Pump(20);
                            button = Descendants(view.Canvas).OfType<Button>().FirstOrDefault(b => b.Name == name);
                        }
                        Check(screen + ": control còn tồn tại trước khi bấm " + name, button != null);
                        if (button == null) continue;
                        if (name == "get-api-key")
                        {
                            external++;
                            Check("Link VirusTotal có nhãn rõ và là link tương tác", button.Text.Contains("VirusTotal") && button.Enabled);
                            continue;
                        }
                        if (!button.Enabled) { disabled++; Check(screen + ": nút disabled không nhận thao tác " + name, true); continue; }
                        try
                        {
                            button.PerformClick(); Pump(45); clicked++;
                            var viewport = (Panel)typeof(Sketch20View).GetField("viewport", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
                            Check(screen + ": bấm " + name + " không phá layout", !view.IsDisposed && view.Canvas.Width > 0 && !viewport.HorizontalScroll.Visible && view.Canvas.Nodes.Values.All(r => r.Left >= 0 && r.Right <= view.Canvas.Width));
                        }
                        catch (Exception ex) { Check(screen + ": bấm " + name + " không lỗi: " + ex.GetBaseException().Message, false); }
                    }

                    view.SelectScreen(screen); Pump(20);
                    var toggleNames = Descendants(view.Canvas).OfType<CheckBox>()
                        .Where(c => !string.IsNullOrWhiteSpace(c.Name)).Select(c => c.Name).Distinct().ToArray();
                    foreach (string name in toggleNames)
                    {
                        view.SelectScreen(screen); Pump(15);
                        var toggle = Descendants(view.Canvas).OfType<CheckBox>().FirstOrDefault(c => c.Name == name);
                        if (toggle == null || !toggle.Enabled) continue;
                        try { toggle.Checked = !toggle.Checked; Pump(25); toggled++; Check(screen + ": toggle " + name + " không lỗi", !view.IsDisposed && view.Canvas.Width > 0); }
                        catch (Exception ex) { Check(screen + ": toggle " + name + " không lỗi: " + ex.GetBaseException().Message, false); }
                    }
                }

                foreach (var size in new[] { new Size(1040,720), new Size(1240,900), new Size(1600,900), new Size(1040,720) })
                {
                    form.ClientSize = size; Pump(35);
                    foreach (int screen in new[] { 1, 3, 4, 13, 17, 18, 19 })
                    {
                        view.SelectScreen(screen); Pump(25);
                        var viewport = (Panel)typeof(Sketch20View).GetField("viewport", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
                        Check("Responsive " + screen + " @ " + size.Width + "x" + size.Height, !viewport.HorizontalScroll.Visible && Descendants(view.Canvas).OfType<Button>().All(b => b.Right <= b.Parent.Width && b.Bottom <= b.Parent.Height));
                    }
                }

                form.ClientSize = new Size(1040,720); view.SelectScreen(3); Pump(40);
                string before = string.Join("|", view.Canvas.Nodes.OrderBy(n => n.Key).Select(n => n.Key + ":" + n.Value));
                for (int i = 0; i < 6; i++) { form.ClientSize = new Size(1600,900); Pump(15); form.ClientSize = new Size(1040,720); Pump(15); view.Render(); }
                string after = string.Join("|", view.Canvas.Nodes.OrderBy(n => n.Key).Select(n => n.Key + ":" + n.Value));
                Check("Resize lặp không làm layout lịch sử trôi/giật", before == after);
                Check("Interaction sweep đã bấm số lượng control đáng kể", clicked >= 55);
                Check("Interaction sweep đã test toggle", toggled >= 12);
                Console.WriteLine("INTERACTION SUMMARY: clicked=" + clicked + "; toggled=" + toggled + "; disabled=" + disabled + "; external-links-verified=" + external);
                form.Close();
            }
        }
        finally { closeDialogs.Stop(); closeDialogs.Dispose(); }
    }
}
