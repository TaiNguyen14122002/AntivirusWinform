using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Xml;
using ScanAndRemoveVirus.Database;

namespace ScanAndRemoveVirus.Services
{
    public sealed class ScanSchedule
    {
        public long Id;
        public string Name, ScanType, TargetPath, Frequency;
        public DateTime StartTime;
        public bool Enabled;
    }

    /// <summary>Đăng ký lịch chạy bằng Task Scheduler, không dùng Timer của giao diện.</summary>
    public static class ScanScheduleService
    {
        // TargetPath trong SQL hiện là NVARCHAR(1000). Mã hóa danh sách bằng dấu xuống
        // dòng (ký tự Windows không cho phép trong tên tệp) và tiền tố phiên bản.
        // Lịch cũ chỉ có một đường dẫn vẫn đọc được mà không cần migration SQL.
        private const string MultiTargetPrefix = "MULTI_TARGETS_V1:\n";
        private const int TargetFieldLimit = 1000;

        public static string[] DecodeTargets(string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return new string[0];
            string payload = stored.StartsWith(MultiTargetPrefix, StringComparison.Ordinal)
                ? stored.Substring(MultiTargetPrefix.Length) : stored;
            return stored.StartsWith(MultiTargetPrefix, StringComparison.Ordinal)
                ? payload.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim()).Where(p => p.Length > 0).ToArray()
                : new[] { payload.Trim() };
        }

