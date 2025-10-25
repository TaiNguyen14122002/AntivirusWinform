using System;
using System.Diagnostics;
using System.Threading;
using ScanAndRemoveVirus.Services;

class BenchQuick
{
    static void Main(string[] args)
    {
        var sw = Stopwatch.StartNew();
        int last = 0;
        var r = ScanEngine.Scan(ScanType.Quick, null, CancellationToken.None,
            (n, p) => { if (last == 0 || n - last >= 50000) { last = n; Console.WriteLine("  {0,8:N0} tep  ({1:F1}s)  {2}", n, sw.Elapsed.TotalSeconds, p ?? ""); } });
        sw.Stop();
        Console.WriteLine("Ket qua: {0:N0} tep, {1} de doa, {2:F1}s | {3}",
            r.FilesScanned, r.Threats.Count, r.Duration.TotalSeconds,
            args.Length > 0 ? args[0] : "");
        foreach (var t in r.Threats) Console.WriteLine("  DE DOA: " + t.FilePath);
    }
}
