// Generate an inert SQL CDC signature script from a VERIFIED known-malware sample.
// File content is READ ONLY; executable samples are not launched.
using System;
using System.IO;
using System.Text;
using System.Threading;
using ScanAndRemoveVirus.Services;

internal static class CdcSignatureBuilder
{
    private static int Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: CdcSignatureBuilder.exe <known-sample-file> <VirusSignatures.SignatureID> <GroupName> <output.sql>");
            return 2;
        }
        string source = Path.GetFullPath(args[0]);
        string output = Path.GetFullPath(args[3]);
        long signatureId;
        if (!long.TryParse(args[1], out signatureId) || signatureId <= 0 || !File.Exists(source) ||
            string.Equals(source, output, StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Invalid source, output path or SignatureID.");
            return 2;
        }
        string temp = output + ".working-" + Guid.NewGuid().ToString("N");
        try
        {
            long chunks;
            var config = new HybridSignatureScanner.CdcConfig();
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read,
                256 * 1024, FileOptions.SequentialScan))
            using (var writer = new StreamWriter(temp, false, new UTF8Encoding(false)))
                chunks = HybridSignatureScanner.WriteContentDefinedSignatureSql(input, writer, config,
                    signatureId, args[2], 3, 0.6m, CancellationToken.None);
            if (File.Exists(output)) throw new IOException("Output already exists; choose a new output path.");
            File.Move(temp, output);
            Console.WriteLine("Created " + chunks + " CDC chunks in " + output);
            Console.WriteLine("SQL group defaults to IsActive=0. Verify independently before activation.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            try { File.Delete(temp); } catch (IOException) { }
            return 1;
        }
    }
}
