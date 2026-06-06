using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartVillageAPI.Model
{
    public class Device
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } // e.g., Kitchen Fan, Main Pump, Parking Spot 1, Rain Servo
        public DeviceType Type { get; set; }
        public bool IsActive { get; set; }
        public string CurrentState { get; set; } // e.g., "ON", "OFF", "Occupied", "Empty", "90°"
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [ForeignKey("Zone")]
        public int ZoneId { get; set; }
        public Zone Zone { get; set; }

        // Navigation Properties
        public ICollection<SensorReading> Readings { get; set; }
        public ICollection<ActivityLog> ActivityLogs { get; set; }
    }
}