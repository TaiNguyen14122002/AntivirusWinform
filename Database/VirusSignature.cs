using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanAndRemoveVirus.Database
{
    public class VirusSignature
    {
        public long SignatureID { get; set; }
        public string MalwareName { get; set; }
        public string MalwareFamily { get; set; }
        public string Category { get; set; }
        public string SignatureType { get; set; }
        public string MD5 { get; set; }
        public string SHA1 { get; set; }
        public string SHA256 { get; set; }
        public string FileExtension { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
        public string RecommendedAction { get; set; }
    }
}

