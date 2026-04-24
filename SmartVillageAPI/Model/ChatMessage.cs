using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartVillageAPI.Model
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;
        public ApplicationUser? Sender { get; set; }

        [Required]
        public string ReceiverId { get; set; } = string.Empty;
        public ApplicationUser? Receiver { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}
