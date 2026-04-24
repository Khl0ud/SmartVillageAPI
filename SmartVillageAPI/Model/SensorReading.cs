using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartVillageAPI.Model
{
    public class SensorReading
    {
        [Key]
        public int Id { get; set; }
        public ReadingType Type { get; set; }
        public double Value { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("Device")]
        public int DeviceId { get; set; }
        public Device? Device { get; set; }
    }
}
