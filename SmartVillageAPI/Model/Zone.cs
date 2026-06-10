using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartVillageAPI.Model
{
    public class Zone
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } // e.g., "My Smart Home", "Main Parking"
        public string Icon { get; set; }
        public string Description { get; set; }

        // التعديل الأول: تحديد هل المنطقة عامة لكل سكان القرية أم خاصة؟
        public bool IsPublic { get; set; } = false;

        // التعديل الثاني: ربط المنطقة بالمستخدم (صاحب البيت)
        // لاحظي علامة الاستفهام (?) تعني أن الحقل يمكن أن يكون فارغاً (Null) للأماكن العامة
        [ForeignKey("User")]
        public string? UserId { get; set; }
        public ApplicationUser User { get; set; }

        // Navigation Properties
        public ICollection<Device> Devices { get; set; }
        public AutomationSetting AutomationSetting { get; set; }
    }
}
