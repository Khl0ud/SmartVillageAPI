using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartVillageAPI.Model;
using System.Security.Claims;

namespace SmartVillageAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WasteController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WasteController(AppDbContext context)
        {
            _context = context;
        }

        // 1. الداتا المجمعة لشاشة الـ Overview
        [HttpGet("Dashboard/{zoneId}")]
        public async Task<IActionResult> GetDashboard(int zoneId)
        {
            // بنجيب كل سلات الزبالة اللي في الزون دي مع آخر قراءة ليهم
            var bins = await _context.Devices
                .Where(d => d.ZoneId == zoneId)
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Latitude,
                    d.Longitude,
                    // بنجيب آخر قراءة لنسبة الامتلاء للسلة دي
                    FillLevel = _context.SensorReadings
                                .Where(s => s.DeviceId == d.Id && s.Type == ReadingType.Distance) // افتراض إن الـ Distance هو الامتلاء
                                .OrderByDescending(s => s.CreatedAt)
                                .Select(s => s.Value)
                                .FirstOrDefault()
                })
                .ToListAsync();

            if (!bins.Any()) return Ok(new { Message = "No bins found in this zone." });

            // الحسابات اللي الموبايل محتاجها
            int critical = bins.Count(b => b.FillLevel >= 80);
            int moderate = bins.Count(b => b.FillLevel >= 50 && b.FillLevel < 80);
            int healthy = bins.Count(b => b.FillLevel < 50);
            double averageFill = bins.Average(b => b.FillLevel);

            return Ok(new
            {
                TotalBins = bins.Count,
                CriticalCount = critical,
                ModerateCount = moderate,
                HealthyCount = healthy,
                AverageFillPercentage = Math.Round(averageFill, 1),
                BinsDetails = bins // الموبايل هيستخدم دي عشان يرسم الخريطة (عشان فيها Lat/Lng و FillLevel)
            });
        }

        // 2. جدولة طلب تجميع زبالة (Collection Request)
        [HttpPost("SchedulePickup")]
        public async Task<IActionResult> SchedulePickup([FromBody] SchedulePickupDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var pickup = new WasteCollectionRequest
            {
                DeviceId = request.BinId,
                ScheduledTime = request.ScheduledDateTime,
                UserId = userId,
                Status = "Pending"
            };

            _context.WasteCollectionRequests.Add(pickup);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Collection scheduled successfully!" });
        }
    }

    // DTO للريكويست
    public class SchedulePickupDto
    {
        public int BinId { get; set; }
        public DateTime ScheduledDateTime { get; set; }
    }
}