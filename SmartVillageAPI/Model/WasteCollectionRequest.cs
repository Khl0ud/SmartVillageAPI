using SmartVillageAPI.Model;

public class WasteCollectionRequest
{
    public int Id { get; set; }
    public int DeviceId { get; set; } // رقم السلة
    public Device? Device { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Completed
    public string? UserId { get; set; }
}