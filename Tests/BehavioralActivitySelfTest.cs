using System;
using System.IO;
using ScanAndRemoveVirus.Services;

internal static class BehavioralActivitySelfTest
{
    private static int failures;
    private static void Check(string name, bool success)
    {
        Console.WriteLine((success ? "PASS " : "FAIL ") + name);
        if (!success) failures++;
    }

    private static string Root()
    {
        return Path.Combine(Path.GetTempPath(), "BehaviorTest_" + Guid.NewGuid().ToString("N"));
    }

    public static int Main()
    {
        try
        {
            string ordinary = Path.Combine(Root(), "notepad.exe");
            Check("Ordinary process stays quiet", BehavioralActivityAnalyzer.AssessProcessStart(
                ordinary, "notepad.exe document.txt", "explorer.exe") == null);
            Check("Office spawning shell is suspicious", BehavioralActivityAnalyzer.AssessProcessStart(
                @"C:\Windows\System32\cmd.exe", "cmd.exe /c echo hi", "WINWORD.EXE") != null);
            Check("Encoded PowerShell is suspicious", BehavioralActivityAnalyzer.AssessProcessStart(
                @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                "powershell -EncodedCommand AAAA", "explorer.exe") != null);
            Check("Executable launched from Temp is suspicious", BehavioralActivityAnalyzer.AssessProcessStart(
                Path.Combine(Path.GetTempPath(), "example.exe"), "example.exe", "explorer.exe") != null);

            var analyzer = new BehavioralActivityAnalyzer();
            string root = Root();
            DateTime t = new DateTime(2026, 10, 16, 0, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < 11; i++)
            {
                BehavioralFinding f = analyzer.ObserveFileChange(
                    Path.Combine(root, "report" + i + ".locked"),
                    Path.Combine(root, "report" + i + ".docx"),
                    WatcherChangeTypes.Renamed, t.AddMilliseconds(i * 20));
                Check("No early alert " + i, f == null);
            }
            BehavioralFinding alert = analyzer.ObserveFileChange(
                Path.Combine(root, "report11.locked"), Path.Combine(root, "report11.docx"),
                WatcherChangeTypes.Renamed, t.AddSeconds(1));
            Check("Twelve document renames produce alert", alert != null && alert.Score >= 80);
            Check("Repeated event does not alert", analyzer.ObserveFileChange(
                Path.Combine(root, "report11.locked"), Path.Combine(root, "report11.docx"),
                WatcherChangeTypes.Renamed, t.AddSeconds(1.1)) == null);
            Check("Cooldown suppresses second alarm", analyzer.ObserveFileChange(
                Path.Combine(root, "report12.locked"), Path.Combine(root, "report12.docx"),
                WatcherChangeTypes.Renamed, t.AddSeconds(2)) == null);

            var edits = new BehavioralActivityAnalyzer();
            for (int i = 0; i < 34; i++)
                Check("Modification below threshold " + i, edits.ObserveFileChange(
                    Path.Combine(root, "file" + i + ".txt"), null,
                    WatcherChangeTypes.Changed, t.AddMilliseconds(i * 20)) == null);
            Check("35 distinct edits produce suspicion", edits.ObserveFileChange(
                Path.Combine(root, "file34.txt"), null, WatcherChangeTypes.Changed,
                t.AddSeconds(1)) != null);

            var duplicates = new BehavioralActivityAnalyzer();
            bool duplicateTriggered = false;
            for (int i = 0; i < 70; i++)
                duplicateTriggered |= duplicates.ObserveFileChange(Path.Combine(root, "same.txt"), null,
                    WatcherChangeTypes.Changed, t.AddMilliseconds(i * 25)) != null;
            Check("One repeatedly saved file is not a burst", !duplicateTriggered);
            var reset = new BehavioralActivityAnalyzer();
            for (int i = 0; i < 11; i++)
                reset.ObserveFileChange(Path.Combine(root, "a" + i + ".encrypt"),
                    Path.Combine(root, "a" + i + ".pdf"), WatcherChangeTypes.Renamed, t.AddMilliseconds(i * 20));
            reset.Reset();
            Check("Turning behavior off clears tracked events", reset.ObserveFileChange(
                Path.Combine(root, "a11.encrypt"), Path.Combine(root, "a11.pdf"),
                WatcherChangeTypes.Renamed, t.AddSeconds(1)) == null);

            Console.WriteLine("Failures: " + failures);
            return failures == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 2;
        }
    }
}
