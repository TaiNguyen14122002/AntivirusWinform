using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanAndRemoveVirus.Modal
{
    internal class UpdateHistoryItem
    {
        public int UpdateID { get; set; }

        public DateTime UpdateTime { get; set; }

        public string DatabaseVersion { get; set; }

        public string Size { get; set; }

        public string UpdateSource { get; set; }

        public string Status { get; set; }

        public string Note { get; set; }

        public string UpdateMethod { get; set; }
    }
}
