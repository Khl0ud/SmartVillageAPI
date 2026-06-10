using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartVillageAPI.Model
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }   // e.g., "DANGER DETECTED!"
        public string Message { get; set; } // e.g., "Gas leak detected in Kitchen!"
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("User")]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}