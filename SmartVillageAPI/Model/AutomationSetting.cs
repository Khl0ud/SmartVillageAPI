using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartVillageAPI.Model
{
    public class AutomationSetting
    {
        [Key]
        public int Id { get; set; }
        public bool IsAutoIrrigationEnabled { get; set; }
        public double TargetSoilMoisture { get; set; } // الحد الأدنى للرطوبة للزراعة

        // ✅ الإضافة الجديدة الخاصة بشاشة الغاز (خليناها true كأمان افتراضي)
        public bool IsGasAutoProtectionEnabled { get; set; } = true;

        [ForeignKey("Zone")]
        public int ZoneId { get; set; }
        public Zone Zone { get; set; }
    }
}