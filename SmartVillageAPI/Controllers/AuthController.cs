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
                expires: DateTime.Now.AddYears(10), //10 years <- خلينا التوكن يعيش 7 أيام عشان يوزر الموبايل ميخرجش كل شوية
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