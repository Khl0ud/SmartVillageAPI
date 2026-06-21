using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartVillageAPI.Dto;
using SmartVillageAPI.Hubs;
using SmartVillageAPI.Model;
using SmartVillageAPI.Services;
using System.Security.Claims;
using System.Text.Json;

namespace SmartVillageAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize]
    public class IrrigationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<SmartVillageHub> _hubContext;
        private readonly IMqttService _mqttService;

        public IrrigationController(AppDbContext context,
                                    IHubContext<SmartVillageHub> hubContext,
                                    IMqttService mqttService)
        {
            _context = context;
            _hubContext = hubContext;
            _mqttService = mqttService;
        }

        // ============================================================
        // 1. Dashboard - بيرجع كل حاجة للشاشة الرئيسية (Garden Hub)
        // GET /api/Irrigation/Dashboard/{userId}
        // ============================================================
        [HttpGet("Dashboard/{userId}")]
        public async Task<IActionResult> GetDashboard(string userId)
        {
            var zones = await _context.IrrigationZones
                .Where(z => z.UserId == userId)
                .ToListAsync();

            if (!zones.Any())
                return Ok(new IrrigationDashboardDto
                {
                    AverageSoilMoisture = 0,
                    AiRecommendation = GenerateAiRecommendation(0, 0, false),
                    Zones = new List<IrrigationZoneDto>()
                });

            double avgMoisture = zones.Average(z => z.CurrentSoilMoisture);
            bool anyAutoActive = zones.Any(z => z.IsAutoMode);

            return Ok(new IrrigationDashboardDto
            {
                AverageSoilMoisture = Math.Round(avgMoisture, 1),
                AiRecommendation = GenerateAiRecommendation(avgMoisture, zones.Count, anyAutoActive),
                Zones = zones.Select(MapToDto).ToList()
            });
        }

        // ============================================================
        // 2. جلب كل الـ Zones بتاعة اليوزر
        // GET /api/Irrigation/Zones/{userId}
        // ============================================================
        [HttpGet("Zones/{userId}")]
        public async Task<IActionResult> GetZones(string userId)
        {
            var zones = await _context.IrrigationZones
                .Where(z => z.UserId == userId)
                .ToListAsync();

            return Ok(zones.Select(MapToDto));
        }

        // ============================================================
        // 3. جلب تفاصيل Zone واحدة
        // GET /api/Irrigation/Zone/{id}
        // ============================================================
        [HttpGet("Zone/{id}")]
        public async Task<IActionResult> GetZone(int id)
        {
            var zone = await _context.IrrigationZones.FindAsync(id);
            if (zone == null) return NotFound(new { Message = "Irrigation zone not found" });

            return Ok(MapToDto(zone));
        }

        // ============================================================
        // 4. إنشاء Irrigation Zone جديدة
        // POST /api/Irrigation/Zone
        // ============================================================
        [HttpPost("Zone")]
        public async Task<IActionResult> CreateZone([FromBody] CreateIrrigationZoneDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? dto.UserId;

            // التأكد إن الـ Agriculture Zone موجودة (ZoneId = 2 افتراضياً)
            var parentZone = await _context.Zones.FindAsync(dto.ZoneId);
            if (parentZone == null)
                return BadRequest(new { Message = "Parent zone not found" });

            var irrigationZone = new IrrigationZone
            {
                Name = dto.Name,
                ZoneId = dto.ZoneId,
                UserId = userId,
                MoistureThreshold = dto.MoistureThreshold,
                PlantType = dto.PlantType,
                IsAutoMode = false,
                ValveStatus = "CLOSED"
            };

            _context.IrrigationZones.Add(irrigationZone);
            await _context.SaveChangesAsync();

            return Ok(MapToDto(irrigationZone));
        }

        // ============================================================
        // 5. تحديث إعدادات الـ Zone (Plant Type + Threshold + Auto Mode)
        // PUT /api/Irrigation/Zone/{id}/Settings
        // ============================================================
        [HttpPut("Zone/{id}/Settings")]
        public async Task<IActionResult> UpdateSettings(int id, [FromBody] UpdateIrrigationSettingsDto dto)
        {
            var zone = await _context.IrrigationZones.FindAsync(id);
            if (zone == null) return NotFound(new { Message = "Irrigation zone not found" });

            zone.PlantType = dto.PlantType;
            zone.MoistureThreshold = dto.MoistureThreshold;
            zone.IsAutoMode = dto.IsAutoMode;

            await _context.SaveChangesAsync();

            // إبلاغ الموبايل بالتغيير عبر SignalR
            await _hubContext.Clients.Group($"Zone_{zone.ZoneId}")
                .SendAsync("IrrigationSettingsUpdated", MapToDto(zone));

            return Ok(new { Message = "Settings saved successfully!", Zone = MapToDto(zone) });
        }

        // ============================================================
        // 6. Start / Stop Irrigation يدوي
        // POST /api/Irrigation/Zone/{id}/Control
        // ============================================================
        [HttpPost("Zone/{id}/Control")]
        public async Task<IActionResult> ControlIrrigation(int id, [FromBody] ControlIrrigationDto dto)
        {
            var zone = await _context.IrrigationZones.FindAsync(id);
            if (zone == null) return NotFound(new { Message = "Irrigation zone not found" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (dto.Action.ToUpper() == "START")
            {
                zone.ValveStatus = "OPEN";
                zone.LastIrrigatedAt = DateTime.UtcNow;

                // إنشاء Log جديد
                var log = new IrrigationLog
                {
                    IrrigationZoneId = zone.Id,
                    StartedAt = DateTime.UtcNow,
                    SoilMoistureBeforeIrrigation = zone.CurrentSoilMoisture,
                    IsAutomatic = false,
                    TriggeredByUserId = userId
                };
                _context.IrrigationLogs.Add(log);

                // إرسال أمر MQTT للهاردوير
                await _mqttService.PublishAsync($"FadiSmartVillage2026/irrigation/zone/{zone.Id}/valve", "OPEN");
            }
            else if (dto.Action.ToUpper() == "STOP")
            {
                zone.ValveStatus = "CLOSED";

                // إغلاق آخر Log مفتوح
                var openLog = await _context.IrrigationLogs
                    .Where(l => l.IrrigationZoneId == zone.Id && l.EndedAt == null)
                    .OrderByDescending(l => l.StartedAt)
                    .FirstOrDefaultAsync();

                if (openLog != null)
                {
                    openLog.EndedAt = DateTime.UtcNow;
                    openLog.SoilMoistureAfterIrrigation = zone.CurrentSoilMoisture;
                }

                await _mqttService.PublishAsync($"FadiSmartVillage2026/irrigation/zone/{zone.Id}/valve", "CLOSED");
            }
            else
            {
                return BadRequest(new { Message = "Action must be START or STOP" });
            }

            await _context.SaveChangesAsync();

            // إبلاغ الموبايل بالتغيير
            await _hubContext.Clients.Group($"Zone_{zone.ZoneId}")
                .SendAsync("IrrigationStateChanged", new
                {
                    ZoneId = zone.Id,
                    ValveStatus = zone.ValveStatus,
                    LastIrrigatedAt = zone.LastIrrigatedAt
                });

            return Ok(new { Message = $"Irrigation {dto.Action}ED successfully", ValveStatus = zone.ValveStatus });
        }

        // ============================================================
        // 7. استقبال قراءة رطوبة التربة من الهاردوير (ESP32)
        // POST /api/Irrigation/Zone/{id}/SoilReading
        // ============================================================
        [HttpPost("Zone/{id}/SoilReading")]
        [AllowAnonymous] // الهاردوير بيكلمها من غير توكن
        public async Task<IActionResult> UpdateSoilMoisture(int id, [FromBody] SoilReadingDto dto)
        {
            var zone = await _context.IrrigationZones.FindAsync(id);
            if (zone == null) return NotFound(new { Message = "Irrigation zone not found" });

            zone.CurrentSoilMoisture = dto.Value;

            // الري الأوتوماتيك: لو الرطوبة وصلت للحد الأدنى وفيه Auto Mode
            if (zone.IsAutoMode && dto.Value <= zone.MoistureThreshold && zone.ValveStatus == "CLOSED")
            {
                zone.ValveStatus = "OPEN";
                zone.LastIrrigatedAt = DateTime.UtcNow;

                var log = new IrrigationLog
                {
                    IrrigationZoneId = zone.Id,
                    StartedAt = DateTime.UtcNow,
                    SoilMoistureBeforeIrrigation = dto.Value,
                    IsAutomatic = true
                };
                _context.IrrigationLogs.Add(log);

                await _mqttService.PublishAsync($"FadiSmartVillage2026/irrigation/zone/{zone.Id}/valve", "OPEN");

                // إشعار للموبايل
                var notification = new Notification
                {
                    Title = "🌱 Auto Irrigation Started",
                    Message = $"{zone.Name}: Soil moisture dropped to {dto.Value}%. Valve opened automatically.",
                    UserId = zone.UserId ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);

                await _hubContext.Clients.Group($"Zone_{zone.ZoneId}")
                    .SendAsync("ReceiveNotification", new
                    {
                        Title = notification.Title,
                        Message = notification.Message,
                        Time = notification.CreatedAt
                    });
            }

            await _context.SaveChangesAsync();

            // إبلاغ الموبايل بالقراءة الجديدة
            await _hubContext.Clients.Group($"Zone_{zone.ZoneId}")
                .SendAsync("SoilMoistureUpdated", new
                {
                    ZoneId = zone.Id,
                    ZoneName = zone.Name,
                    SoilMoisture = zone.CurrentSoilMoisture,
                    ValveStatus = zone.ValveStatus
                });

            return Ok(new { Message = "Soil moisture updated", ValveStatus = zone.ValveStatus });
        }

        // ============================================================
        // 8. جلب الـ Irrigation Logs
        // GET /api/Irrigation/Logs/{userId}
        // ============================================================
        [HttpGet("Logs/{userId}")]
        public async Task<IActionResult> GetLogs(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var logs = await _context.IrrigationLogs
                .Include(l => l.IrrigationZone)
                .Where(l => l.IrrigationZone.UserId == userId)
                .OrderByDescending(l => l.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(logs.Select(l => new IrrigationLogDto
            {
                Id = l.Id,
                ZoneName = l.IrrigationZone.Name,
                StartedAt = l.StartedAt,
                EndedAt = l.EndedAt,
                WaterUsedLiters = l.WaterUsedLiters,
                SoilMoistureBeforeIrrigation = l.SoilMoistureBeforeIrrigation,
                SoilMoistureAfterIrrigation = l.SoilMoistureAfterIrrigation,
                IsAutomatic = l.IsAutomatic,
                Duration = l.EndedAt.HasValue
                    ? FormatDuration(l.EndedAt.Value - l.StartedAt)
                    : "In progress..."
            }));
        }

        // ============================================================
        // 9. AI Recommendation
        // GET /api/Irrigation/AiRecommendation/{userId}
        // ============================================================
        // ============================================================
        // 9. AI Recommendation عامة للـ Dashboard
        // GET /api/Irrigation/AiRecommendation/{userId}
        // ============================================================
        [HttpGet("AiRecommendation/{userId}")]
        public async Task<IActionResult> GetAiRecommendation(string userId)
        {
            var zones = await _context.IrrigationZones
                .Where(z => z.UserId == userId)
                .ToListAsync();

            if (!zones.Any())
                return Ok(GenerateAiRecommendation(0, 0, false));

            double avgMoisture = zones.Average(z => z.CurrentSoilMoisture);
            bool anyAutoActive = zones.Any(z => z.IsAutoMode);

            return Ok(GenerateAiRecommendation(avgMoisture, zones.Count, anyAutoActive));
        }

        // ============================================================
        // 10. AI Recommendation مخصصة لـ Zone معينة (بتاخد نوع النبتة بعين الاعتبار)
        // GET /api/Irrigation/Zone/{id}/AiRecommendation
        // ============================================================
        [HttpGet("Zone/{id}/AiRecommendation")]
        public async Task<IActionResult> GetZoneAiRecommendation(int id)
        {
            var zone = await _context.IrrigationZones.FindAsync(id);
            if (zone == null) return NotFound(new { Message = "Irrigation zone not found" });

            return Ok(GenerateAiRecommendationForPlant(zone.CurrentSoilMoisture, zone.PlantType, zone.IsAutoMode));
        }

        // ============================================================
        // 11. قائمة كل النباتات المتاحة (للـ Dropdown في الـ Settings)
        // GET /api/Irrigation/Plants
        // ============================================================
        [HttpGet("Plants")]
        public IActionResult GetPlants()
        {
            var plants = PlantDatabase.All.Select(p => new
            {
                Value = (int)p.Type,
                Name = p.Name,
                OptimalMoisture = p.OptimalMoisture,
                OptimalTemp = p.OptimalTemp,
                BestTimeToIrrigate = p.BestTimeToIrrigate,
                WeeklyFrequency = p.WeeklyFrequency,
                Season = p.Season
            });

            return Ok(plants);
        }

        // ============================================================
        // 12. تعيين حالة النظام (Auto/Manual)
        // POST /api/Irrigation/SetSystemMode
        // ============================================================
        [HttpPost("SetSystemMode")]
        [AllowAnonymous]
        public async Task<IActionResult> SetSystemMode([FromBody] SystemModeDto mode)
        {
            try
            {
                // Get or create system mode record (always use Id = 1)
                var systemMode = await _context.SystemModes.FindAsync(1);
                if (systemMode == null)
                {
                    systemMode = new SystemMode { Id = 1, IsAuto = mode.IsAuto };
                    _context.SystemModes.Add(systemMode);
                }
                else
                {
                    systemMode.IsAuto = mode.IsAuto;
                    systemMode.LastUpdated = DateTime.UtcNow;
                    _context.SystemModes.Update(systemMode);
                }

                await _context.SaveChangesAsync();

                // إبلاغ ESP32 بالتغيير عبر MQTT
                await _mqttService.PublishAsync("FadiSmartVillage2026/irrigation/system/mode", mode.IsAuto ? "AUTO" : "MANUAL");

                return Ok(new { Message = "System mode updated successfully", IsAuto = mode.IsAuto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error updating system mode", Error = ex.Message });
            }
        }

        // ============================================================
        // 13. جلب حالة النظام (Auto/Manual)
        // GET /api/Irrigation/GetSystemMode
        // ============================================================
        [HttpGet("GetSystemMode")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSystemMode()
        {
            try
            {
                var systemMode = await _context.SystemModes.FindAsync(1);
                if (systemMode == null)
                {
                    // Create default record if doesn't exist
                    systemMode = new SystemMode { Id = 1, IsAuto = true };
                    _context.SystemModes.Add(systemMode);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { isAuto = systemMode.IsAuto });
            }
            catch (Exception ex)
            {
                // Default to Auto on error
                return Ok(new { isAuto = true });
            }
        }

        // ============================================================
        // Helper Methods
        // ============================================================

        private static IrrigationZoneDto MapToDto(IrrigationZone z) => new()
        {
            Id = z.Id,
            Name = z.Name,
            PlantType = z.PlantType.ToString(),
            MoistureThreshold = z.MoistureThreshold,
            CurrentSoilMoisture = z.CurrentSoilMoisture,
            ValveStatus = z.ValveStatus,
            IsAutoMode = z.IsAutoMode,
            LastIrrigatedAt = z.LastIrrigatedAt
        };

        /// <summary>
        /// AI Recommendation بيانات عامة (للـ Dashboard)
        /// </summary>
        private static AiRecommendationDto GenerateAiRecommendation(double avgMoisture, int zoneCount, bool autoActive)
        {
            string recommendation;
            string bestTime;
            double ecoScore;

            if (avgMoisture >= 70)
            {
                recommendation = "Soil moisture is optimal. No irrigation needed now.";
                bestTime = "Evening";
                ecoScore = 90;
            }
            else if (avgMoisture >= 50)
            {
                recommendation = "Monitor soil moisture. Next irrigation recommended in morning.";
                bestTime = "Morning";
                ecoScore = 70;
            }
            else if (avgMoisture >= 30)
            {
                recommendation = "Soil is getting dry. Consider irrigating soon.";
                bestTime = "Now";
                ecoScore = 50;
            }
            else
            {
                recommendation = "Soil moisture is critically low. Immediate irrigation required!";
                bestTime = "Immediately";
                ecoScore = 20;
            }

            if (autoActive) ecoScore = Math.Min(ecoScore + 10, 100);

            return new AiRecommendationDto
            {
                Recommendation = recommendation,
                BestTimeToIrrigate = bestTime,
                EcoScore = ecoScore,
                WaterSavingsPercent = autoActive ? Math.Round(ecoScore * 0.3, 1) : 0
            };
        }

        /// <summary>
        /// AI Recommendation مخصصة لنبتة معينة في Zone معينة
        /// </summary>
        private static AiRecommendationDto GenerateAiRecommendationForPlant(double currentMoisture, PlantType plantType, bool autoActive)
        {
            var profile = PlantDatabase.Get(plantType);

            // لو مفيش profile (None)، نرجع للـ logic العام
            if (profile == null)
                return GenerateAiRecommendation(currentMoisture, 1, autoActive);

            double optimal = profile.OptimalMoisture;
            double diff = currentMoisture - optimal;
            string recommendation;
            double ecoScore;

            if (diff >= 10)
            {
                recommendation = $"{profile.Name} soil is well hydrated. Skip next irrigation.";
                ecoScore = 95;
            }
            else if (diff >= 0)
            {
                recommendation = $"{profile.Name} moisture is at optimal level ({optimal}%). Next irrigation: {profile.BestTimeToIrrigate}.";
                ecoScore = 85;
            }
            else if (diff >= -15)
            {
                recommendation = $"{profile.Name} needs water soon. Irrigate {profile.BestTimeToIrrigate} ({profile.WeeklyFrequency}x/week recommended).";
                ecoScore = 60;
            }
            else
            {
                recommendation = $"{profile.Name} is critically dry! Immediate irrigation required. Target: {optimal}% moisture.";
                ecoScore = 25;
            }

            if (autoActive) ecoScore = Math.Min(ecoScore + 10, 100);

            return new AiRecommendationDto
            {
                Recommendation = recommendation,
                BestTimeToIrrigate = profile.BestTimeToIrrigate,
                EcoScore = ecoScore,
                WaterSavingsPercent = autoActive ? Math.Round(ecoScore * 0.3, 1) : 0
            };
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalMinutes < 1)
                return $"{duration.Seconds} sec";
            return $"{(int)duration.TotalMinutes} min {duration.Seconds} sec";
        }
    }

    // DTO داخلي لاستقبال قراءة الرطوبة من الهاردوير
    public class SoilReadingDto
    {
        public double Value { get; set; }
    }

    // DTO لإنشاء Irrigation Zone جديدة
    public class CreateIrrigationZoneDto
    {
        public string? Name { get; set; } = string.Empty;
        public int ZoneId { get; set; }
        public string? UserId { get; set; }
        public double MoistureThreshold { get; set; } = 40.0;
        public SmartVillageAPI.Model.PlantType PlantType { get; set; } = SmartVillageAPI.Model.PlantType.None;
    }

    // DTO لتخزين حالة Auto/Manual
    public class SystemModeDto
    {
        public bool IsAuto { get; set; }
    }
}
