using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanAndRemoveVirus.Database
{
    public class VirusSignatureRepository
    {
        //tim theo Sha256
        public VirusSignature FindBySHA256(string sha256)
        {
            if (string.IsNullOrWhiteSpace(sha256))
                return null;
            const string sql = @"
                SELECT TOP 1
                    SignatureID,
                    MalwareName,
                    MalwareFamily,
                    Category,
                    SignatureType,
                    MD5,
                    SHA1,
                    SHA256,
                    FileExtension,
                    Severity,
                    Description,
                    RecommendedAction
                FROM dbo.VirusSignatures
                WHERE SHA256 = @SHA256
                  AND IsActive = 1;";
            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(
                    "@SHA256",
                    SqlDbType.Char,
                    64
                ).Value = sha256.Trim().ToUpperInvariant();
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;
                    return ReadSignature(reader);
                }
            }
        }

        //Tim theo SHA1
        public VirusSignature FindBySHA1(string sha1)
        {
            if (string.IsNullOrWhiteSpace(sha1))
                return null;
            const string sql = @"
                SELECT TOP 1
                    SignatureID,
                    MalwareName,
                    MalwareFamily,
                    Category,
                    SignatureType,
                    MD5,
                    SHA1,
                    SHA256,
                    FileExtension,
                    Severity,
                    Description,
                    RecommendedAction
                FROM dbo.VirusSignatures
                WHERE SHA1 = @SHA1 AND IsActive = 1;";
            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(
                    "@SHA1",
                    SqlDbType.Char,
                    40
                ).Value = sha1.Trim().ToUpperInvariant();
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;
                    return ReadSignature(reader);
                }
            }
        }

        //Tim theo MD5
        public VirusSignature FindByMD5(string md5)
        {
            if (string.IsNullOrWhiteSpace(md5))
                return null;
            const string sql = @"
                SELECT TOP 1
                    SignatureID,
                    MalwareName,
                    MalwareFamily,
                    Category,
                    SignatureType,
                    MD5,
                    SHA1,
                    SHA256,
                    FileExtension,
                    Severity,
                    Description,
                    RecommendedAction
                FROM dbo.VirusSignatures
                WHERE MD5 = @MD5 AND IsActive = 1;";
            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(
                    "@MD5",
                    SqlDbType.Char,
                    32
                ).Value = md5.Trim().ToUpperInvariant();
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;
                    return ReadSignature(reader);
                }
            }
        }
        public long CountActive()
        {
            const string sql = @"
                SELECT COUNT_BIG(*)
                FROM dbo.VirusSignatures
                WHERE IsActive = 1;";
            using (SqlConnection conn = DbConnection.Create())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                return Convert.ToInt64(
                    cmd.ExecuteScalar());
            }
        }
        private VirusSignature ReadSignature(SqlDataReader reader)
        {
            return new VirusSignature
            {
                SignatureID = Convert.ToInt64(reader["SignatureID"]),
                MalwareName = reader["MalwareName"].ToString(),
                MalwareFamily = reader["MalwareFamily"] == DBNull.Value ? null : reader["MalwareFamily"].ToString(),
                Category = reader["Category"] == DBNull.Value ? null : reader["Category"].ToString(),
                SignatureType = reader["SignatureType"].ToString(),
                MD5 = reader["MD5"] == DBNull.Value ? null : reader["MD5"].ToString(),
                SHA1 = reader["SHA1"] == DBNull.Value ? null : reader["SHA1"].ToString(),
                SHA256 = reader["SHA256"] == DBNull.Value ? null : reader["SHA256"].ToString(),
                FileExtension = reader["FileExtension"] == DBNull.Value ? null : reader["FileExtension"].ToString(),
                Severity = reader["Severity"].ToString(),
                Description = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString(),
                RecommendedAction = reader["RecommendedAction"].ToString()
            };
        }

    }

}
