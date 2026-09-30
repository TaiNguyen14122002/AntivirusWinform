using System;
using System.Data;
using System.Data.SqlClient;

namespace ScanAndRemoveVirus.Database
{
    public class ThreatDetectionRepository
    {
        // =========================================================
        // 1. THÊM MỘT MỐI ĐE DỌA MỚI
        // =========================================================

        public long Add(
            long? scanId,
            long? signatureId,
            string fileName,
            string originalPath,
            string threatName,
            long? fileSize,
            string md5,
            string sha1,
            string sha256)
        {
            const string sql = @"
INSERT INTO dbo.ThreatDetections
(
    ScanID,
    SignatureID,
    FileName,
    OriginalPath,
    ThreatName,
    DetectedAt,
    FileSizeBytes,
    FileMD5,
    FileSHA1,
    FileSHA256,
    ActionTaken,
    Status
)
OUTPUT INSERTED.DetectionID
VALUES
(
    @ScanID,
    @SignatureID,
    @FileName,
    @OriginalPath,
    @ThreatName,
    SYSDATETIME(),
    @FileSize,
    @MD5,
    @SHA1,
    @SHA256,
    'None',
    'Detected'
);";

            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(
                    "@ScanID",
                    SqlDbType.BigInt
                ).Value = scanId.HasValue
                    ? (object)scanId.Value
                    : DBNull.Value;

                cmd.Parameters.Add(
                    "@SignatureID",
                    SqlDbType.BigInt
                ).Value = signatureId.HasValue
                    ? (object)signatureId.Value
                    : DBNull.Value;

                cmd.Parameters.Add(
                    "@FileName",
                    SqlDbType.NVarChar,
                    260
                ).Value = string.IsNullOrEmpty(fileName)
                    ? string.Empty
                    : fileName;

                cmd.Parameters.Add(
                    "@OriginalPath",
                    SqlDbType.NVarChar,
                    1000
                ).Value = string.IsNullOrEmpty(originalPath)
                    ? string.Empty
                    : originalPath;

                cmd.Parameters.Add(
                    "@ThreatName",
                    SqlDbType.NVarChar,
                    200
                ).Value = string.IsNullOrEmpty(threatName)
                    ? "Unknown"
                    : threatName;

                cmd.Parameters.Add(
                    "@FileSize",
                    SqlDbType.BigInt
                ).Value = fileSize.HasValue
                    ? (object)fileSize.Value
                    : DBNull.Value;

                AddNullable(
                    cmd,
                    "@MD5",
                    SqlDbType.Char,
                    32,
                    md5);

                AddNullable(
                    cmd,
                    "@SHA1",
                    SqlDbType.Char,
                    40,
                    sha1);

                AddNullable(
                    cmd,
                    "@SHA256",
                    SqlDbType.Char,
                    64,
                    sha256);

                conn.Open();

                object result = cmd.ExecuteScalar();

                return Convert.ToInt64(result);
            }
        }

        // =========================================================
        // 2. LẤY DANH SÁCH PHÁT HIỆN THEO PHIÊN QUÉT
        // =========================================================

