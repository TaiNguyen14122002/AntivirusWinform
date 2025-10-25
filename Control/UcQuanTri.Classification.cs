using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ScanAndRemoveVirus.Database;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>
    /// Gán Category cho chữ ký chưa phân loại, có xác nhận bằng chứng của quản trị viên.
    /// Không biến UserVerified/UserSuspected (trạng thái duyệt) thành loại mã độc.
    /// Chỉ cập nhật cột Category/UpdatedAt đã có trong dbo.VirusSignatures.
    /// </summary>
    public sealed partial class UcQuanTri
    {
        private static readonly string[] StandardThreatCategories = {
            "Trojan", "Ransomware", "Spyware", "Adware", "Backdoor", "Downloader",
            "Dropper", "Stealer", "Keylogger", "Worm", "Virus", "Rootkit",
            "Miner", "HackTool", "Riskware", "PUA/PUP", "Exploit", "Botnet",
            "Banker", "Wiper"
        };

        private void ManageUnclassifiedSignatures()
        {
            if (!RequireAdmin()) return;
            using (var dialog = new Form {
                Text = "Phân loại chữ ký mã độc - Quản trị",
                StartPosition = FormStartPosition.CenterParent,
                MinimumSize = new Size(790, 495), ClientSize = new Size(1010, 660),
                BackColor = Color.FromArgb(246, 249, 253), Font = new Font(Theme.UiFontFamily, 10.65f)
            })
            {
                var heading = new Label {
                    Dock = DockStyle.Top, Height = 74, Padding = new Padding(15, 8, 15, 4),
                    ForeColor = Ink, Font = new Font(Theme.UiFontFamily, 10.65f),
                    Text = "Chữ ký chưa có LOẠI mã độc\r\n" +
                           "UserVerified và Suspected là trạng thái kiểm duyệt, không phải loại mã độc. Chỉ gán loại khi có bằng chứng."
                };
                var bottom = new FlowLayoutPanel {
                    Dock = DockStyle.Bottom, Height = 58, FlowDirection = FlowDirection.RightToLeft,
                    BackColor = Color.White, Padding = new Padding(12, 7, 12, 6)
                };
                var close = new Button { Text = "Đóng", Width = 94, Height = 36, DialogResult = DialogResult.Cancel };
                var classify = new Button { Text = "Gán loại đã xác minh", Width = 190, Height = 36,
                    BackColor = Color.FromArgb(33, 103, 217), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                var reload = new Button { Text = "Làm mới", Width = 104, Height = 36 };
                bottom.Controls.Add(close);
                bottom.Controls.Add(classify);
                bottom.Controls.Add(reload);

                var searchPanel = new Panel { Dock = DockStyle.Top, Height = 57, BackColor = Color.White };
                var searchLabel = new Label { Text = "Tìm tên / SHA-256:", ForeColor = Ink };
                searchLabel.SetBounds(15, 16, 148, 27);
                var find = new TextBox { Font = new Font(Theme.UiFontFamily, 10.65f) };
                find.SetBounds(165, 12, 360, 31);
                find.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                var statusLabel = new Label { Text = "Chỉ hiển thị 300 chữ ký mỗi lần; nhập từ khóa để tìm thêm.",
                    TextAlign = ContentAlignment.MiddleRight, ForeColor = Muted };
                statusLabel.SetBounds(545, 10, 447, 36);
                statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                searchPanel.Controls.AddRange(new System.Windows.Forms.Control[] { searchLabel, find, statusLabel });

                var view = new DataGridView {
                    Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false, MultiSelect = false,
                    RowHeadersVisible = false, BackgroundColor = Color.White,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                };
                dialog.Controls.Add(view);
                dialog.Controls.Add(bottom);
                dialog.Controls.Add(searchPanel);
                dialog.Controls.Add(heading);

                Action load = () => {
                    try
                    {
                        var table = new DataTable();
                        using (var cn = DbConnection.Create())
                        using (var cmd = new SqlCommand(@"
SELECT TOP (300) SignatureID AS [ID], MalwareName AS [Tên mẫu],
       MalwareFamily AS [Họ mã độc], Category AS [Category hiện tại],
       SHA256, IsActive AS [Đang bật]
FROM dbo.VirusSignatures
WHERE (Category IS NULL OR LTRIM(RTRIM(Category)) = N'' OR
       LOWER(LTRIM(RTRIM(Category))) IN
       (N'userverified',N'usersuspected',N'suspected',N'adminreview',
        N'pendingreview',N'verified',N'userapproved',N'pending',
        N'unknown',N'other',N'khác',N'chưa phân loại',N'unclassified'))
  AND (@term=N'' OR MalwareName LIKE N'%' + @term + N'%' OR SHA256 LIKE @term + N'%')
ORDER BY SignatureID DESC", cn))
                        using (var adapter = new SqlDataAdapter(cmd))
                        {
                            cmd.CommandTimeout = 15;
                            cmd.Parameters.Add("@term", SqlDbType.NVarChar, 100).Value = find.Text.Trim();
                            adapter.Fill(table);
                        }
                        view.DataSource = table;
                        statusLabel.Text = table.Rows.Count.ToString("N0") + " mẫu chưa phân loại";
                    }
                    catch (Exception ex)
                    {
                        statusLabel.Text = "Không tải được danh sách";
                        MessageBox.Show(dialog, ex.Message, "SQL Server", MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                };
                reload.Click += (sender, args) => load();
                find.KeyDown += (sender, args) => {
                    if (args.KeyCode == Keys.Enter) { args.SuppressKeyPress = true; load(); }
                };
                classify.Click += (sender, args) => {
                    if (!AdminSecurity.IsAuthenticated) { dialog.Close(); return; }
                    if (view.CurrentRow == null || view.CurrentRow.Cells["ID"].Value == null)
                    {
                        MessageBox.Show(dialog, "Hãy chọn chữ ký cần phân loại.");
                        return;
                    }
                    try
                    {
                        long id = Convert.ToInt64(view.CurrentRow.Cells["ID"].Value);
                        if (AssignVerifiedCategory(id, dialog))
                        {
                            load();
                            RefreshDashboardAsync();
                        }
                    }
                    catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Phân loại chữ ký", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                };
                load();
                dialog.ShowDialog(FindForm());
            }
            RefreshData();
        }

        private bool AssignVerifiedCategory(long signatureId, Form owner)
        {
            string name, family, oldCategory, hash;
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand(@"SELECT MalwareName, MalwareFamily, Category, SHA256
FROM dbo.VirusSignatures WHERE SignatureID=@id", cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = signatureId;
                cn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) throw new InvalidOperationException("Chữ ký không còn trong CSDL.");
                    name = reader.IsDBNull(0) ? "" : Convert.ToString(reader[0]);
                    family = reader.IsDBNull(1) ? "" : Convert.ToString(reader[1]);
                    oldCategory = reader.IsDBNull(2) ? null : Convert.ToString(reader[2]);
                    hash = reader.IsDBNull(3) ? "" : Convert.ToString(reader[3]);
                }
            }
            using (var form = new Form {
                Text = "Xác nhận loại mã độc của chữ ký #" + signatureId,
                StartPosition = FormStartPosition.CenterParent, MinimumSize = new Size(600, 415),
                ClientSize = new Size(665, 425), BackColor = Color.White,
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false,
                Font = new Font(Theme.UiFontFamily, 10.65f)
            })
            {
                var info = new Label {
                    Text = "Tên mẫu: " + name + "\r\nHọ: " + (string.IsNullOrEmpty(family) ? "Chưa rõ" : family) +
                        "\r\nSHA-256: " + (string.IsNullOrEmpty(hash) ? "Không có" : hash) +
                        "\r\nCategory hiện tại: " + (oldCategory ?? "(trống)"),
                    AutoSize = false, ForeColor = Ink, AutoEllipsis = true,
                    BorderStyle = BorderStyle.FixedSingle
                };
                info.SetBounds(17, 14, 628, 106);
                var typeLabel = new Label { Text = "Loại mã độc đã xác minh:", ForeColor = Ink };
                typeLabel.SetBounds(17, 138, 230, 28);
                var category = new ComboBox {
                    DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                    AutoCompleteSource = AutoCompleteSource.ListItems, Font = new Font(Theme.UiFontFamily, 10.65f)
                };
                category.Items.AddRange(StandardThreatCategories.Cast<object>().ToArray());
                category.SetBounds(252, 135, 393, 32);
                var evidenceLabel = new Label { Text = "Bằng chứng xác minh (bắt buộc):", ForeColor = Ink };
                evidenceLabel.SetBounds(17, 184, 410, 25);
                var evidence = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical };
                evidence.SetBounds(17, 212, 628, 113);
                var note = new Label { Text = "Không tự đoán loại từ SHA-256 hoặc trạng thái UserVerified. " +
                    "Có thể nhập loại riêng nếu đã kiểm chứng.", ForeColor = Muted };
                note.SetBounds(17, 332, 625, 34);
                var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel };
                cancel.SetBounds(537, 376, 108, 36);
                var save = new Button { Text = "Xác nhận loại", DialogResult = DialogResult.None,
                    BackColor = Color.FromArgb(33, 103, 217), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat };
                save.SetBounds(383, 376, 142, 36);
                form.Controls.AddRange(new System.Windows.Forms.Control[] {
                    info, typeLabel, category, evidenceLabel, evidence, note, save, cancel
                });
                bool saved = false;
                save.Click += (sender, args) => {
                    string chosen = category.Text.Trim();
                    string why = evidence.Text.Trim();
                    if (chosen.Length < 2 || chosen.Length > 60 || IsUnclassifiedThreatValue(chosen) ||
                        chosen.Any(char.IsControl) || chosen.Contains("<") || chosen.Contains(">"))
                    {
                        MessageBox.Show(form, "Nhập tên loại mã độc cụ thể (2–60 ký tự), không phải trạng thái UserVerified/Suspected.",
                            "Loại không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (why.Length < 10)
                    {
                        MessageBox.Show(form, "Ghi nguồn hoặc bằng chứng xác minh cụ thể (ít nhất 10 ký tự).",
                            "Thiếu bằng chứng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (MessageBox.Show(form, "Gán loại '" + chosen + "' cho chữ ký #" + signatureId + "?\r\n" +
                        "Thao tác này không thay đổi IsActive hoặc các giá trị hash.",
                        "Xác nhận phân loại", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                    if (!AdminSecurity.IsAuthenticated) throw new UnauthorizedAccessException("Phiên quản trị đã hết hiệu lực.");
                    using (var cn = DbConnection.Create())
                    using (var cmd = new SqlCommand(@"
UPDATE dbo.VirusSignatures SET Category=@newCategory, UpdatedAt=SYSDATETIME()
WHERE SignatureID=@id AND ((Category=@oldCategory) OR (Category IS NULL AND @oldCategory IS NULL))", cn))
                    {
                        cmd.Parameters.Add("@newCategory", SqlDbType.NVarChar, 100).Value = chosen;
                        cmd.Parameters.Add("@oldCategory", SqlDbType.NVarChar, 100).Value =
                            oldCategory == null ? (object)DBNull.Value : oldCategory;
                        cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = signatureId;
                        cn.Open();
                        if (cmd.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("Chữ ký đã được sửa ở nơi khác. Vui lòng tải lại.");
                    }
                    // Nhật ký cục bộ hiện có; không tạo bảng SQL mới.
                    AdminAudit.Write("signature-category", "ID=" + signatureId +
                        "; " + (oldCategory ?? "NULL") + " -> " + chosen + "; bằng chứng=" +
                        (why.Length > 320 ? why.Substring(0, 320) : why));
                    HybridSignatureScanner.Invalidate();
                    saved = true;
                    form.DialogResult = DialogResult.OK;
                    form.Close();
                };
                form.ShowDialog(owner);
                return saved;
            }
        }
    }
}
