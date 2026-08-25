using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScanAndRemoveVirus.Services;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    public partial class UcTongQuan : UserControl
    {
        private string customScanPath;
        private bool isScanning;
        private CancellationTokenSource cts;
        // File lưu thời điểm cập nhật CSDL: %AppData%\ScanAndRemoveVirus\dbupdate.txt
        private static string StatePath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ScanAndRemoveVirus", "dbupdate.txt");
            }
        }

        public UcTongQuan()
        {
            InitializeComponent();
            btnCheckUpdate.Click += BtnCheckUpdate_Click;
            btnScanNow.Click += BtnScanNow_Click;
            // 3 radio nằm ở 3 container khác nhau -> WinForms không tự loại trừ, phải tự xử lý
            rdoQuickScan.CheckedChanged += ScanTypeChanged;
            rdoFullScan.CheckedChanged += ScanTypeChanged;
            rdoCustomScan.CheckedChanged += ScanTypeChanged;
            LoadProtectionStatus();
            btnQuarantineSelected.Click += BtnQuarantineSelected_Click;
            btnDeleteSelected.Click += BtnDeleteSelected_Click;
            btnQuarantineAll.Click += BtnQuarantineAll_Click;
            btnDeleteAll.Click += BtnDeleteAll_Click;
        }

        private void ScanTypeChanged(object sender, EventArgs e)
        {
            var selected = (RadioButton)sender;
            if (!selected.Checked) return;
            foreach (RadioButton other in new[] { rdoQuickScan, rdoFullScan, rdoCustomScan })
                if (!ReferenceEquals(other, selected) && other.Checked)
                    other.Checked = false;
        }

        private ScanType GetSelectedScanType()
        {
            if (rdoFullScan.Checked) return ScanType.Full;
            if (rdoCustomScan.Checked) return ScanType.Custom;
            return ScanType.Quick;
        }

        private static string DescribeScan(ScanType type)
        {
            switch (type)
            {
                case ScanType.Full: return "Quét toàn bộ";
                case ScanType.Custom: return "Quét tùy chọn";
                default: return "Quét nhanh";
            }
        }

        // Nút Quét ngay kiêm nút Hủy khi đang quét
        private async void BtnScanNow_Click(object sender, EventArgs e)
        {
            if (isScanning)
            {
                cts.Cancel();
                return;
            }

            ScanType type = GetSelectedScanType();
            string path = customScanPath;
            if (type == ScanType.Custom && string.IsNullOrEmpty(path))
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "Chọn thư mục cần quét";
                    if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                    path = dlg.SelectedPath;
                    customScanPath = path;
                }
            }

            SetScanning(true);
            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            try
            {
                ScanResult result = await Task.Run(() =>
                    ScanEngine.Scan(type, path, token, (count, current) =>
                    {
                        try
                        {
                            BeginInvoke((MethodInvoker)(() => lblScannedCount.Text = count.ToString("N0")));
                        }
                        catch (ObjectDisposedException) { } // form đã đóng giữa chừng
                        catch (InvalidOperationException) { }
                    }));

                lblScannedCount.Text = result.FilesScanned.ToString("N0");
                LoadDetectedThreats(result);
                lblLastScanDate.Text = DateTime.Now.ToString("dd/MM/yyyy  HH:mm");
                lblLastScanType.Text = DescribeScan(type);
                MessageBox.Show(
                    result.HasThreats
                        ? string.Format("Phát hiện {0} mối đe dọa trong {1:N0} tệp. Hãy xử lý trong khu vực \"Hành động\" bên dưới.",
                            result.Threats.Count, result.FilesScanned)
                        : string.Format("Đã quét {0:N0} tệp, không phát hiện mối đe dọa.", result.FilesScanned),
                    DescribeScan(type) + " hoàn tất",
                    MessageBoxButtons.OK,
                    result.HasThreats ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Đã hủy phiên quét.", DescribeScan(type),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                SetScanning(false);
            }
        }

        private void SetScanning(bool scanning)
        {
            isScanning = scanning;
            btnScanNow.Text = scanning ? "Hủy quét" : "Quét ngay";
        }

        private void LoadDetectedThreats(ScanResult result)
        {
            dgvActions.Rows.Clear();
            foreach (ThreatFound threat in result.Threats)
                dgvActions.Rows.Add(threat.FilePath, threat.Reason);
            UpdateThreatUi();
        }

        private void UpdateThreatUi()
        {
            bool hasRows = dgvActions.Rows.Count > 0;
            lblThreatCount.Text = dgvActions.Rows.Count.ToString();
            lblThreatText.Text = hasRows ? "Cần xử lý" : "Không phát hiện mối đe dọa";
            btnQuarantineSelected.Enabled = hasRows;
            btnDeleteSelected.Enabled = hasRows;
            btnQuarantineAll.Enabled = hasRows;
            btnDeleteAll.Enabled = hasRows;
        }

        private void BtnQuarantineSelected_Click(object sender, EventArgs e)
        {
            if (dgvActions.SelectedRows.Count == 0) return;
            QuarantineRows(dgvActions.SelectedRows.Cast<DataGridViewRow>().ToList());
        }

        private void BtnQuarantineAll_Click(object sender, EventArgs e)
        {
            QuarantineRows(dgvActions.Rows.Cast<DataGridViewRow>().ToList());
        }

        private void QuarantineRows(List<DataGridViewRow> rows)
        {
            if (rows.Count == 0) return;
            int done = 0;
            foreach (DataGridViewRow row in rows)
            {
                string path = row.Cells[0].Value as string;
                if (string.IsNullOrEmpty(path) || !File.Exists(path) || ScanEngine.Quarantine(path))
                {
                    dgvActions.Rows.Remove(row);
                    done++;
                }
            }
            UpdateThreatUi();
            MessageBox.Show(string.Format("Đã cách ly {0}/{1} tệp.", done, rows.Count),
                "Cách ly", MessageBoxButtons.OK,
                done == rows.Count ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void BtnDeleteSelected_Click(object sender, EventArgs e)
        {
            if (dgvActions.SelectedRows.Count == 0) return;
            DeleteRows(dgvActions.SelectedRows.Cast<DataGridViewRow>().ToList());
        }

        private void BtnDeleteAll_Click(object sender, EventArgs e)
        {
            DeleteRows(dgvActions.Rows.Cast<DataGridViewRow>().ToList());
        }

        private void DeleteRows(List<DataGridViewRow> rows)
        {
            if (rows.Count == 0) return;
            DialogResult answer = MessageBox.Show(
                string.Format("Xóa vĩnh viễn {0} tệp khỏi máy tính? Hành động này không thể hoàn tác.", rows.Count),
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;

            int done = 0;
            foreach (DataGridViewRow row in rows)
            {
                string path = row.Cells[0].Value as string;
                try
                {
                    if (string.IsNullOrEmpty(path)) { dgvActions.Rows.Remove(row); continue; }
                    if (!File.Exists(path)) { dgvActions.Rows.Remove(row); done++; continue; }
                    File.Delete(path);
                    done++;
                    dgvActions.Rows.Remove(row);
                }
                catch (Exception) { } // tệp đang bị khoá: giữ dòng lại để thử lại sau
            }
            UpdateThreatUi();
            MessageBox.Show(string.Format("Đã xóa {0}/{1} tệp.", done, rows.Count),
                "Xóa tệp", MessageBoxButtons.OK,
                done == rows.Count ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void LoadProtectionStatus()
        {
            lblVersionValue.Text = System.Reflection.Assembly
                .GetExecutingAssembly().GetName().Version.ToString(3);

            DateTime last;
            if (File.Exists(StatePath) && DateTime.TryParse(File.ReadAllText(StatePath), out last))
                MarkDbUpdated(last);
            else
                lblLastUpdateTitle.Text = "Chưa cập nhật";
        }

        private void BtnCheckUpdate_Click(object sender, EventArgs e)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath));
            DateTime now = DateTime.Now;
            File.WriteAllText(StatePath, now.ToString("o"));
            MarkDbUpdated(now);
            MessageBox.Show("Cơ sở dữ liệu virus đã được cập nhật.",
                "Kiểm tra cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MarkDbUpdated(DateTime when)
        {
            lblDatabaseValue.Text = "Đã cập nhật";
            lblDatabaseValue.ForeColor = Color.FromArgb(22, 163, 74);
            lblLastUpdateTitle.Text = when.ToString("dd/MM/yyyy HH:mm");
        }

        private void lblQuarantineCount_Click(object sender, EventArgs e)
        {

        }
    }
}
