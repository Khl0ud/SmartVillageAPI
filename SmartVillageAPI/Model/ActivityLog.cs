using SmartVillageAPI.Model;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ActivityLog
{
    [Key]
    public int Id { get; set; }
    public string? ActionType { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("Device")]
    public int DeviceId { get; set; }
    public Device Device { get; set; }

    [ForeignKey("User")]
    public string? UserId { get; set; }
    public ApplicationUser User { get; set; }
}