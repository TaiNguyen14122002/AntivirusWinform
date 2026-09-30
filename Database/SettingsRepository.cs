using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanAndRemoveVirus.Database
{
    public class SettingsRepository
    {
        public DataRow Get()
        {
            const string sql = @"
                SELECT TOP 1 *
                FROM dbo.Settings
                ORDER BY SettingID;";
            using (SqlConnection conn = DbConnection.Create())
            using (SqlDataAdapter adapter = new SqlDataAdapter(sql, conn))
            {
                DataTable table = new DataTable();
                adapter.Fill(table);
                if (table.Rows.Count == 0)
                    return null;
                return table.Rows[0];
            }
        }
        public void UpdateProtection(bool realTime, bool file, bool usb, bool web, bool download, bool ransomware)
        {
            const string sql = @"
                UPDATE dbo.Settings
                SET
                    RealTimeProtection = @RealTime,
                    FileProtection = @File,
                    USBProtection = @USB,
                    WebProtection = @Web,
                    DownloadProtection = @Download,
                    RansomwareProtection = @Ransomware,
                    UpdatedAt = SYSDATETIME()
                WHERE SettingID =
                (
                    SELECT MIN(SettingID)
                    FROM dbo.Settings
                );";
            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@RealTime", SqlDbType.Bit).Value = realTime;
                cmd.Parameters.Add("@File", SqlDbType.Bit).Value = file;
                cmd.Parameters.Add("@USB", SqlDbType.Bit).Value = usb;
                cmd.Parameters.Add("@Web", SqlDbType.Bit).Value = web;
                cmd.Parameters.Add("@Download", SqlDbType.Bit).Value = download;
                cmd.Parameters.Add("@Ransomware", SqlDbType.Bit).Value = ransomware;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public void UpdateGeneral(bool autoStart, bool autoUpdate, bool submitSamples, bool showNotifications)
        {
            const string sql = @"
                UPDATE dbo.Settings
                SET
                    AutoStart = @AutoStart,
                    AutoUpdate = @AutoUpdate,
                    SubmitSamples = @SubmitSamples,
                    ShowNotifications = @ShowNotifications,
                    UpdatedAt = SYSDATETIME()
                WHERE SettingID =
                (
                    SELECT MIN(SettingID)
                    FROM dbo.Settings
                );";

            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@AutoStart", SqlDbType.Bit)
                    .Value = autoStart;

                cmd.Parameters.Add("@AutoUpdate", SqlDbType.Bit)
                    .Value = autoUpdate;

                cmd.Parameters.Add("@SubmitSamples", SqlDbType.Bit)
                    .Value = submitSamples;

                cmd.Parameters.Add("@ShowNotifications", SqlDbType.Bit)
                    .Value = showNotifications;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}


