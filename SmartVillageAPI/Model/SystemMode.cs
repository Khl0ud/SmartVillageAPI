using System.ComponentModel.DataAnnotations;

namespace SmartVillageAPI.Model
{
    public class SystemMode
    {
        [Key]
        public int Id { get; set; }

        // حالة النظام (Auto/Manual)
        public bool IsAuto { get; set; } = true;

        // آخر تحديث
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
