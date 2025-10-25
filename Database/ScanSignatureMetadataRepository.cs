using System;
using System.Data;
using System.Data.SqlClient;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Database
{
    /// <summary>F03: lưu nguồn chữ ký cho phiên quét khi kết nối SQL khả dụng.</summary>
    internal static class ScanSignatureMetadataRepository
    {
        internal static void Save(long scanId, ScanResult scan)
        {
            if (scanId <= 0 || scan == null) return;
            try
            {
                using (SqlConnection conn = DbConnection.Create())
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(@"
IF OBJECT_ID(N'dbo.ScanSignatureMetadata', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.ScanSignatureMetadata WHERE ScanID=@id)
        INSERT dbo.ScanSignatureMetadata (ScanID, SignatureSource, SnapshotCreatedAtUtc, IsDegraded)
        VALUES (@id, @source, @created, @degraded);
END", conn))
                    {
                        cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = scanId;
                        cmd.Parameters.Add("@source", SqlDbType.VarChar, 30).Value =
                            (object)(scan.SignatureSource ?? "Unavailable");
                        cmd.Parameters.Add("@created", SqlDbType.DateTime2).Value =
                            scan.SignatureCreatedAtUtc == DateTime.MinValue
                                ? (object)DBNull.Value : scan.SignatureCreatedAtUtc;
                        cmd.Parameters.Add("@degraded", SqlDbType.Bit).Value = scan.SignatureIsDegraded;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("ScanSignatureMetadata save: " + ex.GetType().Name);
            }
        }
    }
}
