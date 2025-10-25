using System;
using System.Data;
using System.Data.SqlClient;
using ScanAndRemoveVirus.Database;

namespace ScanAndRemoveVirus.Services
{
    // Mọi quyết định duyệt chữ ký và nhật ký được commit trong cùng SQL transaction.
    public static class SignatureVerificationService
    {
        public static DataTable Search(string phrase, string filter)
        {
            const string sql = @"SELECT TOP (300) SignatureID, MalwareName, MalwareFamily, Category,
                SHA256, IsActive, VerificationStatus, SourceName, CreatedAt
              FROM dbo.VirusSignatures
              WHERE (@q = '' OR MalwareName LIKE '%' + @q + '%'
                 OR MalwareFamily LIKE '%' + @q + '%' OR SHA256 LIKE @q + '%')
                AND (@f = 'All' OR VerificationStatus = @f)
              ORDER BY SignatureID DESC";
            var table = new DataTable();
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand(sql, cn))
            using (var ad = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add("@q", SqlDbType.NVarChar, 200).Value = (phrase ?? "").Trim();
                cmd.Parameters.Add("@f", SqlDbType.VarChar, 30).Value = filter ?? "All";
                ad.Fill(table);
            }
            return table;
        }

        public static void ChangeStatus(long signatureId, string action, string reason)
        {
            if (signatureId <= 0) throw new ArgumentOutOfRangeException("signatureId");
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Phải nhập lý do để lưu nhật ký.");
            string newStatus;
            bool active;
            switch (action)
            {
                case "Approve": newStatus = "Verified"; active = true; break;
                case "Reject": newStatus = "Rejected"; active = false; break;
                case "Disable": newStatus = "Disabled"; active = false; break;
                case "PendingReview": newStatus = "PendingReview"; active = false; break;
                default: throw new ArgumentException("Hành động không hợp lệ.");
            }
            using (var cn = DbConnection.Create())
            {
                cn.Open();
                using (var tr = cn.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        string oldStatus;
                        bool oldActive;
                        using (var cmd = new SqlCommand(@"SELECT IsActive, VerificationStatus
                             FROM dbo.VirusSignatures WITH (UPDLOCK, ROWLOCK)
                             WHERE SignatureID=@id", cn, tr))
                        {
                            cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = signatureId;
                            using (var rd = cmd.ExecuteReader())
                            {
                                if (!rd.Read()) throw new InvalidOperationException("Không tìm thấy chữ ký.");
                                oldActive = Convert.ToBoolean(rd[0]);
                                oldStatus = Convert.ToString(rd[1]);
                            }
                        }
                        using (var cmd = new SqlCommand(@"UPDATE dbo.VirusSignatures
                          SET IsActive=@active, VerificationStatus=@status,
                              VerifiedAt=CASE WHEN @status='Verified' THEN SYSUTCDATETIME() ELSE NULL END,
                              VerifiedBy=CASE WHEN @status='Verified' THEN @user ELSE NULL END,
                              UpdatedAt=SYSDATETIME() WHERE SignatureID=@id;
                          INSERT INTO dbo.SignatureAuditLogs
                            (SignatureID, ActionType, OldValue, NewValue, Reason, ChangedBy)
                          VALUES(@id,@action,@oldValue,@newValue,@reason,@user);", cn, tr))
                        {
                            cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = signatureId;
                            cmd.Parameters.Add("@active", SqlDbType.Bit).Value = active;
                            cmd.Parameters.Add("@status", SqlDbType.VarChar, 30).Value = newStatus;
                            cmd.Parameters.Add("@action", SqlDbType.VarChar, 32).Value = action;
                            cmd.Parameters.Add("@oldValue", SqlDbType.NVarChar, 1000).Value =
                                oldStatus + "; IsActive=" + oldActive;
                            cmd.Parameters.Add("@newValue", SqlDbType.NVarChar, 1000).Value =
                                newStatus + "; IsActive=" + active;
                            cmd.Parameters.Add("@reason", SqlDbType.NVarChar, 500).Value = reason.Trim().Substring(0, Math.Min(500, reason.Trim().Length));
                            cmd.Parameters.Add("@user", SqlDbType.NVarChar, 128).Value = Environment.UserName;
                            cmd.ExecuteNonQuery();
                        }
                        tr.Commit();
                    }
                    catch { try { tr.Rollback(); } catch { } throw; }
                }
            }
            HybridSignatureScanner.Invalidate();
        }
        public static DataTable Audit(long signatureId)
        {
            var table = new DataTable();
            using (var cn = DbConnection.Create())
            using (var cmd = new SqlCommand(@"SELECT TOP (200) ChangedAt, ActionType, OldValue,
             NewValue, ChangedBy, Reason FROM dbo.SignatureAuditLogs
             WHERE SignatureID=@id ORDER BY AuditID DESC", cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = signatureId;
                da.Fill(table);
            }
            return table;
        }
    }
}
