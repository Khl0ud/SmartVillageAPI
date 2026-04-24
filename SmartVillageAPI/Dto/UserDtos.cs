namespace SmartVillageAPI.DTOs
{
    public class ProfileDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public double WalletBalance { get; set; }
    }

    public class UpdateProfileDto
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
