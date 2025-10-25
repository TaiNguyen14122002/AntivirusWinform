using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.FileIO;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Imports HASH METADATA lists, not the hash of the list file itself.
    /// CSV/TSV and JSONL are streamed; SQL is written in bounded batches.
    /// Data is untrusted and must never be interpreted as executable instructions.
    /// </summary>
    internal static class SubmittedSignatureListImporter
    {
        private const int BatchSize = 1000;
        private const int JsonMaxBytes = 16 * 1024 * 1024;
        private static readonly Regex Hex32 = new Regex("\\A[0-9a-fA-F]{32}\\z", RegexOptions.Compiled);
        private static readonly Regex Hex40 = new Regex("\\A[0-9a-fA-F]{40}\\z", RegexOptions.Compiled);
        private static readonly Regex Hex64 = new Regex("\\A[0-9a-fA-F]{64}\\z", RegexOptions.Compiled);

        private sealed class Row
        {
            internal string Sha256, Sha1, Md5, Name, Family, Extension, Description;
        }

        internal static bool IsMetadata(string name)
        {
            string n = (name ?? "").ToLowerInvariant();
            if (n.EndsWith(".gz")) n = n.Substring(0, n.Length - 3);
            return n.EndsWith(".csv") || n.EndsWith(".tsv") || n.EndsWith(".jsonl") ||
                   n.EndsWith(".ndjson") || n.EndsWith(".json") || n.EndsWith(".hashes.txt") ||
                   n.EndsWith(".hashlist") || n.EndsWith(".sha256") || n.EndsWith(".sha1") || n.EndsWith(".md5");
        }

        internal static void Import(Stream content, string fileName, SqlConnection connection, bool active,
            SuspiciousSampleImporter.Report report, Action<string> reportProgress, CancellationToken cancellation)
        {
            if (!IsMetadata(fileName)) throw new InvalidDataException("Đây không phải định dạng danh sách chữ ký được hỗ trợ.");
            var batch = MakeBatch();
            var inBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CreateStage(connection);
            string lower = fileName.ToLowerInvariant();
            if (lower.EndsWith(".gz")) lower = lower.Substring(0, lower.Length - 3);
            int parsed = 0;
            int valid = 0;
            Action<Row> accept = item =>
            {
                cancellation.ThrowIfCancellationRequested();
                parsed++;
                if (item == null || (item.Sha256 == null && item.Sha1 == null && item.Md5 == null))
                {
                    report.Skipped++;
                    return;
                }
                valid++;
                string key = item.Sha256 != null ? "256:" + item.Sha256 :
                    item.Sha1 != null ? "1:" + item.Sha1 : "MD5:" + item.Md5;
                string[] allKeys = new[] {
                    item.Sha256 == null ? null : "256:" + item.Sha256,
                    item.Sha1 == null ? null : "1:" + item.Sha1,
                    item.Md5 == null ? null : "MD5:" + item.Md5
                };
                if (allKeys.Any(k => k != null && inBatch.Contains(k)))
                {
                    report.Processed++;
                    report.Duplicate++;
                    return;
                }
                foreach (string hashKey in allKeys)
                    if (hashKey != null) inBatch.Add(hashKey);
                string name = Limit(Clean(item.Name), 200);
                if (string.IsNullOrWhiteSpace(name)) name = "ImportedHash." + key.Substring(key.IndexOf(':') + 1, Math.Min(16, key.Length - key.IndexOf(':') - 1));
                string desc = "Danh sách hash do người dùng nhập: " + Limit(fileName, 125) +
                    "; dữ liệu chưa được kiểm chứng độc lập.";
                if (!string.IsNullOrWhiteSpace(item.Description)) desc += " " + Limit(Clean(item.Description), 700);
                batch.Rows.Add(Db(item.Sha256), Db(item.Sha1), Db(item.Md5), name,
                    Db(Limit(Clean(item.Family), 100)), Db(SafeExtension(item.Extension)),
                    Limit(desc, 1000), active);
                if (batch.Rows.Count >= BatchSize)
                {
                    CommitBatch(connection, batch, report, cancellation);
                    batch.Clear(); inBatch.Clear();
                    if (reportProgress != null) reportProgress(fileName + " — " + parsed.ToString("N0") + " dòng");
                }
            };

            if (lower.EndsWith(".csv") || lower.EndsWith(".tsv")) ReadDelimited(content, lower.EndsWith(".tsv") ? "\t" : ",", accept, cancellation);
            else if (lower.EndsWith(".jsonl") || lower.EndsWith(".ndjson")) ReadJsonLines(content, accept, cancellation);
            else if (lower.EndsWith(".json")) ReadJson(content, accept, cancellation);
            else ReadHashes(content, accept, cancellation);

            if (batch.Rows.Count > 0) CommitBatch(connection, batch, report, cancellation);
            if (reportProgress != null) reportProgress(fileName + " — đọc " + parsed.ToString("N0") + " dòng");
            if (valid == 0) throw new InvalidDataException("Danh sách không có MD5, SHA-1 hoặc SHA-256 hợp lệ. Không lưu hash của tệp danh sách.");
        }

        private static void ReadDelimited(Stream stream, string delimiter, Action<Row> accept, CancellationToken token)
        {
            using (var parser = new TextFieldParser(stream, Encoding.UTF8, true))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(delimiter);
                parser.HasFieldsEnclosedInQuotes = true;
                parser.TrimWhiteSpace = true;
                Dictionary<string, int> header = null;
                bool checkedHeader = false;
                while (!parser.EndOfData)
                {
                    token.ThrowIfCancellationRequested();
                    string[] fields;
                    try { fields = parser.ReadFields(); }
                    catch (MalformedLineException) { accept(null); continue; }
                    if (fields == null || fields.Length == 0) continue;
                    if (!checkedHeader)
                    {
                        if (fields.Length == 1 && string.IsNullOrWhiteSpace(fields[0])) continue;
                        // MalwareBazaar exports may include descriptive comment lines and a '# "sha256_hash"' header.
                        header = BuildHeader(fields);
                        if (header != null) { checkedHeader = true; continue; }
                        if (fields[0].TrimStart().StartsWith("#")) continue;
                        checkedHeader = true;
                        // A single hash per line is supported even without a CSV header.
                        if (fields.Length == 1 && IsHash(fields[0])) { accept(NormalizeHashOnly(fields[0])); continue; }
                        throw new InvalidDataException("CSV/TSV thiếu tiêu đề hash (SHA256/SHA1/MD5 hoặc sha256_hash/...); không nhập nhầm tệp danh sách thành mẫu mã độc.");
                    }
                    if (fields[0].TrimStart().StartsWith("#") && fields.Length == 1) continue;
                    if (header == null) accept(fields.Length == 1 ? NormalizeHashOnly(fields[0]) : null);
                    else accept(Normalize(fields, header));
                }
            }
        }

        private static Dictionary<string, int> BuildHeader(string[] fields)
        {
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < fields.Length; i++)
            {
                string key = Key(fields[i]);
                if (!columns.ContainsKey(key)) columns.Add(key, i);
            }
            return Has(columns, "sha256", "sha256hash", "sha1", "sha1hash", "md5", "md5hash") ? columns : null;
        }
        private static bool Has(Dictionary<string, int> columns, params string[] names)
        { return names.Any(columns.ContainsKey); }
        private static string Key(string text)
        { return new string((text ?? "").Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray()); }
        private static string Get(string[] fields, Dictionary<string, int> columns, params string[] names)
        {
            foreach (string name in names)
            {
                int index;
                if (columns.TryGetValue(name, out index) && index >= 0 && index < fields.Length)
                {
                    string v = fields[index] == null ? null : fields[index].Trim();
                    if (!string.IsNullOrWhiteSpace(v) && !string.Equals(v, "n/a", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(v, "null", StringComparison.OrdinalIgnoreCase)) return v;
                }
            }
            return null;
        }
        private static Row Normalize(string[] values, Dictionary<string, int> columns)
        {
            string sha256 = NormalizeHash(Get(values, columns, "sha256", "sha256hash"), 64);
            string sha1 = NormalizeHash(Get(values, columns, "sha1", "sha1hash"), 40);
            string md5 = NormalizeHash(Get(values, columns, "md5", "md5hash"), 32);
            if (sha256 == null && sha1 == null && md5 == null) return null;
            string file = Get(values, columns, "filename", "name", "originalfilename");
            string family = Get(values, columns, "signature", "malwarefamily", "family");
            string malwareName = Get(values, columns, "malwarename", "threatname", "signature", "malwarefamily");
            return new Row
            {
                Sha256 = sha256, Sha1 = sha1, Md5 = md5,
                Name = malwareName ?? (file != null ? "Imported." + Path.GetFileName(file.Replace('\\', '/')) : null),
                Family = family,
                Extension = Get(values, columns, "fileextension", "extension", "filetypeguess", "filetype", "filename"),
                Description = Get(values, columns, "description", "firstseenutc", "firstseen")
            };
        }
        private static Row NormalizeHashOnly(string value)
        {
            return new Row
            {
                Sha256 = NormalizeHash(value, 64), Sha1 = NormalizeHash(value, 40), Md5 = NormalizeHash(value, 32)
            };
        }
        private static bool IsHash(string value)
        { return NormalizeHash(value, 64) != null || NormalizeHash(value, 40) != null || NormalizeHash(value, 32) != null; }
        private static string NormalizeHash(string hash, int length)
        {
            if (hash == null) return null;
            hash = hash.Trim();
            return length == 64 && Hex64.IsMatch(hash) || length == 40 && Hex40.IsMatch(hash) ||
                   length == 32 && Hex32.IsMatch(hash) ? hash.ToUpperInvariant() : null;
        }

        private static void ReadJsonLines(Stream input, Action<Row> accept, CancellationToken token)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = JsonMaxBytes, RecursionLimit = 24 };
            using (var reader = new StreamReader(input, Encoding.UTF8, true, 64 * 1024))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    token.ThrowIfCancellationRequested();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.Length > JsonMaxBytes) { accept(null); continue; }
                    try { ConsumeJson(serializer.DeserializeObject(line), accept); }
                    catch (ArgumentException) { accept(null); }
                    catch (InvalidOperationException) { accept(null); }
                }
            }
        }
        private static void ReadJson(Stream input, Action<Row> accept, CancellationToken token)
        {
            byte[] data = new byte[JsonMaxBytes + 1];
            int count = 0, got;
            while ((got = input.Read(data, count, data.Length - count)) > 0)
            {
                token.ThrowIfCancellationRequested();
                count += got;
                if (count >= data.Length) throw new InvalidDataException("JSON dạng mảng vượt 16 MiB. Hãy xuất JSONL hoặc CSV để nhập theo luồng.");
            }
            var serializer = new JavaScriptSerializer { MaxJsonLength = JsonMaxBytes, RecursionLimit = 24 };
            ConsumeJson(serializer.DeserializeObject(Encoding.UTF8.GetString(data, 0, count).TrimStart('\ufeff')), accept);
        }
        private static void ConsumeJson(object obj, Action<Row> accept)
        {
            var array = obj as object[];
            if (array != null) { foreach (object item in array) ConsumeJson(item, accept); return; }
            var map = obj as Dictionary<string, object>;
            if (map == null) { accept(null); return; }
            object nested;
            if (map.TryGetValue("data", out nested) && nested is object[]) { ConsumeJson(nested, accept); return; }
            if (map.TryGetValue("samples", out nested) && nested is object[]) { ConsumeJson(nested, accept); return; }
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var values = new List<string>();
            foreach (var pair in map)
            {
                string key = Key(pair.Key);
                if (columns.ContainsKey(key)) continue;
                columns[key] = values.Count;
                values.Add(pair.Value == null ? null : Convert.ToString(pair.Value));
            }
            accept(Normalize(values.ToArray(), columns));
        }
        private static void ReadHashes(Stream input, Action<Row> accept, CancellationToken token)
        {
            using (var reader = new StreamReader(input, Encoding.UTF8, true, 64 * 1024))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    token.ThrowIfCancellationRequested();
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                    // Common sha256sum format: HASH [*]filename
                    string[] fields = line.Trim().Split(new []{' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                    accept(fields.Length == 0 ? null : NormalizeHashOnly(fields[0]));
                }
            }
        }
        private static string SafeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return null;
            string value = extension.Replace('\\', '/').Trim();
            if (value.Contains("/")) value = value.Substring(value.LastIndexOf('/') + 1);
            string ext = value.StartsWith(".") && value.IndexOf('.', 1) < 0 ? value : Path.GetExtension(value);
            if (string.IsNullOrWhiteSpace(ext) && value.Length <= 12) ext = "." + value;
            return Regex.IsMatch(ext ?? "", "^\\.[a-zA-Z0-9]{1,20}$") ? ext.ToLowerInvariant() : null;
        }
        private static string Clean(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return new string(text.Where(c => !char.IsControl(c) && c != '<' && c != '>' && c != '\'').ToArray()).Trim();
        }
        private static string Limit(string text, int maximum)
        { return string.IsNullOrEmpty(text) ? null : text.Length > maximum ? text.Substring(0, maximum) : text; }
        private static object Db(string v) { return v == null ? (object)DBNull.Value : v; }
        private static DataTable MakeBatch()
        {
            var dt = new DataTable();
            dt.Columns.Add("SHA256", typeof(string)); dt.Columns.Add("SHA1", typeof(string));
            dt.Columns.Add("MD5", typeof(string)); dt.Columns.Add("MalwareName", typeof(string));
            dt.Columns.Add("MalwareFamily", typeof(string)); dt.Columns.Add("FileExtension", typeof(string));
            dt.Columns.Add("Description", typeof(string)); dt.Columns.Add("IsActive", typeof(bool));
            return dt;
        }
        private static void CreateStage(SqlConnection conn)
        {
            using (var cmd = new SqlCommand(@"
IF OBJECT_ID('tempdb..#UserHashStage') IS NULL
CREATE TABLE #UserHashStage (
  SHA256 CHAR(64) COLLATE DATABASE_DEFAULT NULL, SHA1 CHAR(40) COLLATE DATABASE_DEFAULT NULL,
  MD5 CHAR(32) COLLATE DATABASE_DEFAULT NULL,
  MalwareName NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
  MalwareFamily NVARCHAR(100) COLLATE DATABASE_DEFAULT NULL,
  FileExtension NVARCHAR(50) COLLATE DATABASE_DEFAULT NULL,
  Description NVARCHAR(1000) COLLATE DATABASE_DEFAULT NULL,
  IsActive BIT NOT NULL);", conn)) cmd.ExecuteNonQuery();
        }
        private static void CommitBatch(SqlConnection conn, DataTable batch,
            SuspiciousSampleImporter.Report report, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            using (var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    using (var clean = new SqlCommand("TRUNCATE TABLE #UserHashStage", conn, tx)) clean.ExecuteNonQuery();
                    using (var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tx))
                    {
                        bulk.DestinationTableName = "#UserHashStage";
                        bulk.BatchSize = batch.Rows.Count;
                        bulk.BulkCopyTimeout = 240;
                        foreach (DataColumn column in batch.Columns)
                            bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                        bulk.WriteToServer(batch);
                    }
                    token.ThrowIfCancellationRequested();
                    using (var insert = new SqlCommand(@"
DECLARE @NewUserIds TABLE (SignatureID BIGINT PRIMARY KEY);
INSERT INTO dbo.VirusSignatures
(MalwareName, MalwareFamily, Category, SignatureType, MD5, SHA1, SHA256,
 FileExtension, Severity, Description, RecommendedAction, IsActive)
OUTPUT INSERTED.SignatureID INTO @NewUserIds
SELECT s.MalwareName, s.MalwareFamily,
       NULL, -- Category là loại mã độc, không phải trạng thái duyệt mẫu
       'Hash', s.MD5, s.SHA1, s.SHA256, s.FileExtension, 'Medium',
       s.Description, CASE WHEN s.IsActive=1 THEN 'Quarantine' ELSE 'Review' END,
       s.IsActive
FROM #UserHashStage s
WHERE NOT EXISTS (
 SELECT 1 FROM dbo.VirusSignatures v WITH (UPDLOCK, HOLDLOCK)
 WHERE (s.SHA256 IS NOT NULL AND (v.SHA256 = s.SHA256 OR v.SHA256 = LOWER(s.SHA256)))
    OR (s.SHA1 IS NOT NULL AND (v.SHA1 = s.SHA1 OR v.SHA1 = LOWER(s.SHA1)))
    OR (s.MD5 IS NOT NULL AND (v.MD5 = s.MD5 OR v.MD5 = LOWER(s.MD5)))
);
IF COL_LENGTH('dbo.VirusSignatures', 'VerificationStatus') IS NOT NULL
BEGIN
    UPDATE v SET VerificationStatus = CASE WHEN v.IsActive=1 THEN 'UserApproved' ELSE 'PendingReview' END
      FROM dbo.VirusSignatures v JOIN @NewUserIds s ON v.SignatureID = s.SignatureID;
END;
IF COL_LENGTH('dbo.VirusSignatures', 'SourceName') IS NOT NULL
BEGIN
    UPDATE v SET SourceName = 'UserSubmission'
      FROM dbo.VirusSignatures v JOIN @NewUserIds s ON v.SignatureID = s.SignatureID;
END;
IF OBJECT_ID('dbo.SignatureAuditLogs', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.SignatureAuditLogs (SignatureID, ActionType, OldValue, NewValue, Reason, ChangedBy)
    SELECT SignatureID, 'Imported', NULL, 'UserSubmission',
       N'Nhập danh sách hash theo lô; chưa có xác minh độc lập.', SUSER_SNAME() FROM @NewUserIds;
END;
SELECT COUNT(*) FROM @NewUserIds;", conn, tx))
                    {
                        insert.CommandTimeout = 240;
                        int inserted = Convert.ToInt32(insert.ExecuteScalar());
                        report.Processed += batch.Rows.Count;
                        report.Inserted += inserted;
                        report.Duplicate += batch.Rows.Count - inserted;
                        if (inserted > 0 && Convert.ToBoolean(batch.Rows[0]["IsActive"])) report.Activated += inserted;
                    }
                    tx.Commit();
                }
                catch
                {
                    try { tx.Rollback(); } catch (InvalidOperationException) { }
                    throw;
                }
            }
        }
    }
}
