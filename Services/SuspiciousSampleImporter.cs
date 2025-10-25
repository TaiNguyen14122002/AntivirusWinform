using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using ScanAndRemoveVirus.Database;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>
    /// Local import of user-submitted samples. Never executes, extracts or transmits binaries.
    /// Only hashes/metadata are stored; activation is controlled by the user choice in the submission form.
    /// The form enables new signatures by default; uncheck it to keep unverified samples inactive.
    /// </summary>
    internal static class SuspiciousSampleImporter
    {
        private const int MaxSelectedFiles = 500;
        private const int MaxEntriesPerZip = 2000;
        private const long MaxEntryBytes = 512L * 1024 * 1024;
        private const long MaxTotalZipBytes = 2L * 1024 * 1024 * 1024;
        private const long MaxSampleBytes = 8L * 1024 * 1024 * 1024;
        private const long MaxMetadataEntryBytes = 8L * 1024 * 1024 * 1024;
        private const long MaxMetadataZipBytes = 12L * 1024 * 1024 * 1024;
        private const int BlockSize = 256 * 1024;

        internal sealed class Report
        {
            internal int Processed, Inserted, Duplicate, Failed, Skipped, Activated;
            internal readonly List<string> Messages = new List<string>();
            internal void Log(string line)
            {
                if (Messages.Count < 250) Messages.Add(line);
            }
        }
        internal sealed class Progress
        {
            internal string Current;
            internal int SelectedIndex, SelectedTotal, Processed, Inserted, Duplicate, Failed;
        }
        private sealed class Digest
        {
            internal string Md5, Sha1, Sha256;
            internal long Length;
        }

        internal static Report Import(IList<string> inputs, bool verifiedByAdministrator,
            IProgress<Progress> progress, CancellationToken cancel)
        {
            if (inputs == null || inputs.Count == 0) throw new ArgumentException("Chưa chọn tệp.");
            if (inputs.Count > MaxSelectedFiles) throw new ArgumentException("Mỗi lần chỉ chọn tối đa 500 tệp.");
            var report = new Report();
            try
            {
                // Open SQL first: failure must be explicit, not displayed as 'no malware found'.
                using (var connection = DbConnection.Create())
                {
                    connection.Open();
                    for (int i = 0; i < inputs.Count; i++)
                    {
                        cancel.ThrowIfCancellationRequested();
                        string file = inputs[i];
                        Notify(progress, report, Path.GetFileName(file), i + 1, inputs.Count);
                        try
                        {
                            if (!File.Exists(file)) throw new FileNotFoundException("Tệp đã bị di chuyển hoặc bị chặn.");
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Length > MaxSampleBytes) throw new InvalidDataException("Tệp vượt giới hạn an toàn 8 GiB mỗi mẫu.");
                            if (string.Equals(Path.GetExtension(file), ".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                ImportZip(connection, file, verifiedByAdministrator, report, progress,
                                    i + 1, inputs.Count, cancel);
                            }
                            else if (SubmittedSignatureListImporter.IsMetadata(Path.GetFileName(file)))
                            {
                                using (var original = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, BlockSize, FileOptions.SequentialScan))
                                using (var source = OpenMetadata(original, file))
                                using (var bounded = new BoundedInput(source, MaxSampleBytes))
                                    SubmittedSignatureListImporter.Import(bounded, Path.GetFileName(file), connection,
                                        verifiedByAdministrator, report,
                                        name => Notify(progress, report, name, i + 1, inputs.Count), cancel);
                            }
                            else
                            {
                                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, BlockSize, FileOptions.SequentialScan))
                                {
                                    Digest hash = Calculate(stream, MaxSampleBytes, cancel);
                                    Store(connection, hash, Path.GetFileName(file), null, verifiedByAdministrator, report, cancel);
                                }
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                             ex is InvalidDataException || ex is CryptographicException || ex is NotSupportedException ||
                             ex is ArgumentException)
                        {
                            report.Failed++;
                            report.Log("LỖI: " + Path.GetFileName(file) + " — " + ex.Message);
                        }
                    }
                }
                return report;
            }
            finally
            {
                // The user may cancel after some active signatures have been saved.
                // Always invalidate the scanner cache, even on cancellation/error.
                if (report.Activated > 0)
                {
                    HybridSignatureScanner.Invalidate();
                    ScanEngine.ClearScanCache();
                }
            }
        }

        private static void ImportZip(SqlConnection connection, string zipPath, bool activate,
            Report report, IProgress<Progress> progress, int selectedIndex, int selectedTotal, CancellationToken cancel)
        {
            using (var file = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, BlockSize, FileOptions.SequentialScan))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Read, false))
            {
                // A CSV.ZIP is a signature list, not a malicious ZIP sample. Never
                // register the archive's own hash when it contains metadata lists.
                bool containsLists = zip.Entries.Any(e => !string.IsNullOrEmpty(e.Name) &&
                    SubmittedSignatureListImporter.IsMetadata(e.Name));
                if (!containsLists && Path.GetFileName(zipPath).EndsWith(".csv.zip", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Tệp .csv.zip không chứa entry CSV. Không nhập nhầm hash của tệp nén.");
                if (!containsLists)
                {
                    using (var archiveHashFile = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, BlockSize, FileOptions.SequentialScan))
                    {
                        Digest archiveHash = Calculate(archiveHashFile, MaxSampleBytes, cancel);
                        Store(connection, archiveHash, Path.GetFileName(zipPath), null, activate, report, cancel);
                    }
                }
                int count = 0;
                long cumulative = 0;
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    cancel.ThrowIfCancellationRequested();
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    count++;
                    if (count > MaxEntriesPerZip)
                    {
                        report.Skipped++;
                        report.Log("Dừng đọc ZIP: vượt " + MaxEntriesPerZip + " mục bên trong.");
                        break;
                    }
                    string leaf = Path.GetFileName(entry.FullName.Replace('\\', '/'));
                    if (string.IsNullOrWhiteSpace(leaf)) continue;
                    bool list = SubmittedSignatureListImporter.IsMetadata(leaf);
                    // A metadata ZIP often also contains README/LICENSE files.
                    // Never import unrelated attachments as active malware signatures.
                    if (containsLists && !list)
                    {
                        report.Skipped++;
                        continue;
                    }
                    long entryLimit = list ? MaxMetadataEntryBytes : MaxEntryBytes;
                    long totalLimit = containsLists ? MaxMetadataZipBytes : MaxTotalZipBytes;
                    if (entry.Length > entryLimit || entry.Length > totalLimit - cumulative)
                    {
                        report.Skipped++;
                        report.Log("BỎ QUA ZIP: " + leaf + " (vượt giới hạn dữ liệu đọc).");
                        continue;
                    }
                    if (count == 1 || count % 10 == 0)
                        Notify(progress, report, Path.GetFileName(zipPath) + " → " + leaf, selectedIndex, selectedTotal);
                    try
                    {
                        if (list)
                        {
                            using (var part = entry.Open())
                            using (var source = OpenMetadata(part, leaf))
                            using (var bounded = new BoundedInput(source, entryLimit))
                            {
                                SubmittedSignatureListImporter.Import(bounded, leaf, connection, activate, report,
                                    name => Notify(progress, report, Path.GetFileName(zipPath) + " → " + name,
                                        selectedIndex, selectedTotal), cancel);
                                cumulative = checked(cumulative + bounded.Count);
                            }
                        }
                        else
                        {
                            using (var source = entry.Open())
                            {
                                Digest hash = Calculate(source, entryLimit, cancel);
                                cumulative = checked(cumulative + hash.Length);
                                Store(connection, hash, leaf, Path.GetFileName(zipPath), activate, report, cancel);
                            }
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) when (ex is IOException || ex is InvalidDataException ||
                        ex is NotSupportedException || ex is CryptographicException || ex is ArgumentException)
                    {
                        report.Failed++;
                        report.Log("LỖI ZIP ENTRY: " + leaf + " — " + ex.Message);
                    }
                }
            }
        }

        private static Stream OpenMetadata(Stream stream, string name)
        {
            if (name.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                return new GZipStream(stream, CompressionMode.Decompress, true);
            // This wrapper owns no underlying stream, which is managed by the caller.
            return new BoundedInput(stream, long.MaxValue);
        }
        private sealed class BoundedInput : Stream
        {
            private readonly Stream inner;
            private readonly long maximum;
            internal long Count { get; private set; }
            internal BoundedInput(Stream inner, long maximum) { this.inner = inner; this.maximum = maximum; }
            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position { get { return Count; } set { throw new NotSupportedException(); } }
            public override int Read(byte[] buffer, int offset, int count)
            {
                int amount = inner.Read(buffer, offset, count);
                Count = checked(Count + amount);
                if (Count > maximum) throw new InvalidDataException("Danh sách giải nén vượt giới hạn dung lượng.");
                return amount;
            }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
            protected override void Dispose(bool disposing) { base.Dispose(disposing); }
        }

        private static Digest Calculate(Stream source, long limit, CancellationToken cancel)
        {
            byte[] buffer = new byte[BlockSize];
            using (var md5 = MD5.Create())
            using (var sha1 = SHA1.Create())
            using (var sha256 = SHA256.Create())
            {
                long bytes = 0;
                int read;
                while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancel.ThrowIfCancellationRequested();
                    bytes = checked(bytes + read);
                    if (bytes > limit) throw new InvalidDataException("Luồng giải nén vượt giới hạn an toàn.");
                    md5.TransformBlock(buffer, 0, read, buffer, 0);
                    sha1.TransformBlock(buffer, 0, read, buffer, 0);
                    sha256.TransformBlock(buffer, 0, read, buffer, 0);
                }
                md5.TransformFinalBlock(buffer, 0, 0);
                sha1.TransformFinalBlock(buffer, 0, 0);
                sha256.TransformFinalBlock(buffer, 0, 0);
                return new Digest
                {
                    Length = bytes,
                    Md5 = BitConverter.ToString(md5.Hash).Replace("-", ""),
                    Sha1 = BitConverter.ToString(sha1.Hash).Replace("-", ""),
                    Sha256 = BitConverter.ToString(sha256.Hash).Replace("-", "")
                };
            }
        }

        private static void Store(SqlConnection connection, Digest digest, string leafName, string archiveName,
            bool confirmed, Report report, CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();
            // One short SERIALIZABLE transaction per sample to prevent duplicate concurrent imports.
            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    const string existingSql = @"SELECT TOP (1) SignatureID
                        FROM dbo.VirusSignatures WITH (UPDLOCK, HOLDLOCK)
                        WHERE SHA256 COLLATE Latin1_General_CI_AS = @SHA256
                           OR (SHA256 IS NULL AND
                               (MD5 COLLATE Latin1_General_CI_AS = @MD5 OR
                                SHA1 COLLATE Latin1_General_CI_AS = @SHA1));";
                    using (var check = new SqlCommand(existingSql, connection, transaction))
                    {
                        check.CommandTimeout = 60;
                        check.Parameters.Add("@SHA256", SqlDbType.Char, 64).Value = digest.Sha256;
                        check.Parameters.Add("@SHA1", SqlDbType.Char, 40).Value = digest.Sha1;
                        check.Parameters.Add("@MD5", SqlDbType.Char, 32).Value = digest.Md5;
                        object id = check.ExecuteScalar();
                        if (id != null && id != DBNull.Value)
                        {
                            transaction.Commit();
                            report.Processed++; report.Duplicate++;
                            report.Log("ĐÃ CÓ (#" + id + "): " + leafName);
                            return;
                        }
                    }
                    string name = CleanName(Path.GetFileNameWithoutExtension(leafName));
                    string extension = Path.GetExtension(leafName);
                    if (extension.Length > 50) extension = extension.Substring(0, 50);
                    string description = "Mẫu người dùng gửi để phân tích; nguồn: " +
                        (archiveName == null ? "tệp độc lập" : "ZIP " + CleanName(archiveName)) +
                        "; kích thước: " + digest.Length.ToString() + " byte. " +
                        (confirmed ? "Được quản trị viên xác minh trước khi kích hoạt." : "CHƯA XÁC MINH, chỉ lưu hash; không tự kết luận là mã độc.");
                    if (description.Length > 1000) description = description.Substring(0, 1000);
                    const string insertSql = @"INSERT INTO dbo.VirusSignatures
                       (MalwareName, MalwareFamily, Category, SignatureType, MD5, SHA1, SHA256,
                        FileExtension, Severity, Description, RecommendedAction, IsActive)
                        OUTPUT INSERTED.SignatureID
                        VALUES (@Name, NULL, @Category, 'Hash', @MD5, @SHA1, @SHA256,
                                @Extension, 'Medium', @Description, @Action, @Active);";
                    using (var insert = new SqlCommand(insertSql, connection, transaction))
                    {
                        insert.CommandTimeout = 60;
                        insert.Parameters.Add("@Name", SqlDbType.NVarChar, 200).Value = (confirmed ? "UserVerified." : "UserSuspected.") + name;
                        // Chưa đủ bằng chứng về loại mã độc: Category = NULL;
                        // việc xác minh/duyệt được lưu qua IsActive, Description và
                        // VerificationStatus (nếu bảng hiện tại có cột này).
                        insert.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = DBNull.Value;
                        insert.Parameters.Add("@MD5", SqlDbType.Char, 32).Value = digest.Md5;
                        insert.Parameters.Add("@SHA1", SqlDbType.Char, 40).Value = digest.Sha1;
                        insert.Parameters.Add("@SHA256", SqlDbType.Char, 64).Value = digest.Sha256;
                        insert.Parameters.Add("@Extension", SqlDbType.NVarChar, 50).Value = (object)extension ?? DBNull.Value;
                        insert.Parameters.Add("@Description", SqlDbType.NVarChar, 1000).Value = description;
                        insert.Parameters.Add("@Action", SqlDbType.VarChar, 30).Value = confirmed ? "Quarantine" : "Review";
                        insert.Parameters.Add("@Active", SqlDbType.Bit).Value = confirmed;
                        long newId = Convert.ToInt64(insert.ExecuteScalar());
                        using (var metadata = new SqlCommand(@"
IF COL_LENGTH('dbo.VirusSignatures','VerificationStatus') IS NOT NULL
    UPDATE dbo.VirusSignatures SET VerificationStatus = @Status
    WHERE SignatureID = @ID;
IF COL_LENGTH('dbo.VirusSignatures','SourceName') IS NOT NULL
    EXEC sys.sp_executesql
        N'UPDATE dbo.VirusSignatures SET SourceName = N''UserSubmission'' WHERE SignatureID = @EntryId',
        N'@EntryId bigint', @EntryId = @ID;
IF OBJECT_ID('dbo.SignatureAuditLogs', 'U') IS NOT NULL
    INSERT INTO dbo.SignatureAuditLogs
        (SignatureID, ActionType, OldValue, NewValue, Reason, ChangedBy)
    VALUES (@ID, 'Imported', NULL, @Status,
        N'Nhập mẫu bằng AntivirusWinform; kiểm tra độc lập vẫn cần thiết.', SUSER_SNAME());",
                            connection, transaction))
                        {
                            metadata.Parameters.Add("@ID", SqlDbType.BigInt).Value = newId;
                            metadata.Parameters.Add("@Status", SqlDbType.VarChar, 30).Value =
                                confirmed ? "Verified" : "PendingReview";
                            metadata.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                    report.Processed++; report.Inserted++;
                    if (confirmed) report.Activated++;
                    report.Log((confirmed ? "ĐÃ KÍCH HOẠT: " : "CHỜ XÁC MINH: ") + leafName);
                }
                catch { try { transaction.Rollback(); } catch (InvalidOperationException) { } throw; }
            }
        }

        private static string CleanName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unnamed";
            var s = new StringBuilder();
            foreach (char c in name)
            {
                if (char.IsControl(c) || c == '<' || c == '>' || c == '"' || c == '\'') continue;
                s.Append(c);
            }
            string clean = s.ToString().Trim();
            return clean.Length > 175 ? clean.Substring(0, 175) : (clean.Length == 0 ? "Unnamed" : clean);
        }
        private static void Notify(IProgress<Progress> progress, Report report,
            string file, int index, int total)
        {
            if (progress != null) progress.Report(new Progress
            {
                Current = file, SelectedIndex = index, SelectedTotal = total,
                Processed = report.Processed, Inserted = report.Inserted,
                Duplicate = report.Duplicate, Failed = report.Failed
            });
        }
    }
}
