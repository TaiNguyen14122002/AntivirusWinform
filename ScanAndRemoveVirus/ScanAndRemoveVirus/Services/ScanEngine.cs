using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ScanAndRemoveVirus.Services
{
    public enum ScanType { Quick, Full, Custom }

    public class ThreatFound
    {
        public string FilePath { get; set; }
        public string Reason { get; set; }
    }

    public class ScanResult
    {
        public int FilesScanned { get; set; }
        public TimeSpan Duration { get; set; }
        private readonly List<ThreatFound> threats = new List<ThreatFound>();
        public List<ThreatFound> Threats { get { return threats; } }
        public bool HasThreats { get { return threats.Count > 0; } }
    }

    /// <summary>
    /// Engine quét + cách ly, tách khỏi UI để chạy nền (Task.Run) và kiểm thử được.
    /// </summary>
    public static class ScanEngine
    {
        // ponytail: chỉ nhận diện chữ ký thử nghiệm tự đặt + tên tệp chứa "eicar"
        // (không dùng đúng chuỗi EICAR chuẩn vì Windows Defender trên máy dev sẽ xoá tệp mẫu).
        // Nâng cấp: thay bằng bảng chữ ký thật khi có engine quét chính thức.
        public const string TestSignature = "XVIRUS-TEST-SIGNATURE::";
        private const long MaxContentScanBytes = 4 * 1024 * 1024;
        private const int ProgressEveryNFiles = 25;
        // Quét là tác vụ I/O-bound (mở/đọc từng tệp) nên dùng nhiều worker hơn số CPU
        private const int MaxParallelWorkers = 64;
        // Gom tệp vào hàng đợi theo lô: giảm tranh chấp queue so với đẩy từng tệp
        private const int FileChunkSize = 512;
        private static readonly byte[] SignatureBytes = Encoding.ASCII.GetBytes(TestSignature);
        // Buffer so khớp tái sử dụng theo thread: tránh cấp phát mảng + chuỗi cho từng tệp
        private static readonly ThreadLocal<byte[]> SignatureBuffer =
            new ThreadLocal<byte[]>(() => new byte[SignatureBytes.Length]);

        public static string QuarantineDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ScanAndRemoveVirus", "Quarantine");
            }
        }

        public static int CountQuarantined()
        {
            return Directory.Exists(QuarantineDir) ? Directory.GetFiles(QuarantineDir).Length : 0;
        }

        public static bool Quarantine(string filePath)
        {
            try
            {
                Directory.CreateDirectory(QuarantineDir);
                // Đổi tên bằng GUID để không đè tệp cách ly trùng tên
                string dest = Path.Combine(QuarantineDir, Guid.NewGuid().ToString("N") + ".qtn");
                File.Move(filePath, dest);
                return true;
            }
            catch (Exception)
            {
                return false; // tệp đang khoá / đã biến mất: bỏ qua
            }
        }

        public static IEnumerable<string> GetScanRoots(ScanType type, string customPath)
        {
            if (type == ScanType.Custom)
                return string.IsNullOrEmpty(customPath) || !Directory.Exists(customPath)
                    ? Enumerable.Empty<string>()
                    : Enumerable.Repeat(customPath, 1);

            if (type == ScanType.Full)
                return DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                    .Select(d => d.Name);

            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new[]
            {
                Path.Combine(profile, "Desktop"),
                Path.Combine(profile, "Downloads"),
                Path.GetTempPath()
            }.Where(Directory.Exists);
        }

        /// <param name="progress">callback (số tệp đã quét, tệp hiện tại) — có thể được gọi từ nhiều thread nền</param>
        /// <exception cref="OperationCanceledException">bị hủy giữa chừng</exception>
        public static ScanResult Scan(ScanType type, string customPath, CancellationToken ct,
            Action<int, string> progress = null)
        {
            ct.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();
            var result = new ScanResult();
            string[] roots = GetScanRoots(type, customPath).Where(Directory.Exists).ToArray();

            if (roots.Length > 0)
            {
                // Không Dispose CountdownEvent/BlockingCollection: worker vẫn có thể chạm
                // vào chúng sau khi Wait trả về; để GC tự thu dọn.
                // Mỗi mục việc (thư mục hoặc tệp) giữ +1 trong completion (thêm trước khi
                // Add, Signal sau khi xử lý xong) nên Wait chỉ trả về khi hết việc hoàn toàn.
                var completion = new CountdownEvent(1);
                var work = new BlockingCollection<WorkItem>();
                var state = new ScanState
                {
                    Threats = new ConcurrentQueue<ThreatFound>(),
                    Work = work,
                    Completion = completion,
                    Progress = progress,
                    Cancellation = ct
                };

                int workerCount = Math.Max(8, Math.Min(MaxParallelWorkers, Environment.ProcessorCount * 4));
                // ThreadPool mặc định tăng thread rất từ từ -> nâng mức tối thiểu
                // để đủ worker cho tác vụ I/O-bound chạy ngay
                int minWorker, minIocp;
                ThreadPool.GetMinThreads(out minWorker, out minIocp);
                ThreadPool.SetMinThreads(Math.Max(minWorker, workerCount), Math.Max(minIocp, workerCount));

                for (int i = 0; i < workerCount; i++)
                    ThreadPool.QueueUserWorkItem(delegate { RunWorker(state); });

                foreach (string root in roots)
                {
                    completion.AddCount();
                    work.Add(new WorkItem { Path = root, IsDirectory = true });
                }

                completion.Signal();
                try
                {
                    completion.Wait(ct);
                }
                finally
                {
                    work.CompleteAdding();
                }

                foreach (ThreatFound threat in state.Threats)
                    result.Threats.Add(threat);
                result.FilesScanned = state.Scanned;
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            if (progress != null) progress(result.FilesScanned, null);
            ct.ThrowIfCancellationRequested();
            return result;
        }

        private static void RunWorker(ScanState state)
        {
            try
            {
                foreach (WorkItem item in state.Work.GetConsumingEnumerable(state.Cancellation))
                {
                    try
                    {
                        if (item.IsDirectory)
                            ScanDirectory(item.Path, state);
                        else
                            ScanFileChunk(item.Files, state);
                    }
                    catch (OperationCanceledException) { }
                    finally
                    {
                        try { state.Completion.Signal(); }
                        catch (ObjectDisposedException) { }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private static void ScanDirectory(string folder, ScanState state)
        {
            try
            {
                // Đẩy thư mục con và TỪNG LÔ TỆP vào hàng đợi chung để mọi worker
                // cùng giành việc — kể cả thư mục phẳng chứa hàng trăm nghìn tệp
                foreach (string sub in Directory.EnumerateDirectories(folder))
                {
                    state.Completion.AddCount();
                    state.Work.Add(new WorkItem { Path = sub, IsDirectory = true });
                }

                string[] chunk = null;
                int chunkFill = 0;
                foreach (string file in Directory.EnumerateFiles(folder))
                {
                    state.Cancellation.ThrowIfCancellationRequested();
                    if (chunk == null) chunk = new string[FileChunkSize];
                    chunk[chunkFill++] = file;
                    if (chunkFill == FileChunkSize)
                    {
                        state.Completion.AddCount();
                        state.Work.Add(new WorkItem { Files = chunk });
                        chunk = null;
                        chunkFill = 0;
                    }
                }
                if (chunkFill > 0)
                {
                    if (chunkFill < FileChunkSize) Array.Resize(ref chunk, chunkFill);
                    state.Completion.AddCount();
                    state.Work.Add(new WorkItem { Files = chunk });
                }
            }
            catch (OperationCanceledException) { throw; } // cho hủy lan lên, đừng nuốt
            catch (UnauthorizedAccessException) { } // thư mục hệ thống không có quyền
            catch (IOException) { }
        }

        private static void ScanFileChunk(string[] files, ScanState state)
        {
            for (int i = 0; i < files.Length; i++)
                InspectOneFile(files[i], state);
        }

        private static void InspectOneFile(string file, ScanState state)
        {
            state.Cancellation.ThrowIfCancellationRequested();
            if (InspectFile(file))
                state.Threats.Enqueue(new ThreatFound
                {
                    FilePath = file,
                    Reason = "Chứa chữ ký thử nghiệm"
                });
            int scanned = Interlocked.Increment(ref state.Scanned);
            if (state.Progress != null && scanned % ProgressEveryNFiles == 0)
                state.Progress(scanned, file);
        }

        private static bool InspectFile(string path)
        {
            try
            {
                if (path.IndexOf("eicar", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                // Mở thẳng FileStream thay vì FileInfo + OpenRead: bớt 1 syscall stat mỗi tệp
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.SequentialScan))
                {
                    if (stream.Length == 0 || stream.Length > MaxContentScanBytes) return false;

                    byte[] buffer = SignatureBuffer.Value;
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read < buffer.Length) return false;
                    for (int i = 0; i < SignatureBytes.Length; i++)
                        if (buffer[i] != SignatureBytes[i]) return false;
                    return true;
                }
            }
            catch (Exception)
            {
                return false; // ponytail: tệp bị khoá/không đọc được thì bỏ qua, không dừng phiên quét
            }
        }

        private sealed class WorkItem
        {
            public string Path;
            public string[] Files;
            public bool IsDirectory;
        }

        private sealed class ScanState
        {
            public int Scanned;
            public ConcurrentQueue<ThreatFound> Threats;
            public BlockingCollection<WorkItem> Work;
            public CountdownEvent Completion;
            public Action<int, string> Progress;
            public CancellationToken Cancellation;
        }
    }
}
