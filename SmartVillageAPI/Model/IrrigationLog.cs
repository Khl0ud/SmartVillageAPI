using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartVillageAPI.Model
{
    public class IrrigationLog
    {
        [Key]
        public int Id { get; set; }

        public int IrrigationZoneId { get; set; }

        [ForeignKey("IrrigationZoneId")]
        public IrrigationZone IrrigationZone { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }

        // كمية المياه المستخدمة (بالليتر) - اختياري لو عندك flow sensor
        public double? WaterUsedLiters { get; set; }

        // رطوبة التربة قبل الري
        public double SoilMoistureBeforeIrrigation { get; set; }

        // رطوبة التربة بعد الري
        public double? SoilMoistureAfterIrrigation { get; set; }

        // هل الري كان أوتوماتيك أم يدوي؟
        public bool IsAutomatic { get; set; } = false;

        public string? TriggeredByUserId { get; set; }
    }
}
