using System.ComponentModel;

namespace ClassScanner
{
    public class Config
    {
        [Description("Perform regular scanning throughout the round")]
        public bool RegularScanning { get; set; } = true;

        [Description("Time in seconds the scan takes to complete")]
        public float ScanLength { get; set; } = 90f;

        [Description("Continue scanning after the nuke detonates?")]
        public bool ScanAfterNuke { get; set; }

        [Description("CASSIE message when scan starts")]
        public string ScanStartMessageCassie { get; set; } = "FULL FACILITY SCAN IN {LENGTH} SECONDS";

        [Description("Caption when scan starts")]
        public string ScanStartMessageCaption { get; set; } = "Full facility scan in {LENGTH} seconds";

        [Description("CASSIE message when nobody is detected")]
        public string ScanNobodyMessageCassie { get; set; } = "SCAN COMPLETE . NO SUBJECTS DETECTED";

        [Description("Caption when nobody is detected")]
        public string ScanNobodyMessageCaption { get; set; } = "Scan complete. No subjects detected";

        [Description("Minutes to wait after a scan finishes before announcing next scan")]
        public int DelayAfterScanMinutes { get; set; } = 9;
    }
}
