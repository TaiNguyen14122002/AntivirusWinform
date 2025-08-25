using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace ScanAndRemoveVirus.Modal
{
    internal class ThreatDetection
    {
        public DateTime DetectedTime { get; set; }
        public string FileName { get; set; }
        public string ThreatName { get; set; }
        public string Category { get; set; }
        public string OriginalPath { get; set; }
        public string Status { get; set; }
        public long FileSizeBytes { get; set; }
    }
}
