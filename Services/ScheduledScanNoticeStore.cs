using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ScanAndRemoveVirus.Services
{
    // Thông tin trạng thái nhẹ giữa tiến trình quét --schedule-id và cửa sổ WinForms.
    // Không truy vấn SQL, không cần sửa hay tạo thêm bảng.
    internal sealed class ScheduledScanNotice
    {
        public DateTime UtcTime;
        public long ScheduleId;
        public string ScheduleName;
        public string Kind;
        public string Message;
    }

    internal static class ScheduledScanNoticeStore
    {
        private static string Folder
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                    "ScanAndRemoveVirus", "ScheduleNotifications");
            }
        }

        // File tạm + đổi tên bảo đảm form chỉ đọc sự kiện đã ghi xong.
        // Không cho lỗi thông báo làm hỏng việc quét virus.
        internal static void Publish(long id, string name, string kind, string message)
        {
            string temporary = null;
            try
            {
                Directory.CreateDirectory(Folder);
                string basename = DateTime.UtcNow.Ticks.ToString("D19") + "_" + Guid.NewGuid().ToString("N");
                temporary = Path.Combine(Folder, basename + ".tmp");
                string dest = Path.Combine(Folder, basename + ".xml");
                var notice = new XElement("ScheduleNotice",
                    new XElement("UtcTime", DateTime.UtcNow.ToString("o")),
                    new XElement("ScheduleId", id),
                    new XElement("ScheduleName", name ?? "Quét tự động"),
                    new XElement("Kind", kind ?? "completed"),
                    new XElement("Message", message ?? ""));
                File.WriteAllText(temporary, notice.ToString(SaveOptions.DisableFormatting), Encoding.UTF8);
                File.Move(temporary, dest);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Không ghi được thông báo lịch quét: " + ex.Message);
                if (temporary != null) { try { File.Delete(temporary); } catch { } }
            }
        }

        // Chỉ gọi trên UI thread qua WinForms timer. Tiêu thụ một lần, giới hạn
        // số file được xử lý mỗi lượt và bỏ các thông báo quá cũ.
        internal static List<ScheduledScanNotice> Drain(DateTime sinceUtc)
        {
            var notices = new List<ScheduledScanNotice>();
            try
            {
                if (!Directory.Exists(Folder)) return notices;
                var files = Directory.GetFiles(Folder, "*.xml")
                    .OrderBy(path => path, StringComparer.Ordinal).Take(100).ToArray();
                foreach (string file in files)
                {
                    try
                    {
                        XElement root = XElement.Load(file, LoadOptions.None);
                        if (root.Name != "ScheduleNotice") { File.Delete(file); continue; }
                        DateTime at;
                        if (!DateTime.TryParse((string)root.Element("UtcTime"), null,
                            System.Globalization.DateTimeStyles.RoundtripKind, out at))
                        { File.Delete(file); continue; }
                        at = at.ToUniversalTime();
                        if (at >= sinceUtc && at >= DateTime.UtcNow.AddMinutes(-3))
                        {
                            long id;
                            long.TryParse((string)root.Element("ScheduleId"), out id);
                            notices.Add(new ScheduledScanNotice {
                                UtcTime = at,
                                ScheduleId = id,
                                ScheduleName = (string)root.Element("ScheduleName") ?? "Quét tự động",
                                Kind = (string)root.Element("Kind") ?? "completed",
                                Message = (string)root.Element("Message") ?? ""
                            });
                        }
                        File.Delete(file);
                    }
                    catch (IOException) { /* còn tiến trình khác đang dùng: thử lại */ }
                    catch (UnauthorizedAccessException) { }
                    catch (System.Xml.XmlException) { try { File.Delete(file); } catch { } }
                }
            }
            catch (Exception ex) { Trace.WriteLine("Đọc thông báo lịch quét: " + ex.Message); }
            return notices;
        }
    }
}
