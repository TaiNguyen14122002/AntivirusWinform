using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ScanAndRemoveVirus.Services
{
    /// <summary>Đọc metadata PE, xác minh Authenticode bằng WinVerifyTrust; không thực thi file.</summary>
    public static class PortableExecutableInspector
    {
        public sealed class Finding
        {
            public bool IsPe { get; set; }
            public string Machine { get; set; }
            public int SectionCount { get; set; }
            public bool IsSigned { get; set; }
            public bool SignatureTrusted { get; set; }
            public string SignatureResult { get; set; }
            public string Signer { get; set; }
            public string Note { get; set; }
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WinTrustFileInfo
        {
            public uint Size;
            [MarshalAs(UnmanagedType.LPWStr)] public string Path;
            public IntPtr FileHandle;
            public IntPtr KnownSubject;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WinTrustData
        {
            public uint Size;
            public IntPtr PolicyCallback;
            public IntPtr SipClient;
            public uint UIChoice;
            public uint RevocationChecks;
            public uint UnionChoice;
            public IntPtr FileInfo;
            public uint StateAction;
            public IntPtr StateData;
            public IntPtr UrlReference;
            public uint ProviderFlags;
            public uint UIContext;
            public IntPtr SignatureSettings;
        }
        [DllImport("wintrust.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern uint WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid action, IntPtr data);

        public static Finding Inspect(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) throw new FileNotFoundException("Không thấy tệp.", path);
            var finding = new Finding { Note = "Chữ ký số đáng tin cậy không đảm bảo tệp vô hại." };
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 4096))
            using (var reader = new BinaryReader(file))
            {
                if (file.Length < 256 || reader.ReadUInt16() != 0x5A4D) return finding;
                file.Position = 0x3c;
                uint offset = reader.ReadUInt32();
                if (offset < 64 || offset > file.Length - 24) return finding;
                file.Position = offset;
                if (reader.ReadUInt32() != 0x00004550) return finding;
                finding.IsPe = true;
                ushort machine = reader.ReadUInt16();
                finding.Machine = machine == 0x014c ? "x86" : machine == 0x8664 ? "x64" :
                    machine == 0xAA64 ? "ARM64" : "0x" + machine.ToString("X4");
                finding.SectionCount = reader.ReadUInt16();
            }
            try
            {
                // Chỉ nhận diện nhà phát hành: kết quả này KHÔNG xác minh độ tin cậy.
                using (var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(path)))
                {
                    finding.IsSigned = true;
                    finding.Signer = cert.Subject;
                }
            }
            catch (CryptographicException) { }
            catch (System.Security.SecurityException) { }
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                finding.SignatureResult = "WinVerifyTrust chỉ khả dụng trên Windows.";
                return finding;
            }
            IntPtr filePtr = IntPtr.Zero, dataPtr = IntPtr.Zero;
            try
            {
                var info = new WinTrustFileInfo { Size = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo)),
                    Path = Path.GetFullPath(path) };
                filePtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustFileInfo)));
                Marshal.StructureToPtr(info, filePtr, false);
                var data = new WinTrustData
                {
                    Size = (uint)Marshal.SizeOf(typeof(WinTrustData)), UIChoice = 2,
                    RevocationChecks = 0, UnionChoice = 1, FileInfo = filePtr,
                    StateAction = 0, ProviderFlags = 0x00001010 // cache-only URL + revocation none
                };
                dataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustData)));
                Marshal.StructureToPtr(data, dataPtr, false);
                var action = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
                uint status = WinVerifyTrust(IntPtr.Zero, action, dataPtr);
                finding.SignatureTrusted = status == 0;
                finding.SignatureResult = finding.SignatureTrusted ?
                    "Windows xác minh thành công chữ ký với chính sách cache-only." :
                    "Chưa xác minh được chữ ký (mã Windows: 0x" + status.ToString("X8") + ").";
            }
            catch (Exception ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException ||
                ex is System.ComponentModel.Win32Exception || ex is System.Security.SecurityException)
            { finding.SignatureResult = "Không thể xác minh: " + ex.GetType().Name; }
            finally
            {
                if (dataPtr != IntPtr.Zero)
                { Marshal.DestroyStructure(dataPtr, typeof(WinTrustData)); Marshal.FreeHGlobal(dataPtr); }
                if (filePtr != IntPtr.Zero)
                { Marshal.DestroyStructure(filePtr, typeof(WinTrustFileInfo)); Marshal.FreeHGlobal(filePtr); }
            }
            return finding;
        }
    }
}
