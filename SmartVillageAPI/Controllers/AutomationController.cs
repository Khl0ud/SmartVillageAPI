using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartVillageAPI.Model;
using System.Security.Claims;

namespace SmartVillageAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   // [Authorize] // 🔒 لازم يوزر مسجل
    public class AutomationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AutomationController(AppDbContext context)
        {
            _context = context;
        }

        // 1. جلب الإعدادات عشان الموبايل يعرض الزرار مفتوح ولا مقفول أول ما الشاشة تفتح
        [HttpGet("{zoneId}")]
        public async Task<IActionResult> GetSettings(int zoneId)
        {
            var settings = await _context.AutomationSettings.FirstOrDefaultAsync(a => a.ZoneId == zoneId);

            // لو مفيش إعدادات لسه، نرجع قيم افتراضية
            if (settings == null)
                return Ok(new { IsGasAutoProtectionEnabled = true, IsAutoIrrigationEnabled = false });

            return Ok(settings);
        }

        // 2. تحديث حالة زرار حماية الغاز
        [HttpPut("ToggleGasProtection/{zoneId}")]
        public async Task<IActionResult> ToggleGasProtection(int zoneId, [FromBody] bool isEnabled)
        {
            var settings = await _context.AutomationSettings.FirstOrDefaultAsync(a => a.ZoneId == zoneId);

            // لو الإعدادات مش موجودة في الداتابيز، نكريتها
            if (settings == null)
            {
                settings = new AutomationSetting
                {
                    ZoneId = zoneId,
                    IsGasAutoProtectionEnabled = isEnabled
                };
                _context.AutomationSettings.Add(settings);
            }
            else
            {
                // لو موجودة، نحدثها بس
                settings.IsGasAutoProtectionEnabled = isEnabled;
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Gas Auto Protection is now {(isEnabled ? "ON" : "OFF")}" });
        }
        [HttpPut("UpdateFarmingSettings/{zoneId}")]
        public async Task<IActionResult> UpdateFarmingSettings(int zoneId, [FromBody] FarmingSettingsDto request)
        {
            var settings = await _context.AutomationSettings.FirstOrDefaultAsync(a => a.ZoneId == zoneId);

            if (settings == null)
            {
                settings = new AutomationSetting { ZoneId = zoneId };
                _context.AutomationSettings.Add(settings);
            }

            // تحديث الإعدادات اللي جاية من الموبايل
            settings.IsAutoIrrigationEnabled = request.IsAutoMode;
            settings.TargetSoilMoisture = request.MoistureThreshold;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Farming parameters updated successfully!" });
        }

        // ضيف الـ DTO ده تحت الكلاس أو في فولدر الـ DTOs
        public class FarmingSettingsDto
        {
            public bool IsAutoMode { get; set; }
            public double MoistureThreshold { get; set; }
        }
    }
}