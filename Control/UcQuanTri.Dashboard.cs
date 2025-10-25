using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScanAndRemoveVirus.Database;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Control
{
    // Bảng điều khiển Quản trị: tất cả thống kê lấy từ CSDL hiện có; không có số mẫu cố định.
    public sealed partial class UcQuanTri
    {
        private sealed class DashboardSnapshot
        {
            public bool SqlConnected;
            public bool ScansLoaded;
            public bool ThreatsLoaded;
            public bool SettingsLoaded;
            // Tất cả tổng phát hiện lấy từ ScanHistory.ThreatCount.
            // ThreatDetections chỉ là chi tiết phụ gắn bằng ScanID, có thể thiếu.
            public bool DetectionDetailsLoaded;
            public bool QuarantineStatsLoaded;
            public bool BlockedLoaded;
            public long CompletedScans;
            public long SkippedScans;
            public long FileCountSessions;
            public long DurationSampleCount;
            public long MissingDetailCount;
            public string Error;
            public readonly List<string> QueryErrors = new List<string>();
            public DateTime Updated;
            public int Days;
            public long ScanCount;
            public long FilesScanned;
            public long Detections;
            public long Blocked;
            public long Quarantined;
            public long QuarantineFailed;
            public long QuarantinePending;
            public long Unhandled;
            public long OtherScans;
            public long LatestScanThreatCount;
            public DateTime LatestScanTime;
            public double AverageSeconds;
            public DateTime RangeStart;
            public DateTime RangeEnd;
            public bool HasAverage;
            public string[] Labels = new string[7];
            public int[] Quick = new int[7];
            public int[] Full = new int[7];
            public int[] Other = new int[7];
            // Các loại mã độc được đọc động từ chữ ký; không tự tạo nhóm "Khác".
            public Dictionary<string, int[]> CategoriesByDay =
                new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, long> Categories =
                new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            // Các phát hiện thiếu Category hợp lệ hoặc không có chi tiết ScanID.
            // Luôn cùng nguồn với ScanHistory.ThreatCount và từng bucket của biểu đồ ngày.
            public long UnclassifiedCount;
            public int[] UnclassifiedByDay = new int[7];
            public bool?[] Protection = new bool?[4];
            public string CoverageNote;
        }

        private Panel dashboardCard;
        private Panel dashboardCanvas;
        private float dashboardDrawScaleY = 1f;
        private UiSelect dashboardPeriod;
        private readonly ToolTip dashboardToolTip = new ToolTip();
        private DateTime dashboardCustomStart = DateTime.Today.AddDays(-6);
        private DateTime dashboardCustomEnd = DateTime.Today;
        private int dashboardPreviousPeriod = 0;
        private bool dashboardChangingPeriod;
        private DateTime? dashboardLastSuccess;
        private Label dashboardUpdatedLabel;
        private Button dashboardRefresh;
        private Button dashboardDetails;
        private Button dashboardCategoriesButton;
        private DashboardSnapshot dashboardData = new DashboardSnapshot();
        private int dashboardSequence;
        private System.Windows.Forms.Timer dashboardTimer;
        private Action dashboardHistoryChanged;
        private Action dashboardQuarantineChanged;
        private string dashboardMetricHint = "";

        // Mỗi vùng sử dụng đúng hệ toạ độ ảo của đồ thị. Khi Dashboard được
        // ScaleTransform để vừa cửa sổ, hit-test đảo tỷ lệ trước khi so sánh.
        private sealed class DashboardHoverRegion
        {
            public Rectangle Bounds;
            public string Hint;
        }
        private readonly List<DashboardHoverRegion> dashboardHoverRegions =
            new List<DashboardHoverRegion>();

        private void AddDashboardHover(Rectangle bounds, string hint)
        {
            if (bounds.Width > 0 && bounds.Height > 0 && !string.IsNullOrEmpty(hint))
                dashboardHoverRegions.Add(new DashboardHoverRegion { Bounds = bounds, Hint = hint });
        }

        private static readonly Color DBlue = Color.FromArgb(31, 123, 239);
        private static readonly Color DTeal = Color.FromArgb(9, 173, 164);
        private static readonly Color DGreen = Color.FromArgb(23, 169, 101);
        private static readonly Color DRed = Color.FromArgb(221, 77, 80);
        private static readonly Color DOrange = Color.FromArgb(241, 169, 49);
        private static readonly Color DGray = Color.FromArgb(65, 82, 107);
        private static readonly Color DUnclassified = Color.FromArgb(145, 156, 173);
        private static readonly Color DOtherCategories = Color.FromArgb(94, 116, 146);
        private const string UnclassifiedThreatLabel = "Chưa phân loại";
        private const string OtherThreatTypesLabel = "Các loại khác";
        // Bảng màu dùng thống nhất cho biểu đồ ngày và bảng phân bố loại mã độc.
        private static readonly Color[] DashboardCategoryColors = {
            DBlue, DTeal, DGray, DOrange, Color.FromArgb(153, 123, 193),
            Color.FromArgb(52, 187, 126), Color.FromArgb(10, 140, 212),
            Color.FromArgb(205, 99, 154), Color.FromArgb(115, 126, 214),
            Color.FromArgb(180, 146, 58)
        };

        private void BuildDashboardCard()
        {
            dashboardCard = HomeCard("Thống kê hệ thống",
                "Thống kê phiên quét từ ScanHistory; phân loại dùng chi tiết liên kết ScanID.");
            dashboardUpdatedLabel = new Label {
                ForeColor = Muted, Font = new Font(Theme.UiFontFamily, 11.25f),
                TextAlign = ContentAlignment.MiddleRight, Text = "Đang tải dữ liệu..."
            };
            dashboardRefresh = MakeButton("↻", () => RefreshDashboardAsync());
            dashboardRefresh.Font = new Font("Segoe UI Symbol", 13f, FontStyle.Bold);
            dashboardRefresh.ForeColor = DBlue;
            dashboardRefresh.FlatAppearance.BorderColor = Color.FromArgb(213, 227, 245);
            dashboardRefresh.BackColor = Color.White;
            dashboardCard.Controls.Add(dashboardUpdatedLabel);
            dashboardCard.Controls.Add(dashboardRefresh);
            dashboardDetails = MakeButton("Xem lỗi", () => ShowDashboardErrors());
            dashboardDetails.Visible = false;
            dashboardDetails.ForeColor = DRed;
            dashboardDetails.FlatAppearance.BorderColor = Color.FromArgb(243, 209, 210);
            dashboardDetails.BackColor = Color.White;
            dashboardCard.Controls.Add(dashboardDetails);
            dashboardCanvas = new Panel { BackColor = Color.White, Name = "admin-dashboard-charts" };
            dashboardCanvas.Paint += (s, e) => DrawDashboardFitted(e.Graphics, dashboardCanvas.ClientRectangle);
            dashboardCanvas.MouseMove += DashboardMetrics_MouseMove;
            dashboardCanvas.MouseLeave += (s, e) => {
                dashboardMetricHint = "";
                dashboardToolTip.Hide(dashboardCanvas);
            };
            dashboardToolTip.ShowAlways = true;
            dashboardToolTip.InitialDelay = 260;
            dashboardToolTip.ReshowDelay = 100;
            dashboardToolTip.AutoPopDelay = 8000;
            dashboardCard.Controls.Add(dashboardCanvas);
            dashboardCategoriesButton = MakeButton("Xem tất cả", ShowDashboardCategories);
            dashboardCategoriesButton.Name = "admin-dashboard-all-types";
            dashboardCategoriesButton.Font = new Font(Theme.UiFontFamily, 10.75f, FontStyle.Regular);
            dashboardCategoriesButton.ForeColor = DBlue;
            dashboardCategoriesButton.FlatAppearance.BorderColor = Color.FromArgb(213, 227, 245);
            dashboardCategoriesButton.BackColor = Color.White;
            dashboardCanvas.Controls.Add(dashboardCategoriesButton);
            dashboardToolTip.SetToolTip(dashboardCategoriesButton,
                "Xem toàn bộ loại mã độc có trong các bản ghi đã phát hiện thuộc kỳ thống kê.");

            dashboardPeriod = new UiSelect {
                Name = "admin-dashboard-period",
                Font = new Font(Theme.UiFontFamily, 11.55f, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(96, 104, 116),
                Height = 42,
                DropDownWidth = 230,
                ShowCalendarGlyph = true,
                SquareCorners = true,
                AccentBackColor = Color.FromArgb(244, 245, 247),
                AccentHoverColor = Color.FromArgb(236, 238, 241),
                AccentBorderColor = Color.FromArgb(198, 204, 214),
                AccentFocusedBorderColor = Color.FromArgb(150, 158, 170)
            };
            dashboardPeriod.Items.AddRange(new object[] {
                "7 ngày qua", "30 ngày qua", "90 ngày qua", "Hôm nay", "Hôm qua", "Tùy chọn khoảng ngày..." });
            dashboardPeriod.SelectedIndex = 0;
            dashboardPeriod.SelectedIndexChanged += (s, e) => DashboardPeriodChanged();
            dashboardCard.Controls.Add(dashboardPeriod);
            dashboardToolTip.SetToolTip(dashboardPeriod, "Lọc biểu đồ và các chỉ số theo ngày quét/phát hiện.");
            dashboardToolTip.SetToolTip(dashboardUpdatedLabel,
                "Thời điểm chương trình tải được thống kê từ SQL Server; không phải ngày phát hành chữ ký.");
            homeHost.Controls.Add(dashboardCard);
            // Một phiên quét mới có thể ghi SQL khi trang Admin đang mở.
            dashboardTimer = new System.Windows.Forms.Timer { Interval = 45000 };
            dashboardTimer.Tick += (s, e) => {
                if (!IsDisposed && AdminSecurity.IsAuthenticated && homeHost.Visible)
                    RefreshDashboardAsync();
            };
            dashboardTimer.Start();
            // Làm mới ngay sau một lần quét được ghi lịch sử trong cùng tiến trình.
            dashboardHistoryChanged = () => {
                if (IsDisposed || !IsHandleCreated) return;
                try
                {
                    BeginInvoke((Action)(() => {
                        if (!IsDisposed && AdminSecurity.IsAuthenticated && homeHost.Visible)
                            RefreshDashboardAsync();
                    }));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            };
            ScanHistoryStore.Changed += dashboardHistoryChanged;
            // Cách ly có thể tạo ThreatDetections.ScanID=NULL và không phát sự kiện ScanHistory.
            dashboardQuarantineChanged = () => {
                if (IsDisposed || !IsHandleCreated) return;
                try {
                    BeginInvoke((Action)(() => {
                        if (!IsDisposed && AdminSecurity.IsAuthenticated && homeHost.Visible)
                            RefreshDashboardAsync();
                    }));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            };
            ScanEngine.QuarantineChanged += dashboardQuarantineChanged;
            dashboardCanvas.Disposed += (s, e) => {
                if (dashboardTimer != null) dashboardTimer.Dispose();
                if (dashboardHistoryChanged != null) ScanHistoryStore.Changed -= dashboardHistoryChanged;
                if (dashboardQuarantineChanged != null) ScanEngine.QuarantineChanged -= dashboardQuarantineChanged;
            };
        }

        private void LayoutDashboard(int x, int y, int width, int height)
        {
            if (dashboardCard == null) return;
            // TableLayoutPanel quyết định vị trí và kích thước khung ngoài.
            // Không SetBounds lại card khi đang Dock=Fill (gây xung đột layout/DPI).
            if (dashboardCard.Parent != adminHomeGrid)
                dashboardCard.SetBounds(x, y, width, height);
            var cardTitle = dashboardCard.Controls.Find("home-card-title", false)
                .OfType<Label>().FirstOrDefault();
            if (cardTitle != null) cardTitle.Visible = false;
            var cardIcon = dashboardCard.Controls.Find("home-card-icon", false)
                .OfType<Label>().FirstOrDefault();
            if (cardIcon != null) cardIcon.Visible = false;
            var cardSubtitle = dashboardCard.Controls.Find("home-card-subtitle", false)
                .OfType<Label>().FirstOrDefault();
            if (cardSubtitle != null)
            {
                cardSubtitle.Visible = true;
                cardSubtitle.SetBounds(18, 10, Math.Max(100, width - 36), 26);
                cardSubtitle.Font = new Font(Theme.UiFontFamily, 11.15f, FontStyle.Regular);
            }

            // Thanh công cụ (chọn ngày, thời điểm cập nhật, làm mới, đăng xuất)
            // nằm trên cùng; phần card chỉ hiển thị mô tả và biểu đồ.
            dashboardCanvas.SetBounds(12, 40, Math.Max(10, width - 24),
                Math.Max(10, height - 48));
            dashboardDrawScaleY = Math.Min(1f, Math.Min(dashboardCanvas.Height / 360f,
                dashboardCanvas.Width / 1120f));
            if (dashboardCategoriesButton != null)
            {
                float scale = Math.Max(.05f, dashboardDrawScaleY);
                int cw = Math.Max(1120, (int)Math.Ceiling(dashboardCanvas.Width / scale));
                int chartGap = 10;
                int first = (int)Math.Round((cw - 2 * chartGap - 2) * .38);
                int second = (int)Math.Round((cw - 2 * chartGap - 2) * .36);
                int thirdLeft = 1 + first + chartGap + second + chartGap;
                int chartHeight = Math.Max(360, (int)Math.Ceiling(dashboardCanvas.Height / scale));
                int topHeight = DashboardTopChartHeight(chartHeight);
                dashboardCategoriesButton.SetBounds(
                    (int)Math.Round(Math.Max(thirdLeft + 10, cw - 116) * scale),
                    (int)Math.Round(Math.Max(14, 2 + topHeight - 34) * scale),
                    Math.Max(35, (int)Math.Round(99 * scale)),
                    Math.Max(21, (int)Math.Round(31 * scale)));
                dashboardCategoriesButton.BringToFront();
            }
            dashboardCanvas.Invalidate();
        }

        private void DashboardPeriodChanged()
        {
            if (dashboardChangingPeriod || dashboardPeriod == null) return;
            if (dashboardPeriod.SelectedIndex == 5)
            {
                // Nếu hủy hộp thoại, trở về lựa chọn trước và không chạy truy vấn.
                if (!ShowDashboardDatePicker())
                {
                    dashboardChangingPeriod = true;
                    dashboardPeriod.SelectedIndex = dashboardPreviousPeriod;
                    dashboardChangingPeriod = false;
                }
                return;
            }
            dashboardPreviousPeriod = dashboardPeriod.SelectedIndex;
            RefreshDashboardAsync();
        }

        // Bộ chọn ngày của WinForms, hoàn toàn không cần tạo thêm bảng SQL.
        private bool ShowDashboardDatePicker()
        {
            using (var form = new Form {
                Text = "Chọn khoảng thời gian thống kê", StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false,
                ShowInTaskbar = false, ClientSize = new Size(420, 226), BackColor = Color.White,
                Font = new Font(Theme.UiFontFamily, 10.65f), AutoScaleMode = AutoScaleMode.Font
            })
            {
                var label = new Label { Text = "Lọc dữ liệu thống kê theo khoảng ngày", AutoSize = false,
                    Font = new Font(Theme.UiFontFamily, 11.55f, FontStyle.Bold), ForeColor = Ink };
                label.SetBounds(18, 15, 385, 30);
                var fromLabel = new Label { Text = "Từ ngày", AutoSize = false, ForeColor = Muted };
                fromLabel.SetBounds(18, 66, 103, 28);
                var toLabel = new Label { Text = "Đến ngày", AutoSize = false, ForeColor = Muted };
                toLabel.SetBounds(18, 111, 103, 28);
                var fromPicker = new DateTimePicker { Format = DateTimePickerFormat.Custom,
                    CustomFormat = "dd/MM/yyyy", Value = dashboardCustomStart, MaxDate = DateTime.Today };
                fromPicker.SetBounds(125, 62, 266, 32);
                var toPicker = new DateTimePicker { Format = DateTimePickerFormat.Custom,
                    CustomFormat = "dd/MM/yyyy", Value = dashboardCustomEnd, MaxDate = DateTime.Today };
                toPicker.SetBounds(125, 107, 266, 32);
                var apply = new Button { Text = "Áp dụng", DialogResult = DialogResult.None,
                    BackColor = DBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                apply.SetBounds(214, 165, 94, 36);
                var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel,
                    BackColor = Color.White, FlatStyle = FlatStyle.Flat };
                cancel.SetBounds(316, 165, 75, 36);
                apply.Click += (sender, e) => {
                    if (fromPicker.Value.Date > toPicker.Value.Date)
                    {
                        MessageBox.Show(form, "Ngày bắt đầu không được sau ngày kết thúc.",
                            "Khoảng thời gian không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if ((toPicker.Value.Date - fromPicker.Value.Date).TotalDays > 365)
                    {
                        MessageBox.Show(form, "Vui lòng chọn tối đa 366 ngày mỗi lần để tránh truy vấn quá lớn.",
                            "Khoảng thời gian quá dài", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    form.DialogResult = DialogResult.OK;
                    form.Close();
                };
                form.CancelButton = cancel;
                form.Controls.AddRange(new System.Windows.Forms.Control[] {
                    label, fromLabel, toLabel, fromPicker, toPicker, apply, cancel });
                if (form.ShowDialog(FindForm()) != DialogResult.OK) return false;
                dashboardCustomStart = fromPicker.Value.Date;
                dashboardCustomEnd = toPicker.Value.Date;
            }
            dashboardChangingPeriod = true;
            dashboardPeriod.Items[5] = "Tùy chọn: " + dashboardCustomStart.ToString("dd/MM") + " - " +
                                       dashboardCustomEnd.ToString("dd/MM");
            dashboardPeriod.SelectedIndex = 5;
            dashboardChangingPeriod = false;
            dashboardPreviousPeriod = 5;
            RefreshDashboardAsync();
            return true;
        }

        private void DashboardRange(out DateTime from, out DateTime through)
        {
            through = DateTime.Today;
            int choice = dashboardPeriod == null ? 0 : dashboardPeriod.SelectedIndex;
            switch (choice)
            {
                case 1: from = through.AddDays(-29); break;
                case 2: from = through.AddDays(-89); break;
                case 3: from = through; break;
                case 4: through = through.AddDays(-1); from = through; break;
                case 5: from = dashboardCustomStart; through = dashboardCustomEnd; break;
                default: from = through.AddDays(-6); break;
            }
        }

        private void RefreshDashboardAsync()
        {
            if (!AdminSecurity.IsAuthenticated || dashboardCanvas == null || dashboardCanvas.IsDisposed) return;
            DateTime fromDate, throughDate;
            DashboardRange(out fromDate, out throughDate);
            // Chụp giá trị ngày trước khi chuyển sang tác vụ nền để tránh đọc control khác thread.
            int days = (throughDate - fromDate).Days + 1;
            int request = ++dashboardSequence;
            dashboardUpdatedLabel.Text = "Đang tải thống kê...";
            dashboardUpdatedLabel.Tag = "Đang tải thống kê...";
            dashboardRefresh.Enabled = false;
            Task.Run(() => FetchDashboard(fromDate, throughDate)).ContinueWith(task =>
            {
                if (IsDisposed || !IsHandleCreated) return;
                try
                {
                    BeginInvoke((Action)(() =>
                    {
                        if (IsDisposed || request != dashboardSequence || !AdminSecurity.IsAuthenticated) return;
                        dashboardData = task.IsFaulted ? new DashboardSnapshot {
                            Error = "Không đọc được số liệu hệ thống: " + task.Exception.GetBaseException().Message,
                            Days = days } : task.Result;
                        // Ghi nhận khi SQL đã tải thành công lịch sử HOẶC trạng thái cách ly.
                        bool anyDataLoaded = dashboardData.SqlConnected &&
                            (dashboardData.ScansLoaded || dashboardData.QuarantineStatsLoaded);
                        if (anyDataLoaded) dashboardLastSuccess = dashboardData.Updated;
                        string last = dashboardLastSuccess.HasValue
                            ? "Cập nhật lần cuối: " + dashboardLastSuccess.Value.ToString("dd/MM/yyyy HH:mm")
                            : "Chưa có lần cập nhật thành công";
                        dashboardUpdatedLabel.Text = !dashboardData.SqlConnected ? last + " (SQL lỗi)" :
                            dashboardData.QueryErrors.Count > 0 ? last + " (thiếu dữ liệu)" : last;
                        dashboardUpdatedLabel.Tag = dashboardUpdatedLabel.Text;
                        dashboardToolTip.SetToolTip(dashboardUpdatedLabel,
                            "Lần tải thống kê thành công từ SQL Server: " +
                            (dashboardLastSuccess.HasValue ? dashboardLastSuccess.Value.ToString("dd/MM/yyyy HH:mm:ss") : "chưa có") +
                            ". " + (dashboardData.QueryErrors.Count > 0 ? "Có truy vấn lỗi, nhấn Xem lỗi để kiểm tra." :
                             "Không đồng nghĩa thời điểm cơ sở dữ liệu chữ ký được cập nhật."));
                        dashboardDetails.Visible = !dashboardData.SqlConnected || dashboardData.QueryErrors.Count > 0;
                        LayoutAdminToolbar();
                        LayoutDashboard(dashboardCard.Left, dashboardCard.Top, dashboardCard.Width, dashboardCard.Height);
                        // Tooltip chuyên biệt theo từng thẻ chỉ số được đặt ở MouseMove.
                        // Không hiển thị một tooltip dài che kín các biểu đồ.
                        dashboardMetricHint = "";
                        dashboardToolTip.SetToolTip(dashboardCanvas, "");
                        dashboardRefresh.Enabled = true;
                        dashboardCanvas.Invalidate();
                    }));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            });
        }

        private void ShowDashboardErrors()
        {
            var d = dashboardData;
            string details = !d.SqlConnected
                ? "Không thể kết nối tới SQL Server.\n" + (d.Error ?? "Không rõ lỗi kết nối.")
                : d.QueryErrors.Count == 0
                    ? "Các truy vấn thống kê đã chạy thành công."
                    : "SQL Server đã kết nối. Một số truy vấn thống kê không thực hiện được:\n\n" +
                      string.Join("\n\n", d.QueryErrors.ToArray());
            MessageBox.Show(FindForm(), details, "Chẩn đoán thống kê quản trị",
                MessageBoxButtons.OK, d.SqlConnected ? MessageBoxIcon.Warning : MessageBoxIcon.Error);
        }

        private static void RecordDashboardError(DashboardSnapshot data, string section, Exception ex)
        {
            // Chỉ ghi lỗi cụ thể cho module thất bại; SQL đã mở vẫn được coi là kết nối thành công.
            string code = ex is SqlException ? " (SQL " + ((SqlException)ex).Number + ")" : "";
            data.QueryErrors.Add(section + code + ": " + ex.Message);
        }

        private sealed class ScanThreatBudget
        {
            public int Bucket;
            public long Original;
            public long Remaining;
        }

        private static DashboardSnapshot FetchDashboard(DateTime fromDate, DateTime throughDate)
        {
            int days = (throughDate.Date - fromDate.Date).Days + 1;
            var data = new DashboardSnapshot { Days = days, RangeStart = fromDate.Date, RangeEnd = throughDate.Date };
            DateTime start = fromDate.Date;
            DateTime endExclusive = throughDate.Date.AddDays(1);
            for (int i = 0; i < 7; i++)
                data.Labels[i] = days <= 7
                    ? (i < days ? start.AddDays(i).ToString("dd/MM") : "")
                    : start.AddDays((int)Math.Floor(days * (double)i / 7)).ToString("dd/MM");

            try
            {
                using (var cn = DbConnection.Create())
                {
                    cn.Open();
                    data.SqlConnected = true;
                    var remainingByScan = new Dictionary<long, ScanThreatBudget>();

                    // MỘT NGUỒN THỐNG KÊ DUY NHẤT: bảng dbo.ScanHistory.
                    // Khoảng ngày áp theo StartedAt, giống màn hình Lịch sử.
                    // Không dùng COUNT từ ThreatDetections thay thế ThreatCount trong lịch sử.
                    try
                    {
                        long durationCount = 0;
                        double durationSum = 0;
                        using (var cmd = new SqlCommand(@"
                            SELECT ScanID, StartedAt, ScanType, ActivityTitle,
                                   FilesScanned, ThreatCount, CompletedAt, ResultStatus
                            FROM dbo.ScanHistory
                            WHERE StartedAt >= @start AND StartedAt < @end
                            ORDER BY StartedAt ASC, ScanID ASC", cn))
                        {
                            SetPeriod(cmd, start, endExclusive);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    long scanID = Convert.ToInt64(reader[0]);
                                    DateTime startedAt = Convert.ToDateTime(reader[1]);
                                    string scanType = reader.IsDBNull(2) ? "" : Convert.ToString(reader[2]);
                                    string title = reader.IsDBNull(3) ? "" : Convert.ToString(reader[3]);
                                    int kind = DashboardScanKind(scanType, title);
                                    int bucket = DashboardBucket(startedAt, start, days);
                                    long threatCount = reader.IsDBNull(5) ? 0 : Math.Max(0, Convert.ToInt64(reader[5]));
                                    string resultStatus = reader.IsDBNull(7) ? "" : Convert.ToString(reader[7]);
                                    bool completed = !reader.IsDBNull(6);
                                    DateTime completedAt = completed ? Convert.ToDateTime(reader[6]) : DateTime.MinValue;
                                    // Không cộng dồn tệp và thời gian của phiên đang chạy, lỗi, hủy
                                    // hoặc có mốc thời gian bị đảo (tránh số liệu tăng sai).
                                    bool validCompleted = completed && completedAt >= startedAt &&
                                        DashboardIsCompletedScan(resultStatus);

                                    // Phát hiện chỉ được tính từ những bản ghi lịch sử đã hoàn tất.
                                    // Những sự kiện RealTime hoàn tất vẫn được tính vào tổng phát hiện,
                                    // nhưng không được tính là một lượt quét tay.
                                    if (validCompleted)
                                    {
                                        data.Detections += threatCount;
                                        if (threatCount > 0)
                                            remainingByScan[scanID] = new ScanThreatBudget {
                                                Bucket = bucket, Original = threatCount, Remaining = threatCount };
                                    }

                                    if (kind < 0) continue;
                                    if (!validCompleted)
                                    {
                                        data.SkippedScans++;
                                        continue;
                                    }

                                    data.ScanCount++;
                                    data.CompletedScans++;
                                    if (kind == 0) data.Quick[bucket]++;
                                    else if (kind == 1) data.Full[bucket]++;
                                    else { data.Other[bucket]++; data.OtherScans++; }
                                    if (!reader.IsDBNull(4))
                                    {
                                        data.FilesScanned += Math.Max(0, Convert.ToInt64(reader[4]));
                                        data.FileCountSessions++;
                                    }
                                    if (startedAt >= data.LatestScanTime)
                                    {
                                        data.LatestScanTime = startedAt;
                                        data.LatestScanThreatCount = threatCount;
                                    }
                                    double seconds = (completedAt - startedAt).TotalSeconds;
                                    // Các mốc thời gian bất thường được loại khỏi mẫu trung bình,
                                    // không làm mất số tệp của phiên đã hoàn thành.
                                    if (seconds >= 0 && seconds <= TimeSpan.FromDays(7).TotalSeconds)
                                    {
                                        durationSum += seconds;
                                        durationCount++;
                                    }
                                }
                            }
                        }
                        data.DurationSampleCount = durationCount;
                        data.HasAverage = durationCount > 0;
                        if (data.HasAverage) data.AverageSeconds = durationSum / durationCount;
                        data.ScansLoaded = true;
                        data.ThreatsLoaded = true;
                    }
                    catch (Exception ex)
                    {
                        data.ScanCount = data.FilesScanned = data.OtherScans = data.Detections = 0;
                        data.DurationSampleCount = data.FileCountSessions = data.CompletedScans = data.SkippedScans = 0;
                        data.HasAverage = false;
                        data.Quick = new int[7]; data.Full = new int[7]; data.Other = new int[7];
                        data.CategoriesByDay.Clear();
                        data.UnclassifiedByDay = new int[7];
                        remainingByScan.Clear();
                        RecordDashboardError(data, "Đọc bảng ScanHistory", ex);
                    }

                    if (data.ScansLoaded)
                    {
                        // ThreatDetections CHỈ bổ sung tên loại và tình trạng xử lý.
                        // INNER JOIN theo ScanID đảm bảo các tệp phát hiện thuộc chính
                        // những phiên ScanHistory trong thời gian đã chọn.
                        // Mỗi ScanID được giới hạn bởi ScanHistory.ThreatCount để
                        // biểu đồ loại mã độc không cao hơn tổng lịch sử.
                        try
                        {
                            using (var cmd = new SqlCommand(@"
                                SELECT h.ScanID, s.Category, s.MalwareFamily,
                                       s.MalwareName, d.ThreatName, d.Status, d.ActionTaken,
                                       COUNT_BIG(*) AS Total
                                FROM dbo.ThreatDetections AS d
                                INNER JOIN dbo.ScanHistory AS h ON h.ScanID = d.ScanID
                                LEFT JOIN dbo.VirusSignatures AS s ON s.SignatureID = d.SignatureID
                                WHERE h.StartedAt >= @start AND h.StartedAt < @end
                                GROUP BY h.ScanID, s.Category, s.MalwareFamily,
                                         s.MalwareName, d.ThreatName, d.Status, d.ActionTaken
                                ORDER BY h.ScanID", cn))
                            {
                                SetPeriod(cmd, start, endExclusive);
                                using (var rd = cmd.ExecuteReader())
                                {
                                    while (rd.Read())
                                    {
                                        var scanID = Convert.ToInt64(rd[0]);
                                        ScanThreatBudget budget;
                                        if (!remainingByScan.TryGetValue(scanID, out budget) || budget.Remaining <= 0)
                                            continue;
                                        long matching = Math.Min(Convert.ToInt64(rd[7]), budget.Remaining);
                                        if (matching <= 0) continue;
                                        budget.Remaining -= matching;
                                        // Category có thể đang chứa trạng thái nhập mẫu như UserVerified.
                                        // Chỉ lấy loại mã độc đã xác minh hoặc gợi ý rõ ràng từ tên.
                                        string type = NormalizeThreatCategory(
                                            rd.IsDBNull(1) ? null : Convert.ToString(rd[1]),
                                            rd.IsDBNull(2) ? null : Convert.ToString(rd[2]),
                                            rd.IsDBNull(3) ? null : Convert.ToString(rd[3]),
                                            rd.IsDBNull(4) ? null : Convert.ToString(rd[4]));
                                        string status = rd.IsDBNull(5) ? "" : Convert.ToString(rd[5]);
                                        string action = rd.IsDBNull(6) ? "" : Convert.ToString(rd[6]);

                                        if (type == null)
                                        {
                                            // Thiếu loại hợp lệ: đưa vào nhóm "Chưa phân loại"
                                            // trong cả bảng phổ biến lẫn biểu đồ theo ngày.
                                            data.UnclassifiedCount += matching;
                                            data.UnclassifiedByDay[budget.Bucket] =
                                                SafeAddChartCount(data.UnclassifiedByDay[budget.Bucket], matching);
                                        }
                                        else
                                        {
                                            long previous;
                                            data.Categories.TryGetValue(type, out previous);
                                            data.Categories[type] = previous + matching;
                                            int[] daily;
                                            if (!data.CategoriesByDay.TryGetValue(type, out daily))
                                            {
                                                daily = new int[7];
                                                data.CategoriesByDay[type] = daily;
                                            }
                                            daily[budget.Bucket] = SafeAddChartCount(daily[budget.Bucket], matching);
                                        }
                                        // Chỉ phân loại phát hiện từ lịch sử tại đây.
                                        // Cách ly có thể có ScanID=NULL, được thống kê riêng ở dưới.
                                    }
                                }
                            }
                            data.DetectionDetailsLoaded = true;
                        }
                        catch (Exception ex)
                        {
                            // Lịch sử vẫn phải vẽ bình thường khi chi tiết bị thiếu.
                            data.Categories.Clear();
                            data.CategoriesByDay.Clear();
                            data.UnclassifiedCount = 0;
                            data.UnclassifiedByDay = new int[7];
                            foreach (var record in remainingByScan.Values)
                                record.Remaining = record.Original;
                            RecordDashboardError(data, "Chi tiết ThreatDetections liên kết ScanID", ex);
                        }
                        long notLinked = 0;
                        foreach (var record in remainingByScan.Values)
                        {
                            if (record.Remaining <= 0) continue;
                            notLinked += record.Remaining;
                            data.UnclassifiedByDay[record.Bucket] =
                                SafeAddChartCount(data.UnclassifiedByDay[record.Bucket], record.Remaining);
                        }
                        data.MissingDetailCount = notLinked;
                        data.UnclassifiedCount += notLinked;
                        data.CoverageNote = "Nguồn tổng số: dbo.ScanHistory.ThreatCount. " +
                            "Chi tiết loại: dbo.ThreatDetections kết hợp dbo.VirusSignatures theo ScanID. " +
                            string.Format("{0:N0} phát hiện chưa phân loại (gồm {1:N0} thiếu chi tiết liên kết ScanID).",
                                data.UnclassifiedCount, notLinked);
                    }

                    // "Mối đe dọa đã chặn" phải dựa vào kết quả xử lý THẬT của
                    // ThreatDetections, không dựa vào số phát hiện hoặc JOIN ScanID.
                    // ScanID có thể NULL khi bảo vệ thời gian thực/cách ly thủ công.
                    // Lọc theo DetectedAt vì schema chưa có thời điểm bắt đầu chặn riêng.
                    try
                    {
                        using (var cmd = new SqlCommand(@"
                            SELECT COUNT_BIG(*)
                            FROM dbo.ThreatDetections
                            WHERE DetectedAt >= @start AND DetectedAt < @end
                              AND (
                                LOWER(LTRIM(RTRIM(ISNULL(Status, '')))) IN ('blocked', 'deleted')
                                OR (LOWER(LTRIM(RTRIM(ISNULL(Status, '')))) = 'quarantined'
                                    AND NULLIF(LTRIM(RTRIM(QuarantinePath)), '') IS NOT NULL)
                                OR (LOWER(LTRIM(RTRIM(ISNULL(ActionTaken, '')))) = 'blocked'
                                    AND LOWER(LTRIM(RTRIM(ISNULL(Status, ''))))
                                        NOT IN ('restored', 'deleted', 'failed', 'detected'))
                              )", cn))
                        {
                            SetPeriod(cmd, start, endExclusive);
                            data.Blocked = Convert.ToInt64(cmd.ExecuteScalar());
                        }
                        data.BlockedLoaded = true;
                    }
                    catch (Exception ex)
                    {
                        data.BlockedLoaded = false;
                        RecordDashboardError(data, "Mối đe dọa đã chặn theo trạng thái xử lý", ex);
                    }

                    // Nguồn trạng thái cách ly là ThreatDetections (không JOIN ScanHistory).
                    // ScanEngine.Quarantine có thể tạo bản ghi với ScanID=NULL.
                    // Tính trên toàn hệ thống, vì DB hiện không có cột QuarantinedAt
                    // để lọc chính xác theo thời điểm thao tác cách ly.
                    try
                    {
                        long succeeded = 0, failed = 0, pending = 0, notQuarantined = 0;
                        using (var cmd = new SqlCommand(@"
                            SELECT Status, ActionTaken, COUNT_BIG(*) AS Total
                            FROM dbo.ThreatDetections
                            GROUP BY Status, ActionTaken", cn))
                        {
                            cmd.CommandTimeout = 15;
                            using (var rd = cmd.ExecuteReader())
                            {
                                while (rd.Read())
                                {
                                    string status = rd.IsDBNull(0) ? "" : Convert.ToString(rd[0]);
                                    string action = rd.IsDBNull(1) ? "" : Convert.ToString(rd[1]);
                                    long total = Convert.ToInt64(rd[2]);
                                    if (DashboardQuarantineSuccessful(status, action)) succeeded += total;
                                    else if (DashboardQuarantineFailed(status, action)) failed += total;
                                    else if (DashboardQuarantinePending(status, action)) pending += total;
                                    else notQuarantined += total;
                                }
                            }
                        }
                        data.Quarantined = succeeded;
                        data.QuarantineFailed = failed;
                        data.QuarantinePending = pending;
                        data.Unhandled = notQuarantined;
                        data.QuarantineStatsLoaded = true;
                    }
                    catch (Exception ex)
                    {
                        data.QuarantineStatsLoaded = false;
                        RecordDashboardError(data, "Thống kê cách ly từ ThreatDetections", ex);
                    }

                    // Cấu hình bảo vệ nằm ở Settings, không phải dữ liệu Lịch sử.
                    try
                    {
                        using (var cmd = new SqlCommand(@"
                            SELECT TOP (1) RealTimeProtection, FileProtection,
                                   WebProtection, RansomwareProtection
                            FROM dbo.Settings ORDER BY SettingID DESC", cn))
                        using (var rd = cmd.ExecuteReader())
                        {
                            if (rd.Read())
                                for (int i = 0; i < 4; i++)
                                    data.Protection[i] = rd[i] != DBNull.Value
                                        ? (bool?)Convert.ToBoolean(rd[i]) : null;
                        }
                        data.SettingsLoaded = true;
                    }
                    catch (Exception ex) { RecordDashboardError(data, "Trạng thái bảo vệ", ex); }
                }
            }
            catch (Exception ex)
            {
                data.SqlConnected = false;
                data.Error = ex.Message;
            }
            if (data.SqlConnected && (data.ScansLoaded || data.QuarantineStatsLoaded))
                data.Updated = DateTime.Now;
            return data;
        }

        private static int SafeAddChartCount(int current, long add)
        {
            if (add <= 0) return current;
            return (int)Math.Min(int.MaxValue, (long)current + add);
        }

        private static void SetPeriod(SqlCommand cmd, DateTime start, DateTime end)
        {
            cmd.CommandTimeout = 12;
            cmd.Parameters.Add("@start", SqlDbType.DateTime2).Value = start;
            cmd.Parameters.Add("@end", SqlDbType.DateTime2).Value = end;
        }

        private static int DashboardBucket(DateTime date, DateTime start, int days)
        {
            int offset = Math.Max(0, Math.Min(days - 1, (date.Date - start.Date).Days));
            return days <= 7 ? offset : Math.Min(6, (int)(offset * 7L / days));
        }

        // 0: Quick; 1: Full; 2: các phiên quét Custom/tệp/thư mục/Other.
        // -1: bản ghi sự kiện Realtime/Behavior/Update (không phải một lượt quét thủ công).
        private static int DashboardScanKind(string scanType, string activityTitle)
        {
            string kind = (scanType ?? "").Trim().ToLowerInvariant();
            string title = (activityTitle ?? "").Trim().ToLowerInvariant();
            if (kind == "update" || kind == "behavior" || kind == "realtime" || kind == "real-time" ||
                title.Contains("cập nhật csdl") || title.Contains("bảo vệ thời gian thực") ||
                title.Contains("phát hiện hành vi")) return -1;
            if (kind == "quick" || kind == "quét nhanh" || title.Contains("quét nhanh")) return 0;
            if (kind == "full" || kind == "quét toàn bộ" || title.Contains("quét toàn bộ")) return 1;
            return 2;
        }

        // Category phải là LOẠI mã độc, không được là trạng thái kiểm duyệt.
        // Các bản nhập mẫu cũ từng ghi UserVerified/Suspected vào Category;
        // chuyển chúng thành "chưa phân loại", hoặc nhận diện từ tên có dấu loại rõ ràng.
        // Không suy đoán từ hash hay chỉ từ trạng thái kích hoạt IsActive.
        private static string NormalizeThreatCategory(params string[] candidates)
        {
            if (candidates == null || candidates.Length == 0) return null;
            string raw = (candidates[0] ?? "").Trim();
            // Chỉ tin Category khi đó không phải các nhãn trạng thái/mã nguồn nhập mẫu.
            if (!IsUnclassifiedThreatValue(raw))
            {
                string category = RecognizeThreatType(raw);
                if (category != null) return category;
                string clean = raw.Replace('\r', ' ').Replace('\n', ' ').Trim();
                return clean.Length > 60 ? clean.Substring(0, 60) : clean;
            }
            // Family, MalwareName, ThreatName: chỉ gán loại khi tên chứa từ khóa
            // phân loại tường minh. Không coi "UserVerified" là loại mã độc.
            for (int i = 1; i < candidates.Length; i++)
            {
                string item = StripImportPrefix(candidates[i]);
                string category = RecognizeThreatType(item);
                if (category != null) return category;
            }
            return null;
        }

        private static string StripImportPrefix(string value)
        {
            string name = (value ?? "").Trim();
            foreach (string prefix in new[] { "UserVerified.", "UserSuspected.",
                "ImportedHash.", "UserApproved.", "AdminReview." })
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return name.Substring(prefix.Length);
            return name;
        }

        private static bool IsUnclassifiedThreatValue(string value)
        {
            string text = (value ?? "").Trim().ToLowerInvariant();
            if (text.Length == 0) return true;
            // Trạng thái xét duyệt/nguồn nhập KHÔNG PHẢI tên loại mã độc.
            string[] placeholders = { "khác", "other", "unknown", "undefined", "n/a",
                "chưa xác định", "chưa phân loại", "generic", "malware", "malicious",
                "suspicious", "heuristic", "unclassified", "userverified", "usersuspected",
                "userapproved", "adminreview", "pendingreview", "pending", "verified",
                "approved", "user submission", "usersubmission", "importedhash" };
            return placeholders.Any(x => text == x || text.StartsWith(x + ".", StringComparison.Ordinal));
        }

        private static string RecognizeThreatType(string value)
        {
            string s = (value ?? "").Trim().ToLowerInvariant();
            if (IsUnclassifiedThreatValue(s)) return null;
            // Ưu tiên loại cụ thể trước "Trojan" hoặc "Virus" chung.
            if (s.Contains("ransom")) return "Ransomware";
            if (s.Contains("backdoor")) return "Backdoor";
            if (s.Contains("downloader")) return "Downloader";
            if (s.Contains("dropper")) return "Dropper";
            if (s.Contains("infostealer") || s.Contains("info-stealer") || s.Contains("stealer")) return "Stealer";
            if (s.Contains("keylogger")) return "Keylogger";
            if (s.Contains("spyware") || s.Contains("spy.")) return "Spyware";
            if (s.Contains("adware")) return "Adware";
            if (s.Contains("rootkit")) return "Rootkit";
            if (s.Contains("worm")) return "Worm";
            if (s.Contains("coinminer") || s.Contains("cryptominer") || s.Contains("miner")) return "Miner";
            if (s.Contains("hacktool") || s.Contains("hack-tool")) return "HackTool";
            if (s.Contains("riskware")) return "Riskware";
            if (s.Contains("pua") || s.Contains("pup") || s.Contains("potentially unwanted")) return "PUA/PUP";
            if (s.Contains("exploit")) return "Exploit";
            if (s.Contains("botnet")) return "Botnet";
            if (s.Contains("banker")) return "Banker";
            if (s.Contains("wiper")) return "Wiper";
            if (s.Contains("trojan")) return "Trojan";
            if (s.Contains("virus")) return "Virus";
            return null;
        }

        private static Color DashboardTypeColor(string name)
        {
            if (string.Equals(name, UnclassifiedThreatLabel, StringComparison.OrdinalIgnoreCase))
                return DUnclassified;
            if (string.Equals(name, OtherThreatTypesLabel, StringComparison.OrdinalIgnoreCase))
                return DOtherCategories;
            // Ổn định giữa biểu đồ ngày và danh sách; không phụ thuộc thứ tự lọc.
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in (name ?? "").ToUpperInvariant())
                    hash = (hash ^ c) * 16777619;
                return DashboardCategoryColors[(int)(hash % (uint)DashboardCategoryColors.Length)];
            }
        }

        private static bool DashboardQuarantineSuccessful(string status, string action)
        {
            return string.Equals(status, "Quarantined", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Restored", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Deleted", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(action, "Deleted", StringComparison.OrdinalIgnoreCase);
        }

        private static bool DashboardQuarantineFailed(string status, string action)
        {
            return string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "QuarantineFailed", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(action, "QuarantineFailed", StringComparison.OrdinalIgnoreCase);
        }

        private static bool DashboardQuarantinePending(string status, string action)
        {
            return string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Processing", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(action, "Pending", StringComparison.OrdinalIgnoreCase);
        }

        private void DashboardMetrics_MouseMove(object sender, MouseEventArgs e)
        {
            if (dashboardCanvas == null || dashboardCanvas.IsDisposed) return;
            float scale = Math.Max(.02f, dashboardDrawScaleY);
            Point cursor = new Point((int)(e.X / scale), (int)(e.Y / scale));
            string hint = "";
            // Duyệt ngược: thanh/cột/điểm chính xác được thêm sau vùng ngày,
            // nên luôn được ưu tiên so với tooltip tổng hợp của cả ngày.
            for (int i = dashboardHoverRegions.Count - 1; i >= 0; i--)
            {
                if (dashboardHoverRegions[i].Bounds.Contains(cursor))
                {
                    hint = dashboardHoverRegions[i].Hint;
                    break;
                }
            }
            if (string.Equals(dashboardMetricHint, hint, StringComparison.Ordinal)) return;
            dashboardMetricHint = hint;
            dashboardToolTip.Hide(dashboardCanvas);
            if (!string.IsNullOrEmpty(hint))
                dashboardToolTip.Show(hint, dashboardCanvas,
                    Math.Min(e.X + 16, Math.Max(8, dashboardCanvas.Width - 210)),
                    Math.Min(e.Y + 16, Math.Max(8, dashboardCanvas.Height - 85)), 7000);
        }

        // Cân bằng diện tích: biểu đồ phía trên ~54%, chỉ số phía dưới ~46%.
        // Khi cửa sổ rất thấp, vẫn chừa đủ chiều cao cho trục và chú giải.
        private static int DashboardTopChartHeight(int availableHeight)
        {
            int height = Math.Max(360, availableHeight);
            return Math.Max(205, Math.Min(height - 146,
                (int)Math.Round((height - 14) * .54)));
        }

        private void DrawDashboardFitted(Graphics g, Rectangle bounds)
        {
            // Nén đều hai chiều ở form hẹp: các phần của biểu đồ không bị đè
            // lên nhau (kể cả donut, nhãn trạng thái, và cột số liệu).
            float scale = Math.Min(1f, Math.Min(bounds.Height / 360f, bounds.Width / 1120f));
            scale = Math.Max(.02f, scale);
            dashboardDrawScaleY = scale;
            dashboardHoverRegions.Clear();
            var state = g.Save();
            try
            {
                if (scale < 1f) g.ScaleTransform(scale, scale);
                DrawDashboard(g, new Rectangle(0, 0,
                    Math.Max(1120, (int)Math.Ceiling(bounds.Width / scale)),
                    Math.Max(360, (int)Math.Ceiling(bounds.Height / scale))));
            }
            finally { g.Restore(state); }
        }

        // Chỉ hiện tối đa 5 dòng: 4 loại mã độc có tên phổ biến nhất và
        // 1 dòng "Chưa phân loại" (kể cả khi số lượng bằng 0).
        // Không tự thêm "Các loại khác" ở biểu đồ top 5; danh sách Xem tất cả
        // vẫn liệt kê đủ các loại trong dữ liệu thật.
        private static KeyValuePair<string, long>[] DashboardPopularCategoryRows(DashboardSnapshot data,
            int availableHeight)
        {
            if (data.Detections <= 0 && data.Categories.Count == 0 && data.UnclassifiedCount <= 0)
                return new KeyValuePair<string, long>[0];
            var rows = data.Categories.Where(p => p.Value > 0 &&
                    !string.Equals(p.Key, UnclassifiedThreatLabel, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.Value)
                .ThenBy(p => p.Key, StringComparer.CurrentCultureIgnoreCase)
                .Take(4).ToList();
            rows.Add(new KeyValuePair<string, long>(UnclassifiedThreatLabel,
                Math.Max(0L, data.UnclassifiedCount)));
            return rows.OrderByDescending(p => p.Value)
                .ThenBy(p => p.Key, StringComparer.CurrentCultureIgnoreCase).ToArray();
        }

        private static string[] DashboardDailyThreatLegend(DashboardSnapshot data)
        {
            // Nếu có nhóm chưa phân loại, luôn hiện trong chú giải theo ngày.
            // Giữ tối đa 4 loại có tên để không tràn chiều ngang của biểu đồ.
            var names = data.Categories.Where(p => p.Value > 0)
                .OrderByDescending(p => p.Value)
                .ThenBy(p => p.Key, StringComparer.CurrentCultureIgnoreCase)
                .Take(data.UnclassifiedCount > 0 ? 4 : 5)
                .Select(p => p.Key).ToList();
            if (data.UnclassifiedCount > 0) names.Add(UnclassifiedThreatLabel);
            return names.ToArray();
        }

        private static int[] DashboardDailyThreatValues(DashboardSnapshot data, string category)
        {
            return category == UnclassifiedThreatLabel
                ? data.UnclassifiedByDay : data.CategoriesByDay[category];
        }

        private void DrawDashboard(Graphics g, Rectangle bounds)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.White);
            DashboardSnapshot d = dashboardData;
            if (!d.SqlConnected)
            {
                DrawText(g, d.Error == null ? "Đang lấy số liệu từ SQL Server..." :
                    "Không thể kết nối SQL Server. Nhấn 'Xem lỗi' để biết nguyên nhân cụ thể.",
                    new Rectangle(24, 48, bounds.Width - 48, 52), 11.5f, false, Muted);
                return;
            }
            int w = bounds.Width;
            int gap = 10;
            int left = 1;
            int topHeight = DashboardTopChartHeight(bounds.Height);
            int aWidth = (int)Math.Round((w - 2 * gap - 2) * .38);
            int bWidth = (int)Math.Round((w - 2 * gap - 2) * .36);
            var first = new Rectangle(left, 2, aWidth, topHeight);
            var second = new Rectangle(first.Right + gap, 2, bWidth, topHeight);
            var third = new Rectangle(second.Right + gap, 2, w - second.Right - gap - 2, topHeight);
            DrawChartCard(g, first, "Số lượt quét theo ngày", d.ScansLoaded ? "Tổng " + N(d.ScanCount) + " lượt quét" : "Chưa có dữ liệu");
            DrawLegend(g, first.Left + 18, first.Top + 51,
                new[] { "Quét nhanh", "Quét toàn bộ", "Quét khác" },
                new[] { DBlue, DTeal, DOrange });
            DrawLineChart(g, new Rectangle(first.Left + 21, first.Top + 78, first.Width - 38, first.Height - 101),
                new[] { d.Quick, d.Full, d.Other }, new[] { DBlue, DTeal, DOrange },
                new[] { "Quét nhanh", "Quét toàn bộ", "Quét khác" }, d.Labels, d.Days);

            DrawChartCard(g, second, "Mối đe dọa đã phát hiện", d.ThreatsLoaded ? "Tổng " + N(d.Detections) + " phát hiện" : "Chưa có dữ liệu");
            // "Chưa phân loại" là một nhóm dữ liệu thực sự, không chỉ là chú thích:
            // ngày nào thiếu loại hoặc thiếu liên kết thì phần đếm nằm đúng bucket ngày đó.
            var topThreatTypes = DashboardDailyThreatLegend(d);
            bool multiLineLegend = topThreatTypes.Length > 3 &&
                topThreatTypes.Contains(UnclassifiedThreatLabel);
            DrawDynamicThreatLegend(g, new Rectangle(second.Left + 12, second.Top + 51,
                    second.Width - 24, multiLineLegend ? 43 : 26), topThreatTypes);
            DrawBarChart(g, new Rectangle(second.Left + 20,
                    second.Top + (multiLineLegend ? 100 : 80), second.Width - 34,
                    second.Height - (multiLineLegend ? 123 : 103)),
                topThreatTypes.Select(name => DashboardDailyThreatValues(d, name)).ToArray(),
                topThreatTypes.Select(DashboardTypeColor).ToArray(), topThreatTypes, d.Labels, d.Days);
            if (d.UnclassifiedCount > 0)
                DrawText(g, N(d.UnclassifiedCount) + " chưa phân loại", new Rectangle(second.Left + 15,
                    second.Bottom - 21, second.Width - 30, 16), 8f, false, Muted, StringAlignment.Far);

            DrawChartCard(g, third, "Loại mã độc phổ biến",
                d.ThreatsLoaded ? "Tổng " + N(d.Detections) + " phát hiện" : "Chưa có dữ liệu");
            DrawHorizontalCategoryChart(g, new Rectangle(third.Left + 14, third.Top + 51,
                third.Width - 26, third.Height - 89), d);

            int lowerY = first.Bottom + gap;
            int lowerHeight = Math.Max(90, bounds.Height - lowerY - 3);
            int donutWidth = (int)((w - 3 * gap - 2) * .285);
            int metricsWidth = (int)((w - 3 * gap - 2) * .40);
            var quarantine = new Rectangle(left, lowerY, donutWidth, lowerHeight);
            var metrics = new Rectangle(quarantine.Right + gap, lowerY, metricsWidth, lowerHeight);
            var protection = new Rectangle(metrics.Right + gap, lowerY,
                w - metrics.Right - gap - 2, lowerHeight);
            DrawChartCard(g, quarantine, "Tỷ lệ cách ly thành công", "Toàn hệ thống");
            long attempts = d.Quarantined + d.QuarantineFailed;
            float rate = attempts > 0 ? d.Quarantined / (float)attempts : 0;
            int donutSize = Math.Min(138, Math.Min(Math.Max(72, lowerHeight - 76),
                Math.Max(65, quarantine.Width - 226)));
            int donutY = quarantine.Top + (lowerHeight - donutSize) / 2 + 7;
            Rectangle quarantineDonutBounds = new Rectangle(quarantine.Left + 20,
                donutY, donutSize, donutSize);
            DrawDonut(g, quarantineDonutBounds,
                !d.QuarantineStatsLoaded || attempts == 0 ? "—" : Math.Round(rate * 100).ToString("0") + "%", rate, DGreen);
            AddDashboardHover(quarantineDonutBounds,
                "Tỷ lệ cách ly thành công: " + (attempts > 0 ? (rate * 100).ToString("0.#") + "%" : "Chưa có lượt xử lý") +
                "\nThành công: " + N(d.Quarantined) + " | Lỗi: " + N(d.QuarantineFailed) +
                "\nChưa cách ly: " + N(d.Unhandled) + " (không tính trong tỷ lệ)");
            int statusRowHeight = DashboardStatusRowHeight(
                lowerHeight < 170 ? Math.Min(124, lowerHeight) : lowerHeight);
            int quarantineStatusY = quarantine.Top + Math.Max(41,
                Math.Min(lowerHeight - 4 * statusRowHeight - 7,
                    (lowerHeight - 4 * statusRowHeight) / 2 + 6));
            DrawTinyStatus(g, quarantine.Left + donutSize + 32,
                quarantineStatusY,
                Math.Max(150, quarantine.Width - donutSize - 43),
                lowerHeight < 170 ? Math.Min(124, lowerHeight) : lowerHeight,
                new[] { "Cách ly thành công", "Cách ly lỗi", "Đang xử lý", "Chưa cách ly" },
                d.QuarantineStatsLoaded ? new[] { N(d.Quarantined), N(d.QuarantineFailed), N(d.QuarantinePending), N(d.Unhandled) }
                                : new[] { "—", "—", "—", "—" },
                new[] { DGreen, DRed, Color.FromArgb(146, 163, 187), DOrange });
            DrawMiniMetrics(g, metrics, d);
            if (!d.ScansLoaded) DrawText(g, "Không tải được số liệu phiên quét", new Rectangle(first.Left + 20, first.Top + 96, first.Width - 40, 35), 9.4f, true, DRed);
            if (!d.ThreatsLoaded) DrawText(g, "Không tải được dữ liệu phát hiện", new Rectangle(second.Left + 20, second.Top + 96, second.Width - 40, 35), 9.4f, true, DRed);
            DrawChartCard(g, protection, "Trạng thái cấu hình bảo vệ", "");
            int available = d.SettingsLoaded ? d.Protection.Count(v => v.HasValue) : 0;
            int enabled = d.Protection.Count(v => v.HasValue && v.Value);
            float ratio = available > 0 ? enabled / (float)available : 0;
            int protectionDonut = Math.Min(138, Math.Max(73, lowerHeight - 76));
            Rectangle protectionDonutBounds = new Rectangle(protection.Left + 16,
                protection.Top + (lowerHeight - protectionDonut) / 2 + 7,
                protectionDonut, protectionDonut);
            DrawDonut(g, protectionDonutBounds,
                available == 0 ? "—" : Math.Round(100 * ratio).ToString("0") + "%", ratio, DGreen);
            AddDashboardHover(protectionDonutBounds,
                "Trạng thái bảo vệ: " + N(enabled) + "/" + N(available) + " tính năng đang bật" +
                "\nTỷ lệ: " + (available > 0 ? (ratio * 100).ToString("0.#") + "%" : "Chưa có dữ liệu"));
            DrawTinyStatus(g, protection.Left + protectionDonut + 33,
                protection.Top + Math.Max(32, (lowerHeight - 4 * DashboardStatusRowHeight(lowerHeight)) / 2 + 6),
                protection.Width - protectionDonut - 43, lowerHeight,
                new[] { "Bảo vệ thời gian thực", "Bảo vệ tệp", "Bảo vệ web", "Chống ransomware" },
                d.Protection.Select(v => d.SettingsLoaded && v.HasValue ? (v.Value ? "Bật" : "Tắt") : "—").ToArray(),
                d.Protection.Select(v => !v.HasValue ? Muted : v.Value ? DGreen : DRed).ToArray());
        }

        private static string N(long value) { return value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")); }

        // Giữ nguyên toàn bộ chữ và chữ số. Nếu ô vẽ hẹp thì giảm nhẹ cỡ chữ
        // để vừa ô, tuyệt đối không thay phần cuối bằng dấu ba chấm (...).
        private static void DrawText(Graphics g, string text, Rectangle r, float size, bool bold, Color color,
            StringAlignment align = StringAlignment.Near)
        {
            if (r.Width <= 2 || r.Height <= 2) return;
            string value = text ?? "";
            FontStyle style = bold ? FontStyle.Bold : FontStyle.Regular;
            using (var format = new StringFormat {
                Alignment = align, LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.None,
                FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces
            })
            using (var brush = new SolidBrush(color))
            {
                // Trừ đệm đo chữ để tránh phần cuối bị cắt bởi GDI+ ở màn hình DPI cao.
                float limitW = Math.Max(1, r.Width - 3);
                float limitH = Math.Max(1, r.Height - 2);
                float fittedSize = Theme.ReadableFontSize(size);
                while (fittedSize > 6.5f)
                {
                    using (var testFont = new Font(Theme.UiFontFamily, fittedSize, style))
                    {
                        SizeF measured = g.MeasureString(value, testFont, new PointF(0, 0), format);
                        if (measured.Width <= limitW && measured.Height <= limitH) break;
                    }
                    fittedSize -= .35f;
                }
                using (var fittedFont = new Font(Theme.UiFontFamily, Math.Max(6.5f, fittedSize), style))
                    g.DrawString(value, fittedFont, brush, r, format);
            }
        }

        private static void DrawOutline(Graphics g, Rectangle rect)
        {
            using (var border = new Pen(Color.FromArgb(220, 231, 246)))
                g.DrawRectangle(border, rect.Left, rect.Top, rect.Width - 1, rect.Height - 1);
        }

        private static void DrawChartCard(Graphics g, Rectangle rect, string title, string extra)
        {
            DrawOutline(g, rect);
            using (var brush = new SolidBrush(DBlue)) g.FillEllipse(brush, rect.Left + 12, rect.Top + 18, 9, 9);
            DrawText(g, title, new Rectangle(rect.Left + 28, rect.Top + 8, rect.Width - 37, 28),
                12.4f, true, Ink);
            if (!string.IsNullOrEmpty(extra))
                DrawText(g, extra, new Rectangle(rect.Left + 15, rect.Top + 31, rect.Width - 26, 18),
                    10.2f, false, Muted, StringAlignment.Far);
        }

        private void DrawLegend(Graphics g, int left, int top, string[] labels, Color[] colors)
        {
            int space = labels.Length == 2 ? 113 : labels.Length == 3 ? 112 : 77;
            for (int i = 0; i < labels.Length; i++)
            {
                int x = left + i * space;
                using (var b = new SolidBrush(colors[i])) g.FillEllipse(b, x, top + 5, 8, 8);
                DrawText(g, labels[i], new Rectangle(x + 13, top, space - 13, 18), 9.7f, false, Muted);
                long total = i == 0 ? dashboardData.Quick.Sum(v => (long)v) :
                    i == 1 ? dashboardData.Full.Sum(v => (long)v) :
                    dashboardData.Other.Sum(v => (long)v);
                AddDashboardHover(new Rectangle(x, top, space - 2, 19),
                    labels[i] + ": " + N(total) + " lượt trong kỳ");
            }
        }

        private static void DrawChartGrid(Graphics g, Rectangle plot, int max)
        {
            using (var pen = new Pen(Color.FromArgb(233, 239, 247)))
                for (int i = 0; i <= 4; i++)
                {
                    int y = plot.Bottom - (int)((plot.Height - 20) * i / 4f) - 15;
                    g.DrawLine(pen, plot.Left + 27, y, plot.Right - 3, y);
                    DrawText(g, (max * i / 4).ToString(), new Rectangle(plot.Left, y - 9, 26, 18),
                        9.1f, false, Muted, StringAlignment.Far);
                }
        }

        private static int ScaleMax(int[][] data)
        {
            int max = data.SelectMany(a => a).DefaultIfEmpty(0).Max();
            return Math.Max(4, (int)Math.Ceiling(max / 4.0) * 4);
        }

        private void DrawLineChart(Graphics g, Rectangle plot, int[][] series, Color[] colors,
            string[] seriesNames, string[] dates, int days)
        {
            int max = ScaleMax(series);
            DrawChartGrid(g, plot, max);
            int count = Math.Max(1, Math.Min(7, days));
            int left = plot.Left + 34, right = plot.Right - 8, bottom = plot.Bottom - 16;
            // Một vùng cho mỗi ngày, kể cả khi người dùng không rê trúng điểm.
            for (int i = 0; i < count; i++)
            {
                float cx = count == 1 ? (left + right) / 2f :
                    left + (right - left) * i / (count - 1f);
                int half = count == 1 ? Math.Max(16, (right - left) / 2) :
                    Math.Max(11, (right - left) / (count - 1) / 2);
                var hint = "Ngày " + (dates[i] ?? "") +
                    "\nQuét nhanh: " + N(series[0][i]) + " lượt" +
                    "\nQuét toàn bộ: " + N(series[1][i]) + " lượt" +
                    "\nQuét khác: " + N(series[2][i]) + " lượt";
                AddDashboardHover(new Rectangle((int)cx - half, plot.Top,
                    half * 2, plot.Height), hint);
            }
            for (int k = 0; k < series.Length; k++)
            {
                var points = new PointF[count];
                for (int i = 0; i < count; i++)
                {
                    float x = count == 1 ? (left + right) / 2f : left + (right - left) * i / (count - 1f);
                    points[i] = new PointF(x,
                        bottom - (plot.Height - 36) * series[k][i] / max);
                }
                if (count > 1)
                    using (var pen = new Pen(colors[k], 2.6f)) g.DrawLines(pen, points);
                for (int i = 0; i < count; i++)
                {
                    PointF pt = points[i];
                    using (var b = new SolidBrush(colors[k]))
                        g.FillEllipse(b, pt.X - 3.5f, pt.Y - 3.5f, 7, 7);
                    AddDashboardHover(new Rectangle((int)pt.X - 9, (int)pt.Y - 9, 19, 19),
                        seriesNames[k] + " — " + (dates[i] ?? "") + ": " +
                        N(series[k][i]) + " lượt quét");
                }
            }
            for (int i = 0; i < count; i++)
            {
                float x = count == 1 ? (left + right) / 2f : left + (right - left) * i / (count - 1f);
                DrawText(g, dates[i] ?? "", new Rectangle((int)x - 19,
                    plot.Bottom - 13, 39, 16), 8.9f, false, Muted, StringAlignment.Center);
            }
        }

        private void DrawBarChart(Graphics g, Rectangle plot, int[][] series, Color[] colors,
            string[] seriesNames, string[] dates, int days)
        {
            if (series.Length == 0)
            {
                DrawText(g, "Không có phát hiện trong kỳ", plot, 9.2f, false, Muted,
                    StringAlignment.Center);
                return;
            }
            int max = ScaleMax(series);
            DrawChartGrid(g, plot, max);
            int count = Math.Max(1, Math.Min(7, days));
            int left = plot.Left + 29, plotW = plot.Width - 38;
            int bottom = plot.Bottom - 16;
            float group = plotW / count;
            float barW = Math.Max(2.5f, Math.Min(11f, (group - 6f) / series.Length));
            for (int i = 0; i < count; i++)
            {
                float barGroupW = series.Length * (barW + 1f);
                float groupX = left + i * group + (group - barGroupW) / 2f;
                var hint = "Phát hiện ngày " + (dates[i] ?? "");
                for (int k = 0; k < series.Length; k++)
                    hint += "\n" + seriesNames[k] + ": " + N(series[k][i]);
                AddDashboardHover(new Rectangle(left + (int)(i * group),
                    plot.Top, Math.Max(1, (int)Math.Ceiling(group)), plot.Height), hint);
                for (int k = 0; k < series.Length; k++)
                {
                    float h = Math.Max(0, (plot.Height - 36f) * series[k][i] / max);
                    using (var b = new SolidBrush(colors[k]))
                        g.FillRectangle(b, groupX + k * (barW + 1f), bottom - h, barW, h);
                    if (series[k][i] > 0)
                        AddDashboardHover(new Rectangle((int)(groupX + k * (barW + 1f)) - 3,
                            (int)(bottom - h) - 3, Math.Max(9, (int)Math.Ceiling(barW) + 6),
                            Math.Max(9, (int)Math.Ceiling(h) + 6)),
                            seriesNames[k] + " — " + (dates[i] ?? "") + ": " +
                            N(series[k][i]) + " phát hiện");
                }
                DrawText(g, dates[i] ?? "", new Rectangle(left + (int)(i * group), plot.Bottom - 13,
                    (int)group, 16), 8.9f, false, Muted, StringAlignment.Center);
            }
        }

        private void DrawDynamicThreatLegend(Graphics g, Rectangle bounds, string[] names)
        {
            if (names.Length == 0)
            {
                DrawText(g, "Chưa có loại được xác định", bounds, 8.4f, false, Muted);
                return;
            }
            // Tránh "Chưa phân..." trong chú giải: nếu có đủ 4–5 nhóm và nhóm
            // chưa phân loại, sử dụng 2 hàng thay vì ép nhãn dài vào 1 hàng.
            int columns = names.Length > 3 && names.Contains(UnclassifiedThreatLabel)
                ? 3 : names.Length;
            int cellWidth = Math.Max(50, bounds.Width / Math.Max(1, columns));
            for (int i = 0; i < names.Length; i++)
            {
                int x = bounds.Left + (i % columns) * cellWidth;
                int y = bounds.Top + (i / columns) * 21;
                using (var b = new SolidBrush(DashboardTypeColor(names[i])))
                    g.FillEllipse(b, x, y + 6, 8, 8);
                DrawText(g, names[i], new Rectangle(x + 12, y,
                    Math.Max(8, cellWidth - 13), 20),
                    names[i] == UnclassifiedThreatLabel ? 8.6f : 9.1f, false, Muted);
                long total = names[i] == UnclassifiedThreatLabel ?
                    dashboardData.UnclassifiedCount :
                    (dashboardData.Categories.TryGetValue(names[i], out long amount) ? amount : 0L);
                AddDashboardHover(new Rectangle(x, y, cellWidth - 2, 20),
                    names[i] + ": " + N(total) + " phát hiện trong kỳ");
            }
        }

        private void DrawHorizontalCategoryChart(Graphics g, Rectangle r,
            DashboardSnapshot data)
        {
            var rows = DashboardPopularCategoryRows(data, r.Height);
            if (rows.Length == 0)
            {
                DrawText(g, "Chưa có phát hiện trong khoảng ngày", r, 9.2f, false, Muted,
                    StringAlignment.Center);
                return;
            }
            long max = Math.Max(1L, rows.Max(p => p.Value));
            // Phân bố đều 5 nhóm trên chiều cao thực tế; không dồn lên trên để
            // rồi bỏ trống nửa dưới thẻ khi màn hình lớn.
            int rowHeight = Math.Max(21,
                (r.Height - 4) / Math.Max(1, rows.Length));
            int barHeight = Math.Max(8, Math.Min(31, (int)(rowHeight * .54f)));
            // Cố định số dòng <= 5; đo số dài nhất và nhường đủ chiều rộng
            // để ví dụ 3.421, 64.459 hoặc 1.234.567 không bị cắt thành "...".
            int widestCount = 0;
            using (var countFont = new Font(Theme.UiFontFamily, 10.55f, FontStyle.Bold))
                foreach (var entry in rows)
                    widestCount = Math.Max(widestCount,
                        (int)Math.Ceiling(g.MeasureString(N(entry.Value), countFont).Width));
            int countWidth = Math.Max(57, Math.Min(r.Width / 3, widestCount + 7));
            int labelWidth = Math.Max(73, Math.Min(126, r.Width - countWidth - 20 - 24));
            int barLeft = r.Left + labelWidth + 6;
            int barWidth = Math.Max(8, r.Right - barLeft - countWidth - 6);
            for (int i = 0; i < rows.Length; i++)
            {
                int y = r.Top + i * rowHeight;
                int barY = y + (rowHeight - barHeight) / 2;
                string name = rows[i].Key;
                DrawText(g, name, new Rectangle(r.Left, y, labelWidth, rowHeight),
                    name == UnclassifiedThreatLabel ? 9.4f : 10.1f, false, Ink);
                using (var background = new SolidBrush(Color.FromArgb(239, 244, 249)))
                    g.FillRectangle(background, barLeft, barY, barWidth, barHeight);
                using (var fill = new SolidBrush(DashboardTypeColor(name)))
                    g.FillRectangle(fill, barLeft, barY,
                        (int)Math.Round(barWidth * rows[i].Value / (double)max),
                        barHeight);
                DrawText(g, N(rows[i].Value),
                    new Rectangle(r.Right - countWidth, y, countWidth, rowHeight),
                    9.9f, true, Ink, StringAlignment.Far);
                AddDashboardHover(new Rectangle(r.Left, y, r.Width, rowHeight),
                    name + ": " + N(rows[i].Value) + " phát hiện" +
                    (data.Detections > 0 ? "\nChiếm " +
                        (100d * rows[i].Value / data.Detections).ToString("0.##") +
                        "% tổng phát hiện" : ""));
            }
        }

        private void ShowDashboardCategories()
        {
            if (!AdminSecurity.IsAuthenticated) return;
            var snapshot = dashboardData;
            using (var form = new Form { Text = "Chi tiết loại mã độc đã phát hiện",
                StartPosition = FormStartPosition.CenterParent, BackColor = Color.White,
                Font = new Font(Theme.UiFontFamily, 10.65f), MinimumSize = new Size(620, 400),
                ClientSize = new Size(760, 520) })
            {
                var heading = new Label {
                    Dock = DockStyle.Top, Height = 75, AutoSize = false,
                    Padding = new Padding(18, 10, 18, 0),
                    Text = "Danh sách loại mã độc phát hiện trong khoảng ngày đã chọn" +
                        Environment.NewLine +
                        string.Format("{0:N0} phát hiện có loại • {1:N0} chưa phân loại • {2:N0} tổng theo lịch sử",
                            snapshot.Categories.Values.Sum(), snapshot.UnclassifiedCount, snapshot.Detections),
                    ForeColor = Ink
                };
                var table = new DataTable();
                table.Columns.Add("Loại mã độc", typeof(string));
                table.Columns.Add("Số lượt phát hiện", typeof(long));
                foreach (var item in snapshot.Categories.OrderByDescending(p => p.Value)
                    .ThenBy(p => p.Key, StringComparer.CurrentCultureIgnoreCase))
                    table.Rows.Add(item.Key, item.Value);
                if (snapshot.UnclassifiedCount > 0)
                    table.Rows.Add(UnclassifiedThreatLabel, snapshot.UnclassifiedCount);
                var grid = new DataGridView { Dock = DockStyle.Fill,
                    DataSource = table, ReadOnly = true, AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false, MultiSelect = false,
                    RowHeadersVisible = false, BackgroundColor = Color.White,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
                var footer = new Panel { Dock = DockStyle.Bottom, Height = 54, BackColor = Color.White };
                var explanation = new Label { Dock = DockStyle.Fill,
                    Padding = new Padding(18, 8, 5, 5), ForeColor = Muted,
                    Text = "Chưa phân loại gồm thiếu loại và thiếu chi tiết liên kết ScanID " +
                        "(" + N(snapshot.MissingDetailCount) + " phát hiện). Tổng lấy từ ScanHistory.ThreatCount." };
                var review = new Button { Dock = DockStyle.Right, Width = 174, Height = 39,
                    Text = "Phân loại chữ ký", BackColor = Color.White,
                    ForeColor = DBlue, FlatStyle = FlatStyle.Flat };
                review.Click += (sender, args) => {
                    if (!AdminSecurity.IsAuthenticated) { form.Close(); return; }
                    form.Close();
                    ManageUnclassifiedSignatures();
                };
                footer.Controls.Add(explanation);
                footer.Controls.Add(review);
                form.Controls.Add(grid);
                form.Controls.Add(footer);
                form.Controls.Add(heading);
                form.ShowDialog(FindForm());
            }
        }

        private static void DrawDonut(Graphics g, Rectangle rect, string value, float progress, Color fill)
        {
            using (var bg = new Pen(Color.FromArgb(226, 237, 244), 10f))
            using (var arc = new Pen(fill, 10f))
            {
                bg.StartCap = bg.EndCap = LineCap.Round;
                arc.StartCap = arc.EndCap = LineCap.Round;
                g.DrawArc(bg, rect, -90, 360);
                if (progress > 0) g.DrawArc(arc, rect, -90, Math.Min(359.95f, progress * 360));
            }
            DrawText(g, value,
                new Rectangle(rect.Left + 4, rect.Top + (rect.Height - 42) / 2,
                    rect.Width - 8, 42), 16.8f, true, Ink, StringAlignment.Center);
        }

        private static int DashboardStatusRowHeight(int totalHeight)
        {
            return Math.Max(21, Math.Min(34, (totalHeight - 39) / 4));
        }

        private void DrawTinyStatus(Graphics g, int x, int y, int width, int availableHeight,
            string[] labels, string[] values, Color[] colors)
        {
            int height = DashboardStatusRowHeight(availableHeight);
            for (int i = 0; i < labels.Length; i++)
            {
                using (var br = new SolidBrush(colors[i])) g.FillEllipse(br, x, y + i * height + 9, 7, 7);
                int measuredWidth;
                using (var numericFont = new Font(Theme.UiFontFamily, 10.75f, FontStyle.Bold))
                    measuredWidth = (int)Math.Ceiling(g.MeasureString(values[i], numericFont).Width) + 5;
                int valueWidth = Math.Max(58, Math.Min(Math.Max(60, width / 2), measuredWidth));
                DrawText(g, labels[i], new Rectangle(x + 13, y + i * height,
                    Math.Max(24, width - valueWidth - 16), height), 10.8f, false, Muted);
                DrawText(g, values[i], new Rectangle(x + width - valueWidth, y + i * height,
                    valueWidth, height), 10.1f, true, colors[i], StringAlignment.Far);
                AddDashboardHover(new Rectangle(x, y + i * height, width, height),
                    labels[i] + ": " + values[i]);
            }
        }

        /// <summary>
        /// Icon dashboard tự vẽ, giữ nét ổn định ở những mức DPI khác nhau.
        /// 0: tệp đã quét; 1: khiên phát hiện/đã chặn; 2: thời gian quét.
        /// Dùng cùng hệ tọa độ 40x40, không cần ảnh và không phụ thuộc font.
        /// </summary>
        private static void DrawMetricIcon(Graphics graphics, Rectangle bounds, int kind)
        {
            if (graphics == null || bounds.Width < 12 || bounds.Height < 12) return;
            var state = graphics.Save();
            try
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.TranslateTransform(bounds.Left, bounds.Top);
                graphics.ScaleTransform(bounds.Width / 40f, bounds.Height / 40f);
                var blue = Color.FromArgb(31, 123, 239);
                using (var background = new SolidBrush(Color.FromArgb(233, 242, 253)))
                using (var backgroundPath = new GraphicsPath())
                {
                    // Nền xanh nhạt với góc bo giống các thẻ của dashboard.
                    backgroundPath.AddArc(0, 0, 12, 12, 180, 90);
                    backgroundPath.AddArc(28, 0, 12, 12, 270, 90);
                    backgroundPath.AddArc(28, 28, 12, 12, 0, 90);
                    backgroundPath.AddArc(0, 28, 12, 12, 90, 90);
                    backgroundPath.CloseFigure();
                    graphics.FillPath(background, backgroundPath);
                }
                using (var pen = new Pen(blue, 2.35f))
                {
                    pen.LineJoin = LineJoin.Round;
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    if (kind == 0)
                    {
                        // Tài liệu: đường gấp góc và hai nét nội dung.
                        graphics.DrawLines(pen, new[] {
                            new PointF(13f, 9f), new PointF(23f, 9f),
                            new PointF(29f, 15f), new PointF(29f, 31f),
                            new PointF(13f, 31f), new PointF(13f, 9f) });
                        graphics.DrawLines(pen, new[] {
                            new PointF(23f, 9f), new PointF(23f, 15f),
                            new PointF(29f, 15f) });
                        graphics.DrawLine(pen, 17f, 21f, 25f, 21f);
                        graphics.DrawLine(pen, 17f, 26f, 25f, 26f);
                    }
                    else if (kind == 1)
                    {
                        // Khiên bảo vệ: hình chính và dấu tích an toàn.
                        graphics.DrawLines(pen, new[] {
                            new PointF(20f, 7f), new PointF(30f, 12f),
                            new PointF(29f, 22f), new PointF(26f, 28f),
                            new PointF(20f, 33f), new PointF(14f, 28f),
                            new PointF(11f, 22f), new PointF(10f, 12f),
                            new PointF(20f, 7f) });
                        graphics.DrawLines(pen, new[] {
                            new PointF(15.5f, 20f), new PointF(19f, 23.5f),
                            new PointF(25.5f, 16.5f) });
                    }
                    else
                    {
                        // Đồng hồ: vòng tròn và kim phút/giờ.
                        graphics.DrawEllipse(pen, 10f, 10f, 20f, 20f);
                        graphics.DrawLines(pen, new[] {
                            new PointF(20f, 14f), new PointF(20f, 20f),
                            new PointF(25f, 23f) });
                    }
                }
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        private static string DashboardDuration(double seconds)
        {
            if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return "—";
            long wholeSeconds = (long)Math.Round(seconds, MidpointRounding.AwayFromZero);
            TimeSpan elapsed = TimeSpan.FromSeconds(wholeSeconds);
            return elapsed.TotalHours >= 1
                ? string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}",
                    (long)elapsed.TotalHours, elapsed.Minutes, elapsed.Seconds)
                : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}",
                    (long)elapsed.TotalMinutes, elapsed.Seconds);
        }

        private static bool DashboardIsCompletedScan(string status)
        {
            switch ((status ?? "").Trim().ToLowerInvariant())
            {
                case "running":
                case "failed":
                case "error":
                case "cancelled":
                case "canceled":
                case "aborted":
                case "interrupted":
                case "stopped":
                    return false;
                default:
                    return true;
            }
        }

        private void DrawMiniMetrics(Graphics g, Rectangle bounds, DashboardSnapshot d)
        {
            int gap = 10;
            int each = (bounds.Width - gap * 2) / 3;
            string[] titles = { "Tổng tệp đã quét", "Mối đe dọa đã chặn", "Thời gian quét TB" };
            string[] values = { d.ScansLoaded ? N(d.FilesScanned) : "—",
                d.BlockedLoaded ? N(d.Blocked) : "—",
                d.ScansLoaded && d.HasAverage ? DashboardDuration(d.AverageSeconds) : "—" };
            for (int i = 0; i < 3; i++)
            {
                Rectangle box = new Rectangle(bounds.Left + i * (each + gap), bounds.Top, each, bounds.Height);
                string extra = i == 0 ? "Số lượt tệp quét từ các phiên hoàn tất" :
                    i == 1 ? "Mối đe dọa được chặn hoặc xử lý" :
                    "Thời gian trung bình các phiên quét hoàn tất";
                AddDashboardHover(box, titles[i] + ": " + values[i] + "\n" + extra);
                DrawOutline(g, box);
                // Vẽ icon trực tiếp bằng GDI+: không phụ thuộc Segoe MDL2,
                // FontAwesome hay bitmap có thể thiếu khi chạy trên máy khác.
                int iconSize = Math.Max(36, Math.Min(50, (int)(box.Height * .25f)));
                // Nhóm biểu tượng + nhãn + số theo một cụm được căn giữa.
                // Tránh các vùng trống lớn giữa ba thành phần ở cửa sổ cao.
                int clusterHeight = iconSize + 14 + 28 + 8 + 45;
                int iconTop = box.Top + Math.Max(12, (box.Height - clusterHeight) / 2);
                DrawMetricIcon(g, new Rectangle(box.Left + 13, iconTop, iconSize, iconSize), i);
                int labelY = iconTop + iconSize + 14;
                DrawText(g, titles[i], new Rectangle(box.Left + 13, labelY, box.Width - 24, 28),
                    10.6f, false, Muted);
                int valueY = labelY + 36;
                DrawText(g, values[i], new Rectangle(box.Left + 13, valueY,
                    box.Width - 24, Math.Min(47, Math.Max(26, box.Bottom - valueY - 9))),
                    19f, true, Ink);
            }
        }
    }
}
