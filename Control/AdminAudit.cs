using System;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ScanAndRemoveVirus.Control
{
    internal static class AdminAudit
    {
        private static readonly object Sync = new object();
        internal static string PathName
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ScanAndRemoveVirus", "admin-audit.dpapi.log"); }
        }
        internal static void Write(string operation, string description)
        {
            try
            {
                string payload = DateTimeOffset.Now.ToString("o") + "\t" +
                    Environment.UserName.Replace("\t", " ") + "\t" + operation.Replace("\t", " ") + "\t" +
                    (description ?? "").Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
                byte[] value = ProtectedData.Protect(Encoding.UTF8.GetBytes(payload), null, DataProtectionScope.CurrentUser);
                lock (Sync)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(PathName));
                    File.AppendAllText(PathName, Convert.ToBase64String(value) + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception) { /* Never prevent an operation merely because logging failed. */ }
        }
        internal static DataTable Read()
        {
            var table = new DataTable();
            table.Columns.Add("Thời gian"); table.Columns.Add("Tài khoản Windows");
            table.Columns.Add("Hành động"); table.Columns.Add("Chi tiết");
            if (!File.Exists(PathName)) return table;
            string[] rows;
            lock (Sync) rows = File.ReadAllLines(PathName, Encoding.UTF8);
            int start = Math.Max(0, rows.Length - 300);
            for (int i = rows.Length - 1; i >= start; i--)
            {
                try
                {
                    string plain = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(rows[i]), null, DataProtectionScope.CurrentUser));
                    string[] fields = plain.Split(new[] {'\t'}, 4);
                    if (fields.Length == 4) table.Rows.Add(fields[0], fields[1], fields[2], fields[3]);
                }
                catch { table.Rows.Add("?", "?", "log-unreadable", "Không giải mã được dòng " + (i + 1)); }
            }
            return table;
        }
    }
}
