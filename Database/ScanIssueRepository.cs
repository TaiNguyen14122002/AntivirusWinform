using System;
using System.Data;
using System.Data.SqlClient;
using ScanAndRemoveVirus.Services;

namespace ScanAndRemoveVirus.Database
{
    /// <summary>F06: lưu nguyên nhân các mục chưa được kiểm tra; không ảnh hưởng kết quả quét khi SQL lỗi.</summary>
    internal static class ScanIssueRepository
    {
        internal static void Save(long scanId, ScanResult result)
        {
            if (result == null || result.Issues.Count == 0) return;
            foreach (ScanFileIssue item in result.Issues)
                LocalScanIssueStore.Record(item.Path, "Scan", item.Reason);
            if (scanId <= 0) return;
            try
            {
                using (SqlConnection connection = DbConnection.Create())
                {
                    connection.Open();
                    using (var exists = new SqlCommand("SELECT OBJECT_ID(N'dbo.ScanFileIssues', N'U')", connection))
                    {
                        object found = exists.ExecuteScalar();
                        if (found == null || found == DBNull.Value) return;
                    }
                    using (var tx = connection.BeginTransaction())
                    {
                        using (var insert = new SqlCommand(@"
INSERT INTO dbo.ScanFileIssues
(ScanID, PhysicalPath, ArchiveEntryPath, IssueType, ErrorSummary)
VALUES (@ScanID, @Path, @Entry, @Kind, @Reason);", connection, tx))
                        {
                            insert.Parameters.Add("@ScanID", SqlDbType.BigInt).Value = scanId;
                            var path = insert.Parameters.Add("@Path", SqlDbType.NVarChar, 1000);
                            var entry = insert.Parameters.Add("@Entry", SqlDbType.NVarChar, 1000);
                            var kind = insert.Parameters.Add("@Kind", SqlDbType.VarChar, 40);
                            var reason = insert.Parameters.Add("@Reason", SqlDbType.NVarChar, 500);
                            foreach (ScanFileIssue item in result.Issues)
                            {
                                string virtualPath = item.Path ?? string.Empty;
                                int boundary = virtualPath.IndexOf(" > ", StringComparison.Ordinal);
                                string physicalPath = boundary < 0 ? virtualPath : virtualPath.Substring(0, boundary);
                                string inner = boundary < 0 ? null : virtualPath.Substring(boundary + 3);
                                path.Value = Cut(physicalPath, 1000);
                                entry.Value = inner == null ? (object)DBNull.Value : Cut(inner, 1000);
                                kind.Value = boundary >= 0 ? "ArchiveEntry" : "FileOrDirectory";
                                reason.Value = Cut(item.Reason, 500);
                                insert.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Không lưu được ScanFileIssues: " + ex.GetType().Name);
            }
        }
        private static string Cut(string text, int max)
        {
            text = text ?? string.Empty;
            return text.Length <= max ? text : text.Substring(0, max);
        }
    }
}