        public static string[] NormalizeTargets(IEnumerable<string> paths)
        {
            if (paths == null) return new string[0];
            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            foreach (string candidate in paths)
            {
                string path = (candidate ?? "").Trim();
                if (path.Length == 0) continue;
                if (path.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
                    throw new ArgumentException("Đường dẫn mục tiêu chứa ký tự không hợp lệ.");
                if (!Path.IsPathRooted(path))
                    throw new ArgumentException("Mỗi mục tiêu quét phải có đường dẫn tuyệt đối: " + path);
                try { path = Path.GetFullPath(path); }
                catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
                { throw new ArgumentException("Đường dẫn mục tiêu không hợp lệ: " + path, ex); }
                string root = Path.GetPathRoot(path);
                if (!string.Equals(path, root, StringComparison.OrdinalIgnoreCase))
                    path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (unique.Add(path)) result.Add(path);
            }
            return result.ToArray();
        }

        public static string EncodeTargets(IEnumerable<string> paths)
        {
            string[] targets = NormalizeTargets(paths);
            if (targets.Length == 0) return null;
            string stored = targets.Length == 1 ? targets[0] :
                MultiTargetPrefix + string.Join("\n", targets);
            if (stored.Length > TargetFieldLimit)
                throw new ArgumentException("Danh sách đường dẫn vượt quá 1.000 ký tự mà cơ sở dữ liệu hiện hỗ trợ. " +
                    "Hãy chọn ít mục tiêu hơn hoặc chọn một thư mục cha chứa các tệp cần quét.");
            return stored;
        }

        public static string DescribeTargets(string stored)
        {
            string[] targets = DecodeTargets(stored);
            if (targets.Length <= 1) return targets.Length == 0 ? "—" : targets[0];
            int folders = targets.Count(Directory.Exists);
            int files = targets.Length - folders;
            return targets.Length + " mục tiêu (" + files + " tệp, " + folders + " thư mục)";
        }

        private static bool TargetsEquivalent(string left, string right)
        {
            string[] a = NormalizeTargets(DecodeTargets(left));
            string[] b = NormalizeTargets(DecodeTargets(right));
            return a.Length == b.Length && a.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .SequenceEqual(b.OrderBy(p => p, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
        }

        public static DataTable List()
        {
            // Ưu tiên dữ liệu SQL có sẵn; vẫn cho phép tạo lịch khi dbo.ScanSchedules
            // chưa tồn tại/máy tạm mất SQL, bằng tệp cấu hình riêng của người dùng.
            DataTable data = LocalScheduleStore.NewTable();
            try
            {
                using (var cn = DbConnection.Create())
                using (var cmd = new SqlCommand(@"SELECT ScheduleID, Name, ScanType, TargetPath, Frequency,
                      StartTime, LastRunAt, LastOutcome, IsEnabled FROM dbo.ScanSchedules ORDER BY ScheduleID DESC", cn))
                using (var da = new SqlDataAdapter(cmd))
                {
                    var sql = new DataTable();
                    da.Fill(sql);
                    foreach (DataRow row in sql.Rows) data.ImportRow(row);
                }
            }
            catch (SqlException) { /* SQL không khả dụng: các lịch cục bộ vẫn hiển thị. */ }
            foreach (DataRow row in LocalScheduleStore.List().Rows) data.ImportRow(row);
            return data;
        }
        /// <summary>
        /// Trả về lịch quét đang bật tiếp theo để hiển thị nhanh trên Tổng quan.
        /// Chỉ đọc bảng dbo.ScanSchedules đã tồn tại; không tạo hoặc sửa cấu trúc SQL.
        /// Kết quả dự kiến theo quy tắc lặp, thời gian chạy thực tế do Windows quyết định.
        /// </summary>
        public static string GetOverviewSummary()
        {
            DataTable data = List();
            int enabled = 0;
            DateTime? next = null;
            DateTime now = DateTime.Now;
            foreach (DataRow row in data.Rows)
            {
                if (row["IsEnabled"] == DBNull.Value || !Convert.ToBoolean(row["IsEnabled"])) continue;
                enabled++;
                DateTime start = Convert.ToDateTime(row["StartTime"]);
                string frequency = Convert.ToString(row["Frequency"]);
                DateTime? candidate = NextOccurrence(start, frequency, now);
                if (candidate.HasValue && (!next.HasValue || candidate.Value < next.Value)) next = candidate;
            }
            if (data.Rows.Count == 0) return "Chưa có lịch quét · Nhấn để đặt giờ";
            if (enabled == 0) return "Tất cả lịch quét đang tắt · Nhấn để bật";
            if (!next.HasValue) return "Không có lượt quét sắp tới · Xem lại lịch";
            return "Lần quét tới: " + next.Value.ToString("dd/MM/yyyy HH:mm", new System.Globalization.CultureInfo("vi-VN")) +
                (enabled > 1 ? "  ·  " + enabled + " lịch bật" : "");
        }

        private static DateTime? NextOccurrence(DateTime start, string frequency, DateTime now)
        {
            if (frequency == "Once") return start > now ? (DateTime?)start : null;
            if (frequency == "Daily")
            {
                DateTime value = now.Date.Add(start.TimeOfDay);
                if (value < start) value = start;
                if (value <= now) value = value.AddDays(1);
                return value;
            }
            if (frequency == "Weekly")
            {
                if (start > now) return start;
                double cycles = Math.Floor((now - start).TotalDays / 7);
                DateTime value = start.AddDays(cycles * 7);
                return value <= now ? value.AddDays(7) : value;
            }
            if (frequency == "Monthly")
            {
                int offset = Math.Max(0, (now.Year - start.Year) * 12 + now.Month - start.Month);
                DateTime first = new DateTime(start.Year, start.Month, 1);
                // Bỏ qua tháng không có ngày tương ứng (ví dụ 31/02).
                for (int i = offset; i <= offset + 13; i++)
                {
                    DateTime month = first.AddMonths(i);
                    if (start.Day > DateTime.DaysInMonth(month.Year, month.Month)) continue;
                    DateTime value = new DateTime(month.Year, month.Month, start.Day).Add(start.TimeOfDay);
                    if (value >= start && value > now) return value;
                }
            }
            return null;
        }

        public static ScanSchedule Get(long id)
        {
            if (LocalScheduleStore.Exists(id)) return LocalScheduleStore.Get(id);
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand(@"SELECT ScheduleID, Name, ScanType, TargetPath,
                       Frequency, StartTime, IsEnabled FROM dbo.ScanSchedules WHERE ScheduleID=@id", cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) throw new InvalidOperationException("Lịch không còn tồn tại trong SQL.");
                    return new ScanSchedule { Id = id, Name = Convert.ToString(rd[1]),
                        ScanType = Convert.ToString(rd[2]), TargetPath = rd.IsDBNull(3) ? null : Convert.ToString(rd[3]),
                        Frequency = Convert.ToString(rd[4]), StartTime = Convert.ToDateTime(rd[5]), Enabled = Convert.ToBoolean(rd[6]) };
                }
            }
        }
        /// <summary>
        /// Ràng buộc thực thi tại dịch vụ để lịch lưu từ bất kỳ giao diện nào cũng an toàn.
        /// Ngày giờ là thời gian địa phương của Windows Task Scheduler.
        /// </summary>
        public static void ValidateForSave(ScanSchedule schedule)
        {
            if (schedule == null) throw new ArgumentNullException("schedule");
            schedule.Name = (schedule.Name ?? "").Trim();
            if (schedule.Name.Length < 3 || schedule.Name.Length > 150 ||
                schedule.Name.Contains("\n") || schedule.Name.Contains("\r"))
                throw new ArgumentException("Tên lịch phải dài 3–150 ký tự, không được chứa ký tự xuống dòng.");
            if (schedule.ScanType != "Quick" && schedule.ScanType != "Full" && schedule.ScanType != "Custom")
                throw new ArgumentException("Chế độ quét không hợp lệ.");
            if (schedule.Frequency != "Daily" && schedule.Frequency != "Weekly" &&
                schedule.Frequency != "Monthly" && schedule.Frequency != "Once")
                throw new ArgumentException("Tần suất quét không hợp lệ.");

            // Tránh lập lịch vào quá khứ hoặc quá sát hiện tại, kể cả lịch lặp.
            DateTime current = DateTime.Now;
            if (schedule.StartTime <= current.AddMinutes(2))
                throw new ArgumentException("Thời gian bắt đầu phải muộn hơn thời gian hiện tại ít nhất 2 phút. " +
                    "Hiện tại: " + current.ToString("dd/MM/yyyy HH:mm") + ".");
            if (schedule.StartTime > current.AddYears(5))
                throw new ArgumentException("Thời gian bắt đầu chỉ được nằm trong vòng 5 năm tới.");

            if (schedule.ScanType == "Custom")
            {
                string[] targets = NormalizeTargets(DecodeTargets(schedule.TargetPath));
                if (targets.Length == 0)
                    throw new ArgumentException("Bạn phải chọn ít nhất một tệp hoặc thư mục cần quét.");
                foreach (string target in targets)
                    if (!File.Exists(target) && !Directory.Exists(target))
                        throw new ArgumentException("Mục tiêu quét không còn tồn tại hoặc không thể truy cập: " + target);
                schedule.TargetPath = EncodeTargets(targets);
            }
            else
            {
                // Không để lại đường dẫn Custom cũ khi chuyển sang Quét nhanh/Toàn bộ.
                schedule.TargetPath = null;
            }

            // Lịch cũ có thể đã trùng nhau trước khi có ràng buộc mới.
            // Vẫn cho chỉnh lịch cũ mà không bị ép đổi tên nếu giữ nguyên tên,
            // hoặc bị chặn bởi lịch trùng đã tồn tại nếu chưa sửa thông số kích hoạt.
            DataTable existing = List();
            DataRow previous = null;
            foreach (DataRow row in existing.Rows)
                if (Convert.ToInt64(row["ScheduleID"]) == schedule.Id) { previous = row; break; }
            bool retainedName = previous != null && string.Equals(
                Convert.ToString(previous["Name"]).Trim(), schedule.Name, StringComparison.OrdinalIgnoreCase);
            bool retainedTrigger = previous != null && MatchesTrigger(previous, schedule);
            foreach (DataRow row in existing.Rows)
            {
                if (Convert.ToInt64(row["ScheduleID"]) == schedule.Id) continue;
                if (!retainedName && string.Equals(Convert.ToString(row["Name"]).Trim(), schedule.Name,
                    StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Tên lịch đã tồn tại. Hãy đặt tên khác để dễ quản lý.");
                if (!retainedTrigger && MatchesTrigger(row, schedule))
                    throw new ArgumentException("Đã có lịch cùng chế độ quét, mục tiêu, tần suất và thời gian bắt đầu.");
            }
        }

        private static bool MatchesTrigger(DataRow row, ScanSchedule schedule)
        {
            string previousPath = row["TargetPath"] == DBNull.Value ? "" :
                Convert.ToString(row["TargetPath"]).Trim();
            string requestedPath = schedule.TargetPath ?? "";
            DateTime previousStart = Convert.ToDateTime(row["StartTime"]);
            return string.Equals(Convert.ToString(row["ScanType"]), schedule.ScanType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Convert.ToString(row["Frequency"]), schedule.Frequency, StringComparison.OrdinalIgnoreCase) &&
                TargetsEquivalent(previousPath, requestedPath) &&
                previousStart.Year == schedule.StartTime.Year && previousStart.Month == schedule.StartTime.Month &&
                previousStart.Day == schedule.StartTime.Day && previousStart.Hour == schedule.StartTime.Hour &&
                previousStart.Minute == schedule.StartTime.Minute;

        }

        public static long Save(ScanSchedule schedule)
        {
            ValidateForSave(schedule);
            if (schedule.Id > 0 && LocalScheduleStore.Exists(schedule.Id))
            {
                // Lưu ở trạng thái tắt trước, để Task Scheduler đăng ký xong mới bật.
                LocalScheduleStore.Save(schedule, false);
                try { SetEnabled(schedule.Id, true); }
                catch (Exception ex) { RecordOutcome(schedule.Id, "Đăng ký thất bại: " + ex.Message, false); throw; }
                return schedule.Id;
            }
            long id;
            try
            {
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand(@"IF @id=0
                INSERT dbo.ScanSchedules (Name,ScanType,TargetPath,Frequency,StartTime,IsEnabled)
                OUTPUT INSERTED.ScheduleID VALUES(@name,@type,@target,@freq,@time,0)
                ELSE BEGIN
                   UPDATE dbo.ScanSchedules SET Name=@name,ScanType=@type,TargetPath=@target,
                       Frequency=@freq,StartTime=@time,IsEnabled=0 WHERE ScheduleID=@id;
                   SELECT @id;
                END", cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = schedule.Id;
                cmd.Parameters.Add("@name", SqlDbType.NVarChar, 150).Value = schedule.Name.Trim();
                cmd.Parameters.Add("@type", SqlDbType.VarChar, 20).Value = schedule.ScanType;
                cmd.Parameters.Add("@target", SqlDbType.NVarChar, 1000).Value =
                    string.IsNullOrWhiteSpace(schedule.TargetPath) ? (object)DBNull.Value : schedule.TargetPath.Trim();
                cmd.Parameters.Add("@freq", SqlDbType.VarChar, 20).Value = schedule.Frequency;
                cmd.Parameters.Add("@time", SqlDbType.DateTime2).Value = schedule.StartTime;
                cn.Open();
                id = Convert.ToInt64(cmd.ExecuteScalar());
            }
            }
            catch (SqlException)
            {
                // Giữ nguyên SQL: fallback sang %LocalAppData% khi không có bảng lập lịch.
                // Nếu đang sửa lịch SQL nhưng SQL mất kết nối, tạo ID cục bộ riêng
                // để không đè/ẩn lịch SQL đó khi kết nối trở lại.
                if (schedule.Id > 0) schedule.Id = 0;
                LocalScheduleStore.Save(schedule, false);
                try { SetEnabled(schedule.Id, true); }
                catch (Exception ex) { RecordOutcome(schedule.Id, "Đăng ký thất bại: " + ex.Message, false); throw; }
                return schedule.Id;
            }
            schedule.Id = id;
            // Nếu đăng ký Task Scheduler lỗi, giữ lịch SQL ở trạng thái tắt để tránh nói đã hoàn tất.
            try
            {
                SetEnabled(id, true);
            }
            catch (Exception ex)
            {
                RecordOutcome(id, "Đăng ký Task Scheduler thất bại: " + ex.Message, false);
                throw;
            }
            return id;
        }
        private static string TaskName(long id) { return "AntivirusWinform_Scan_" + id; }

        // Windows Task Scheduler 2.0 COM. TASK_LOGON_INTERACTIVE_TOKEN (3) does not
        // ask for, save or require a Windows account password. The account must be logged on.
        private static dynamic OpenTaskScheduler(out object connection)
        {
            Type schedulerType = Type.GetTypeFromProgID("Schedule.Service");
            if (schedulerType == null)
                throw new InvalidOperationException("Không tìm thấy Windows Task Scheduler 2.0 trên máy.");
            connection = Activator.CreateInstance(schedulerType);
            dynamic scheduler = connection;
            scheduler.Connect();
            return scheduler;
        }

        private static void ReleaseCom(object com)
        {
            if (com != null && Marshal.IsComObject(com))
                Marshal.ReleaseComObject(com);
        }

        private static Exception SchedulerFailure(Exception ex, string action)
        {
            var ce = ex as COMException;
            if (ce == null) return ex;
            uint hr = unchecked((uint)ce.ErrorCode);
            string meaning;
            if (hr == 0x80070005)
                meaning = "Windows từ chối quyền của tài khoản đang đăng nhập. ";
            else if (hr == 0x80041315)
                meaning = "Dịch vụ Task Scheduler chưa chạy hoặc không sẵn sàng. ";
            else if (hr == 0x80041318 || hr == 0x8004131A)
                meaning = "Định dạng hoặc cấu hình lịch không hợp lệ. ";
            else meaning = "";
            return new InvalidOperationException("Không thể " + action + ". " + meaning +
                "Mã Windows: 0x" + hr.ToString("X8") + ". Chi tiết: " + ce.Message, ce);
        }

        private static bool TaskDoesNotExist(COMException ex)
        {
            uint hr = unchecked((uint)ex.ErrorCode);
            return hr == 0x80070002 || hr == 0x8004130F;
        }

        private static void RegisterWindowsTask(ScanSchedule s)
        {
            string exe = Assembly.GetEntryAssembly().Location;
            if (!File.Exists(exe) || !exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Chỉ đăng ký lịch khi chạy từ ScanAndRemoveVirus.exe.");

            object connection = null;
            object folder = null;
            object definition = null;
            object trigger = null;
            object action = null;
            object registeredTask = null;
            try
            {
                dynamic scheduler = OpenTaskScheduler(out connection);
                folder = scheduler.GetFolder("\\");
                definition = scheduler.NewTask(0);
                dynamic task = definition;
                task.RegistrationInfo.Description = "AntivirusWinform - " + s.Name;
                task.Principal.UserId = WindowsIdentity.GetCurrent().Name;
                task.Principal.LogonType = 3; // TASK_LOGON_INTERACTIVE_TOKEN
                task.Principal.RunLevel = 0; // TASK_RUNLEVEL_LUA (không đòi UAC)
                task.Settings.Enabled = true;
                task.Settings.StartWhenAvailable = true;
                task.Settings.DisallowStartIfOnBatteries = false;
                task.Settings.StopIfGoingOnBatteries = false;
                task.Settings.ExecutionTimeLimit = "PT4H";
                task.Settings.MultipleInstances = 2; // TASK_INSTANCES_IGNORE_NEW

                int triggerType = s.Frequency == "Once" ? 1 : s.Frequency == "Daily" ? 2 :
                    s.Frequency == "Weekly" ? 3 : 4;
                trigger = task.Triggers.Create(triggerType);
                dynamic t = trigger;
                t.StartBoundary = s.StartTime.ToString("yyyy-MM-ddTHH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture);
                t.Enabled = true;
                if (s.Frequency == "Daily") t.DaysInterval = 1;
                else if (s.Frequency == "Weekly")
                {
                    t.WeeksInterval = 1;
                    t.DaysOfWeek = 1 << (int)s.StartTime.DayOfWeek;
                }
                else if (s.Frequency == "Monthly")
                {
                    t.DaysOfMonth = 1 << (s.StartTime.Day - 1);
                    t.MonthsOfYear = 4095; // 12 tháng
                }

                action = task.Actions.Create(0); // TASK_ACTION_EXEC
                dynamic a = action;
                a.Path = exe;
                a.Arguments = "--schedule-id " + s.Id;
                a.WorkingDirectory = Path.GetDirectoryName(exe);

                dynamic f = folder;
                registeredTask = f.RegisterTaskDefinition(TaskName(s.Id), task, 6,
                    WindowsIdentity.GetCurrent().Name, null, 3, null);
            }
            catch (COMException ex) { throw SchedulerFailure(ex, "đăng ký lịch quét"); }
            finally
            {
                ReleaseCom(registeredTask);
                ReleaseCom(action);
                ReleaseCom(trigger);
                ReleaseCom(definition);
                ReleaseCom(folder);
                ReleaseCom(connection);
            }
        }

        private static void SetWindowsTaskEnabled(long id, bool enabled)
        {
            object connection = null, folder = null, registeredTask = null;
            try
            {
                dynamic scheduler = OpenTaskScheduler(out connection);
                folder = scheduler.GetFolder("\\");
                try { registeredTask = ((dynamic)folder).GetTask(TaskName(id)); }
                catch (COMException ex) when (!enabled && TaskDoesNotExist(ex)) { return; }
                ((dynamic)registeredTask).Enabled = enabled;
            }
            catch (COMException ex) { throw SchedulerFailure(ex, enabled ? "bật lịch quét" : "tắt lịch quét"); }
            finally { ReleaseCom(registeredTask); ReleaseCom(folder); ReleaseCom(connection); }
        }

        private static void DeleteWindowsTask(long id)
        {
            object connection = null, folder = null;
            try
            {
                dynamic scheduler = OpenTaskScheduler(out connection);
                folder = scheduler.GetFolder("\\");
                try { ((dynamic)folder).DeleteTask(TaskName(id), 0); }
                catch (COMException ex) when (TaskDoesNotExist(ex)) { /* Lịch đăng ký lỗi trước đó. */ }
            }
            catch (COMException ex) { throw SchedulerFailure(ex, "xóa lịch quét"); }
            finally { ReleaseCom(folder); ReleaseCom(connection); }
        }

        public static void RunNow(long id)
        {
            object connection = null, folder = null, registeredTask = null, running = null;
            try
            {
                dynamic scheduler = OpenTaskScheduler(out connection);
                folder = scheduler.GetFolder("\\");
                registeredTask = ((dynamic)folder).GetTask(TaskName(id));
                if (!((dynamic)registeredTask).Enabled)
                    throw new InvalidOperationException("Lịch đang tắt hoặc chưa đăng ký thành công. Hãy bật lịch trước.");
                running = ((dynamic)registeredTask).Run(null);
            }
            catch (COMException ex) { throw SchedulerFailure(ex, "chạy ngay lịch quét"); }
            finally
            {
                ReleaseCom(running); ReleaseCom(registeredTask);
                ReleaseCom(folder); ReleaseCom(connection);
            }
        }

        public static void SetEnabled(long id, bool enabled)
        {
            if (enabled)
            {
                ScanSchedule candidate = Get(id);
                if (!NextOccurrence(candidate.StartTime, candidate.Frequency, DateTime.Now).HasValue)
                    throw new InvalidOperationException("Không thể bật lịch đã hết hạn. " +
                        "Hãy cập nhật lại thời gian bắt đầu trước khi bật.");
            }
            if (LocalScheduleStore.Exists(id))
            {
                if (enabled) RegisterWindowsTask(Get(id));
                else SetWindowsTaskEnabled(id, false);
                var local = LocalScheduleStore.Get(id);
                LocalScheduleStore.Save(local, enabled);
                return;
            }
            if (enabled) RegisterWindowsTask(Get(id));
            else SetWindowsTaskEnabled(id, false);
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand("UPDATE dbo.ScanSchedules SET IsEnabled=@enabled WHERE ScheduleID=@id", cn))
            {
                cmd.Parameters.Add("@enabled", SqlDbType.Bit).Value = enabled;
                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
                cn.Open(); cmd.ExecuteNonQuery();
            }
        }
        public static void Remove(long id)
        {
            if (LocalScheduleStore.Exists(id))
            {
                DeleteWindowsTask(id);
                LocalScheduleStore.Remove(id);
                return;
            }
            DeleteWindowsTask(id);
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand("DELETE dbo.ScanSchedules WHERE ScheduleID=@id", cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
                cn.Open(); cmd.ExecuteNonQuery();
            }
        }
        private static void RecordOutcome(long id, string message, bool completed)
        {
            if (LocalScheduleStore.Exists(id))
            {
                LocalScheduleStore.SetOutcome(id, message, completed);
                return;
            }
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand(@"UPDATE dbo.ScanSchedules
                SET LastRunAt=CASE WHEN @completed=1 THEN SYSDATETIME() ELSE LastRunAt END,
                    LastOutcome=@outcome WHERE ScheduleID=@id", cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
                cmd.Parameters.Add("@completed", SqlDbType.Bit).Value = completed;
                cmd.Parameters.Add("@outcome", SqlDbType.NVarChar, 500).Value = message.Length > 500 ? message.Substring(0,500) : message;
                cn.Open(); cmd.ExecuteNonQuery();
            }
        }
        public static int RunSchedule(long id)
        {
            if (id <= 0) return 2;
            ScanSchedule schedule = Get(id);
            if (!schedule.Enabled) return 3;
            bool isNew;
            using (var mutex = new Mutex(true, "Local\\AntivirusWinform_ScanLock", out isNew))
            {
                if (!isNew)
                {
                    try { RecordOutcome(id, "Đã bỏ qua do đang có phiên quét khác", false); }
                    catch (Exception ex) { Trace.WriteLine("Ghi trạng thái lịch bị bỏ qua: " + ex.Message); }
                    ScheduledScanNoticeStore.Publish(id, schedule.Name, "failed",
                        "Đã bỏ qua lịch vì có phiên quét khác đang chạy.");
                    return 4;
                }
                try
                {
                    ScanType type = schedule.ScanType == "Quick" ? ScanType.Quick :
                                    schedule.ScanType == "Full" ? ScanType.Full : ScanType.Custom;
                    string[] selectedTargets = type == ScanType.Custom
                        ? NormalizeTargets(DecodeTargets(schedule.TargetPath)) : new string[0];
                    int availableTargets = selectedTargets.Count(p => File.Exists(p) || Directory.Exists(p));
                    if (type == ScanType.Custom && availableTargets == 0)
                    {
                        try { RecordOutcome(id, "Tất cả mục tiêu quét đã bị di chuyển hoặc không truy cập được", true); }
                        catch (Exception ex) { Trace.WriteLine("Không cập nhật được kết quả lịch: " + ex.Message); }
                        ScheduledScanNoticeStore.Publish(id, schedule.Name, "failed",
                            "Không còn tệp hoặc thư mục nào có thể quét.");
                        return 6;
                    }
                    string scanScope = type == ScanType.Custom
                        ? DescribeTargets(schedule.TargetPath) : "Lịch quét tự động";
                    // Tiến trình Task Scheduler không có form. Gửi sự kiện qua LocalAppData
                    // để giao diện chính hiển thị thông báo góc phải khi đang mở.
                    ScheduledScanNoticeStore.Publish(id, schedule.Name, "started",
                        "Đã bắt đầu " + (type == ScanType.Quick ? "quét nhanh" :
                            type == ScanType.Full ? "quét toàn bộ" : "quét tệp/thư mục") + ".");
                    var repo = new ScanHistoryRepository();
                    long scanId = 0;
                    var start = DateTime.Now;
                    try { scanId = repo.StartScan(schedule.ScanType, scanScope); }
                    catch (SqlException ex)
                    {
                        // SQL chưa sẵn sàng: vẫn quét bằng engine/snapshot ngoại tuyến,
                        // kết quả đưa vào hàng đợi lịch sử hiện có thay vì bỏ quét.
                        Trace.WriteLine("Lịch quét chạy không có SQL: " + ex.Message);
                    }
                    try
                    {
                        // Một phiên quét duy nhất cho tất cả tệp và thư mục, dùng chung
                        // snapshot chữ ký và bộ lọc trùng tệp có sẵn trong ScanEngine.
                        var result = type == ScanType.Custom && selectedTargets.Length > 1
                            ? ScanEngine.Scan(type, null, CancellationToken.None, null, null, null, selectedTargets)
                            : ScanEngine.Scan(type, type == ScanType.Custom ? selectedTargets[0] : null,
                                CancellationToken.None);
                        if (scanId > 0)
                        {
                            foreach (var threat in result.Threats)
                            {
                                string physical = string.IsNullOrEmpty(threat.PhysicalPath) ? threat.FilePath : threat.PhysicalPath;
                                string name = Path.GetFileName(physical);
                                new ThreatDetectionRepository().Add(scanId, threat.SignatureID, name,
                                    physical, threat.Kind + ": " + threat.Reason, null, null, null, null);
                            }
                            ScanIssueRepository.Save(scanId, result);
                            repo.CompleteScan(scanId, result.FilesScanned, result.Threats.Count);
                        }
                        else
                        {
                            // OfflineHistoryQueue sẽ tự lưu bản ghi chờ và đồng bộ khi SQL sẵn sàng.
                            ScanHistoryStore.AddEntry(new HistoryEntry {
                                Time = start,
                                Type = type == ScanType.Quick ? "Quét nhanh (tự động)" :
                                    type == ScanType.Full ? "Quét toàn bộ (tự động)" : "Quét tùy chọn (tự động)",
                                Scope = scanScope,
                                Files = result.FilesScanned,
                                Threats = result.Threats.Count,
                                Seconds = Math.Max(0, (DateTime.Now - start).TotalSeconds)
                            });
                        }
                        string missingNote = result.MissingTargets > 0
                            ? "; bỏ qua " + result.MissingTargets + " mục tiêu không còn tồn tại" : "";
                        try { RecordOutcome(id, "Hoàn tất: " + result.FilesScanned + " tệp, " + result.Threats.Count + " phát hiện" + missingNote, true); }
                        catch (Exception ex) { Trace.WriteLine("Không cập nhật được kết quả lịch: " + ex.Message); }
                        ScheduledScanNoticeStore.Publish(id, schedule.Name,
                            result.Threats.Count > 0 ? "threats" : "completed",
                            "Đã quét " + result.FilesScanned.ToString("N0") + " tệp · Phát hiện " +
                            result.Threats.Count.ToString("N0") + " mối đe dọa" + missingNote + ".");
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        if (scanId > 0) { try { repo.FailScan(scanId, ex.Message); } catch { } }
                        try { RecordOutcome(id, "Lỗi: " + ex.Message, true); }
                        catch (Exception outcomeEx) { Trace.WriteLine("Không cập nhật được kết quả lịch: " + outcomeEx.Message); }
                        ScheduledScanNoticeStore.Publish(id, schedule.Name, "failed",
                            "Quét không hoàn tất: " + ex.Message);
                        return 5;
                    }
                }
                finally { mutex.ReleaseMutex(); }
            }
        }
    }

    /// <summary>
    /// Chỉ là fallback cho lịch quét, không sửa các bảng SQL hiện có.
    /// Task Scheduler vẫn thực thi EXE với --schedule-id; cùng tài khoản Windows
    /// sẽ đọc cấu hình từ LocalApplicationData ngay cả khi GUI đã đóng.
    /// </summary>
    internal static class LocalScheduleStore
    {
        private static readonly object Gate = new object();
        private static string Folder { get { return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScanAndRemoveVirus", "ScheduledScans"); } }
        private static string FileName(long id) { return Path.Combine(Folder, id + ".xml"); }
        internal static bool Exists(long id) { return id > 0 && File.Exists(FileName(id)); }

        internal static DataTable NewTable()
        {
            var table = new DataTable();
            table.Columns.Add("ScheduleID", typeof(long));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("ScanType", typeof(string));
            table.Columns.Add("TargetPath", typeof(string));
            table.Columns.Add("Frequency", typeof(string));
            table.Columns.Add("StartTime", typeof(DateTime));
            table.Columns.Add("LastRunAt", typeof(DateTime));
            table.Columns.Add("LastOutcome", typeof(string));
            table.Columns.Add("IsEnabled", typeof(bool));
            return table;
        }
        private static string Field(XmlElement e, string name)
        {
            var child = e[name];
            return child == null ? "" : child.InnerText;
        }
        private static XmlDocument Read(long id)
        {
            var doc = new XmlDocument { XmlResolver = null };
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using (var reader = XmlReader.Create(FileName(id), settings)) doc.Load(reader);
            if (doc.DocumentElement == null || doc.DocumentElement.Name != "Schedule")
                throw new InvalidDataException("Tệp cấu hình lịch quét không hợp lệ.");
            return doc;
        }
        internal static ScanSchedule Get(long id)
        {
            lock (Gate)
            {
                XmlElement root = Read(id).DocumentElement;
                return new ScanSchedule {
                    Id = id, Name = Field(root, "Name"), ScanType = Field(root, "ScanType"),
                    TargetPath = Field(root, "TargetPath"), Frequency = Field(root, "Frequency"),
                    StartTime = DateTime.Parse(Field(root, "StartTime"), System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.RoundtripKind),
                    Enabled = Field(root, "Enabled") == "true"
                };
            }
        }
        private static void Put(XmlDocument d, XmlElement root, string key, string value)
        {
            XmlElement element = d.CreateElement(key); element.InnerText = value ?? "";
            root.AppendChild(element);
        }
        internal static void Save(ScanSchedule schedule, bool enabled)
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Folder);
                if (schedule.Id <= 0)
                {
                    long id = DateTime.UtcNow.Ticks;
                    while (File.Exists(FileName(id))) id++;
                    schedule.Id = id;
                }
                // Giữ lại nhật ký lần chạy nếu chỉ sửa lịch.
                DateTime? last = null; string outcome = "";
                if (File.Exists(FileName(schedule.Id)))
                {
                    var old = Read(schedule.Id).DocumentElement;
                    DateTime parsed;
                    if (DateTime.TryParse(Field(old, "LastRunAt"), out parsed)) last = parsed;
                    outcome = Field(old, "LastOutcome");
                }
                Write(schedule, enabled, last, outcome);
            }
        }
        private static void Write(ScanSchedule s, bool enabled, DateTime? last, string outcome)
        {
            var d = new XmlDocument { XmlResolver = null };
            XmlElement root = d.CreateElement("Schedule"); d.AppendChild(root);
            Put(d, root, "Name", s.Name);
            Put(d, root, "ScanType", s.ScanType);
            Put(d, root, "TargetPath", s.TargetPath);
            Put(d, root, "Frequency", s.Frequency);
            Put(d, root, "StartTime", s.StartTime.ToString("o", System.Globalization.CultureInfo.InvariantCulture));
            Put(d, root, "Enabled", enabled ? "true" : "false");
            Put(d, root, "LastRunAt", last.HasValue ? last.Value.ToString("o", System.Globalization.CultureInfo.InvariantCulture) : "");
            Put(d, root, "LastOutcome", outcome);
            string dest = FileName(s.Id), temp = dest + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                d.Save(temp);
                if (File.Exists(dest)) File.Replace(temp, dest, null);
                else File.Move(temp, dest);
            }
            finally { try { if (File.Exists(temp)) File.Delete(temp); } catch (IOException) { } }
        }
        internal static void Remove(long id)
        {
            lock (Gate) { File.Delete(FileName(id)); }
        }
        internal static void SetOutcome(long id, string message, bool completed)
        {
            lock (Gate)
            {
                var d = Read(id); var e = d.DocumentElement;
                var s = Get(id); // C# Monitor lock is reentrant.
                DateTime parsed; DateTime? previous = DateTime.TryParse(Field(e, "LastRunAt"), out parsed)
                    ? (DateTime?)parsed : null;
                Write(s, s.Enabled, completed ? DateTime.Now : previous,
                    (message ?? "").Length > 500 ? message.Substring(0, 500) : message);
            }
        }
        internal static DataTable List()
        {
            var result = NewTable();
            lock (Gate)
            {
                if (!Directory.Exists(Folder)) return result;
                foreach (string file in Directory.GetFiles(Folder, "*.xml"))
                {
                    try
                    {
                        long id;
                        if (!long.TryParse(Path.GetFileNameWithoutExtension(file), out id)) continue;
                        var d = Read(id); XmlElement root = d.DocumentElement;
                        var schedule = Get(id);
                        DateTime at;
                        object runAt = DateTime.TryParse(Field(root, "LastRunAt"), out at) ? (object)at : DBNull.Value;
                        result.Rows.Add(id, schedule.Name, schedule.ScanType, schedule.TargetPath,
                            schedule.Frequency, schedule.StartTime, runAt, Field(root, "LastOutcome"), schedule.Enabled);
                    }
                    catch (Exception ex) { Trace.WriteLine("Bỏ qua cấu hình lịch hỏng: " + ex.Message); }
                }
            }
            return result;
        }
    }
}
