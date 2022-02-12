namespace HeadsetBatteryMonitor.Models
{
    public class Device
    {
        public string Name { get; set; }
        public string VendorId { get; set; }
        public string ProductId { get; set; }
        public Levels Levels { get; set; }
    }

    public class Levels
    {
        public Level High { get; set; }
        public Level Normal { get; set; }
        public Level Low { get; set; }
        public Level Critical { get; set; }
    }

    public class Level
    {
        public decimal? Value { get; set; }
        public string Color { get; set; }
        public Notification Notification { get; set; }

    }

    public class Notification
    {
        public bool Enabled { get; set; }
        public int Timeout { get; set; }

        public string Sound { get; set; }
    }
}

