using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanAndRemoveVirus.Database
{
    public class ScanHistoryRepository
    {
        //Bat Dau Quet
        public long StartScan(string scanType, string scanLocation)
        {
            const string sql = @"
                INSERT INTO dbo.ScanHistory
                (
                    ScanType,
                    StartedAt,
                    ScanLocation,
                    FilesScanned,
                    ThreatCount,
                    ResultStatus
                )
                OUTPUT INSERTED.ScanID
                VALUES
                (
                    @ScanType,
                    SYSDATETIME(),
                    @ScanLocation,
                    0,
                    0,
                    'Running'
                );";
            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(
                    "@ScanType",
                    SqlDbType.VarChar,
                    20
                ).Value = scanType;
                cmd.Parameters.Add(
                    "@ScanLocation",
                    SqlDbType.NVarChar,
                    1000
                ).Value =
                    string.IsNullOrWhiteSpace(scanLocation)
                        ? (object)DBNull.Value
                        : scanLocation;
                conn.Open();
                return Convert.ToInt64(
                    cmd.ExecuteScalar()
                );
            }
        }

        //Hoan Thanh Quet
        public void CompleteScan(long scanId, long filesScanned, int threatCount)
        {
            const string sql = @"
                UPDATE dbo.ScanHistory
                SET CompletedAt = SYSDATETIME(),
                    FilesScanned = @FilesScanned,
                    ThreatCount = @ThreatCount,
                    ResultStatus = 'Completed'
                WHERE ScanID = @ScanID;";
            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(
                    "@ScanID",
                    SqlDbType.BigInt
                ).Value = scanId;
                cmd.Parameters.Add(
                    "@FilesScanned",
                    SqlDbType.BigInt
                ).Value = filesScanned;
                cmd.Parameters.Add(
                    "@ThreatCount",
                    SqlDbType.Int
                ).Value = threatCount;
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        //Quet That Bai
        public void FailScan(long scanId, string error)
        {
            const string sql = @"
                UPDATE dbo.ScanHistory
                SET CompletedAt = SYSDATETIME(),
                    ResultStatus = 'Failed',
                    ErrorMessage = @Error
                WHERE ScanID = @ScanID;";
            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(
                    "@ScanID",
                    SqlDbType.BigInt
                ).Value = scanId;
                cmd.Parameters.Add(
                    "@Error",
                    SqlDbType.NVarChar,
                    1000
                ).Value =
                    string.IsNullOrWhiteSpace(error) ? (object)DBNull.Value : error;
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        //Lay Lich Su Quet
        public DataTable GetAll()
        {
            const string sql = @"
                SELECT
                    ScanID,
                    ScanType,
                    StartedAt,
                    CompletedAt,
                    ScanLocation,
                    FilesScanned,
                    ThreatCount,
                    ResultStatus,
                    ActivityTitle,
                    ActivityDetails,
                    ErrorMessage
                FROM dbo.ScanHistory
                ORDER BY StartedAt DESC;";
            using (SqlConnection conn = DbConnection.Create())
            using (SqlDataAdapter adapter = new SqlDataAdapter(sql, conn))
            {
                DataTable table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }
    }
}
