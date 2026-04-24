using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SmartVillageAPI.DTOs;
using SmartVillageAPI.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartVillageAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        // 1. إنشاء حساب جديد (بيرجع توكن)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return BadRequest(new AuthModel { Message = "Email already exists!", IsAuthenticated = false });

            ApplicationUser user = new ApplicationUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Email,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new AuthModel { Message = "User creation failed!", IsAuthenticated = false });

            // هنا بننادي على دالة توليد التوكن عشان نرجعه فوراً بعد التسجيل
            var authModel = await GenerateJwtTokenAsync(user);
            authModel.Message = "User created successfully!";

            return Ok(authModel);
        }

        // 2. تسجيل الدخول (بيرجع توكن)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                // بننادي على نفس الدالة هنا كمان
                var authModel = await GenerateJwtTokenAsync(user);
                authModel.Message = "Welcome back!";

                return Ok(authModel);
            }

            return Unauthorized(new AuthModel { Message = "Invalid email or password", IsAuthenticated = false });
        }

        // 3. الحصول على بيانات البروفايل
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { Message = "User not found" });

            var profile = new ProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                WalletBalance = user.WalletBalance
            };

            return Ok(profile);
        }

        // 4. تحديث بيانات البروفايل
        [Authorize]
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { Message = "User not found" });

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { Message = errors });
            }

            return Ok(new { Message = "Profile updated successfully" });
        }

        // 5. تغيير كلمة السر
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { Message = "User not found" });

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { Message = errors });
            }

            return Ok(new { Message = "Password changed successfully" });
        }

        // 5. تسجيل الخروج
        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // في حالة الـ JWT الـ logout بيتم غالباً في الموبايل بمسح التوكن
            // لكن بنعمل Endpoint هنا عشان الموبايل ينادي عليه لو محتاج يمسح حاجة في السيرفر أو للتأكيد
            return Ok(new { Message = "Logged out successfully" });
        }

        // --- دالة مساعدة لتوليد التوكن (عشان نمنع تكرار الكود) ---
        private async Task<AuthModel> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddDays(7), // خلينا التوكن يعيش 7 أيام عشان يوزر الموبايل ميخرجش كل شوية
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new AuthModel
            {
                IsAuthenticated = true,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresOn = token.ValidTo,
                UserId = user.Id,
                FullName = user.FullName
            };
        }
    }
}