        public DataTable GetByScan(long scanId)
        {
            const string sql = @"
SELECT
    DetectionID,
    ScanID,
    SignatureID,
    FileName,
    OriginalPath,
    ThreatName,
    DetectedAt,
    FileSizeBytes,
    FileMD5,
    FileSHA1,
    FileSHA256,
    ActionTaken,
    Status,
    QuarantinePath,
    RestoredAt,
    DeletedAt
FROM dbo.ThreatDetections
WHERE ScanID = @ScanID
ORDER BY DetectedAt DESC;";

            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(
                    "@ScanID",
                    SqlDbType.BigInt
                ).Value = scanId;

                using (SqlDataAdapter adapter =
                    new SqlDataAdapter(cmd))
                {
                    DataTable table =
                        new DataTable();

                    adapter.Fill(table);

                    return table;
                }
            }
        }

        // =========================================================
        // 3. LẤY TOÀN BỘ FILE ĐANG CÁCH LY
        // =========================================================

        public DataTable GetQuarantined()
        {
            const string sql = @"
SELECT
    DetectionID,
    ScanID,
    SignatureID,
    FileName,
    OriginalPath,
    ThreatName,
    DetectedAt,
    FileSizeBytes,
    FileMD5,
    FileSHA1,
    FileSHA256,
    ActionTaken,
    Status,
    QuarantinePath,
    RestoredAt,
    DeletedAt
FROM dbo.ThreatDetections
WHERE Status = 'Quarantined'
ORDER BY DetectedAt DESC;";

            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            using (SqlDataAdapter adapter =
                new SqlDataAdapter(cmd))
            {
                DataTable table =
                    new DataTable();

                adapter.Fill(table);

                return table;
            }
        }

        // =========================================================
        // 4. LẤY MỘT PHÁT HIỆN THEO DetectionID
        // =========================================================

        public DataRow GetById(long detectionId)
        {
            const string sql = @"
SELECT TOP 1
    DetectionID,
    ScanID,
    SignatureID,
    FileName,
    OriginalPath,
    ThreatName,
    DetectedAt,
    FileSizeBytes,
    FileMD5,
    FileSHA1,
    FileSHA256,
    ActionTaken,
    Status,
    QuarantinePath,
    RestoredAt,
    DeletedAt
FROM dbo.ThreatDetections
WHERE DetectionID = @DetectionID;";

            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(
                    "@DetectionID",
                    SqlDbType.BigInt
                ).Value = detectionId;

                using (SqlDataAdapter adapter =
                    new SqlDataAdapter(cmd))
                {
                    DataTable table =
                        new DataTable();

                    adapter.Fill(table);

                    if (table.Rows.Count == 0)
                        return null;

                    return table.Rows[0];
                }
            }
        }

        // =========================================================
        // 5. ĐÁNH DẤU FILE ĐÃ ĐƯỢC CÁCH LY
        // =========================================================

        public bool MarkQuarantined(
            long detectionId,
            string quarantinePath)
        {
            const string sql = @"
UPDATE dbo.ThreatDetections
SET
    ActionTaken = 'Quarantined',
    Status = 'Quarantined',
    QuarantinePath = @QuarantinePath,
    RestoredAt = NULL,
    DeletedAt = NULL
WHERE DetectionID = @DetectionID;";

            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(
                    "@DetectionID",
                    SqlDbType.BigInt
                ).Value = detectionId;

                cmd.Parameters.Add(
                    "@QuarantinePath",
                    SqlDbType.NVarChar,
                    1000
                ).Value =
                    string.IsNullOrWhiteSpace(quarantinePath)
                        ? (object)DBNull.Value
                        : quarantinePath;

                conn.Open();

                int affected =
                    cmd.ExecuteNonQuery();

                return affected > 0;
            }
        }

        // =========================================================
        // 6. ĐÁNH DẤU FILE ĐÃ KHÔI PHỤC
        // =========================================================

        public bool MarkRestored(long detectionId)
        {
            const string sql = @"
UPDATE dbo.ThreatDetections
SET
    ActionTaken = 'Restored',
    Status = 'Restored',
    RestoredAt = SYSDATETIME()
WHERE
    DetectionID = @DetectionID
    AND Status = 'Quarantined';";

            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(
                    "@DetectionID",
                    SqlDbType.BigInt
                ).Value = detectionId;

                conn.Open();

                int affected =
                    cmd.ExecuteNonQuery();

                return affected > 0;
            }
        }

        // =========================================================
        // 7. ĐÁNH DẤU FILE ĐÃ XÓA VĨNH VIỄN
        // =========================================================

        public bool MarkDeleted(long detectionId)
        {
            const string sql = @"
UPDATE dbo.ThreatDetections
SET
    ActionTaken = 'Deleted',
    Status = 'Deleted',
    DeletedAt = SYSDATETIME()
WHERE
    DetectionID = @DetectionID
    AND Status = 'Quarantined';";

            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(
                    "@DetectionID",
                    SqlDbType.BigInt
                ).Value = detectionId;

                conn.Open();

                int affected =
                    cmd.ExecuteNonQuery();

                return affected > 0;
            }
        }

        // =========================================================
        // 8. KIỂM TRA DETECTION CÓ ĐANG CÁCH LY KHÔNG
        // =========================================================

        public bool IsQuarantined(long detectionId)
        {
            const string sql = @"
SELECT COUNT(1)
FROM dbo.ThreatDetections
WHERE
    DetectionID = @DetectionID
    AND Status = 'Quarantined';";

            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(
                    "@DetectionID",
                    SqlDbType.BigInt
                ).Value = detectionId;

                conn.Open();

                int count =
                    Convert.ToInt32(
                        cmd.ExecuteScalar());

                return count > 0;
            }
        }

        // =========================================================
        // 9. ĐẾM SỐ FILE ĐANG CÁCH LY
        // =========================================================

        public int CountQuarantined()
        {
            const string sql = @"
SELECT COUNT(*)
FROM dbo.ThreatDetections
WHERE Status = 'Quarantined';";

            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();

                return Convert.ToInt32(
                    cmd.ExecuteScalar());
            }
        }

        // =========================================================
        // 10. HÀM HỖ TRỢ THAM SỐ NULL
        // =========================================================

        private static void AddNullable(
            SqlCommand cmd,
            string name,
            SqlDbType type,
            int size,
            string value)
        {
            cmd.Parameters
                .Add(name, type, size)
                .Value =
                    string.IsNullOrWhiteSpace(value)
                        ? (object)DBNull.Value
                        : value.Trim()
                            .ToUpperInvariant();
        }
    }
}