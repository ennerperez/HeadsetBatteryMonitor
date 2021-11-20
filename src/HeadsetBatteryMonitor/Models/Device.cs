namespace HeadsetBatteryMonitor.Models
{
    public class Device
    {
        public string? Name { get; set; }
        public string? VendorId { get; set; }
        public string? ProductId { get; set; }
        public decimal Success { get; set; }
        public decimal Warning { get; set; }
        public decimal Danger { get; set; }
    }
}
