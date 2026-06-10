using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartVillageAPI.Model
{
    public class IrrigationZone
    {
        [Key]
        public int Id { get; set; }

        // اسم الزون زي "Zone 1", "Zone 2"
        public string Name { get; set; } = string.Empty;

        // نوع النبات المزروع في الزون دي
        public PlantType PlantType { get; set; } = PlantType.None;

        // الحد الأدنى للرطوبة اللي لو وصلها يبدأ الري أوتوماتيك (%)
        public double MoistureThreshold { get; set; } = 40.0;

        // آخر قراءة رطوبة تربة وصلت من الحساس
        public double CurrentSoilMoisture { get; set; } = 0.0;

        // حالة الصمام (OPEN / CLOSED)
        public string ValveStatus { get; set; } = "CLOSED";

        // هل الري الأوتوماتيك شغال؟
        public bool IsAutoMode { get; set; } = false;

        // آخر مرة اتسقى فيها
        public DateTime? LastIrrigatedAt { get; set; }

        // مربوط بالـ Zone الأصلية (Agriculture Zone = Id 2)
        [ForeignKey("Zone")]
        public int ZoneId { get; set; }
        public Zone Zone { get; set; }

        // مربوط بالمستخدم صاحب الحديقة
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        // Navigation
        public ICollection<IrrigationLog> Logs { get; set; } = new List<IrrigationLog>();
    }
}
