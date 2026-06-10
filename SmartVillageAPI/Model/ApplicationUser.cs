using Microsoft.AspNetCore.Identity;

namespace SmartVillageAPI.Model
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public double WalletBalance { get; set; } = 0.0; // رصيد المحفظة
        public ICollection<Notification> Notifications { get; set; }
        public ICollection<Zone> Zones { get; set; }
    }
}
