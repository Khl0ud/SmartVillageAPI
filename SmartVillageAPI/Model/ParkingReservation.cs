using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartVillageAPI.Model
{
    public class ParkingReservation
    {
        [Key]
        public int Id { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = "Active"; // Active, Completed, Cancelled

        [ForeignKey("Device")]
        public int DeviceId { get; set; } // رقم الركنة (مثلاً Spot A1)
        public Device? Device { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
    }
}