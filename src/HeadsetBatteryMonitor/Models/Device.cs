using System.Collections.Generic;

namespace HeadsetBatteryMonitor.Models
{
    public class Device
    {
        public string Name { get; set; }
        public string VendorId { get; set; }
        public string ProductId { get; set; }
        public Dictionary<string, Level> Levels { get; set; }
    }
}
