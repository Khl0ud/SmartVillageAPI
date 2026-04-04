namespace SmartVillageAPI.DTOs
{
    // البيانات اللي الموبايل بيبعتها وقت إنشاء حساب
    public class RegisterModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    // البيانات اللي الموبايل بيبعتها وقت الدخول
    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    // شكل البيانات اللي السيرفر هيرجعها للموبايل (سواء بعد التسجيل أو الدخول)
    public class AuthModel
    {
        public string Message { get; set; }
        public bool IsAuthenticated { get; set; }
        public string Token { get; set; }
        public DateTime? ExpiresOn { get; set; }
        public string UserId { get; set; }
        public string FullName { get; set; }
    }
}