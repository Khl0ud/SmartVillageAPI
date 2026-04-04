using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartVillageAPI.Dto;       // مسار الـ Dto
using SmartVillageAPI.DTOs;
using SmartVillageAPI.Hubs;      // مسار الـ Hub
using SmartVillageAPI.Model;     // مسار الـ Model
using SmartVillageAPI.Services;  // مسار الـ MQTT Service

namespace SmartVillageAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SensorsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<SmartVillageHub> _hubContext;
        private readonly IMqttService _mqttService; // 📡 ضفنا الـ MQTT للتحكم الأوتوماتيك

        public SensorsController(AppDbContext context,
                                 IHubContext<SmartVillageHub> hubContext,
                                 IMqttService mqttService)
        {
            _context = context;
            _hubContext = hubContext;
            _mqttService = mqttService;
        }

        // 1. استقبال القراءات من الهاردوير (ESP32 بيكلم دي)
        [HttpPost("Record")]
        [AllowAnonymous] // الهاردوير بيكلمها من غير توكن
        public async Task<IActionResult> RecordReading([FromBody] CreateSensorReadingDto dto)
        {
            // أ. التأكد إن الجهاز موجود وبنجيب الـ Zone بتاعته عشان الـ SignalR
            var device = await _context.Devices.Include(d => d.Zone).FirstOrDefaultAsync(d => d.Id == dto.DeviceId);
            if (device == null) return NotFound(new { Message = "Device not found" });

            // ب. حفظ القراءة في الداتا بيز
            var reading = new SensorReading
            {
                DeviceId = dto.DeviceId,
                Type = dto.Type, // Type here is Enum (ReadingType)
                Value = dto.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.SensorReadings.Add(reading);

            // ج. تحديث حالة الجهاز كـ "Online" وتسجيل آخر قيمة
            device.CurrentState = $"{dto.Value}";

            // ==========================================
            // 🤖 د. الذكاء الاصطناعي / الأتمتة (Automation)
            // ==========================================
            // مثال 1: لو حساس الغاز قرأ نسبة أعلى من 80، شغل المروحة فوراً!
            if (dto.Type == ReadingType.GasLevel && dto.Value > 80)
            {
                // 1. ابعت أمر عبر MQTT للشفاط أو البازر عشان يشتغل
                string topic = $"FadiSmartVillage2026/zone/{device.ZoneId}/alarm";
                await _mqttService.PublishAsync(topic, "ON");

                // 2. احفظ إشعار خطر في الداتا بيز
                var alert = new Notification
                {
                    Title = "⚠️ DANGER: Gas Leak!",
                    Message = $"High gas level detected in {device.Zone.Name}!",
                    UserId = device.Zone.UserId, // صاحب البيت
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(alert);
                // إرسال الإشعار لايف للموبايل في نفس اللحظة
                await _hubContext.Clients.Group($"Zone_{device.ZoneId}").SendAsync("ReceiveNotification", new
                {
                    Title = alert.Title,
                    Message = alert.Message,
                    Time = alert.CreatedAt
                });
            }

            // مثال 2: الري الذكي (لو التربة جافة)
            if (dto.Type == ReadingType.SoilMoisture)
            {
                var settings = await _context.AutomationSettings.FirstOrDefaultAsync(a => a.ZoneId == device.ZoneId);

                // لو وضع الأوتو شغال، وقراءة التربة الحالية (مثلاً 40) أقل من اللي اليوزر ظبطه (مثلاً 60)
                if (settings != null && settings.IsAutoIrrigationEnabled && dto.Value <= settings.TargetSoilMoisture)
                {
                    // 1. ابعت MQTT يشغل الموتور (Irrigation Pump)
                    // افترضنا إن اسم البامب في التوبيك هو pump
                    string topic = $"FadiSmartVillage2026/zone/{device.ZoneId}/pump";
                    await _mqttService.PublishAsync(topic, "ON");

                    // 2. ابعت إشعار لليوزر إن الري اشتغل أوتوماتيك
                    var alert = new Notification
                    {
                        Title = "🌱 Auto Irrigation Started",
                        Message = $"Soil moisture dropped to {dto.Value}%. Pump activated automatically.",
                        UserId = device.ZoneId.ToString(), // أو جيب الـ UserId من الـ Zone
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(alert);
                    await _context.SaveChangesAsync();
                }
            }

            await _context.SaveChangesAsync();

            // هـ. إرسال القراءة لايف للموبايل (للناس اللي جوه الـ Zone دي بس)
            await _hubContext.Clients.Group($"Zone_{device.ZoneId}").SendAsync("NewSensorReading", new
            {
                DeviceId = reading.DeviceId,
                Type = reading.Type.ToString(),
                Value = reading.Value,
                Time = reading.CreatedAt
            });

            return Ok(new { Message = "Reading saved and processed!" });
        }


        // 2. Endpoint للموبايل: عرض آخر قراءة لكل حساس في المنطقة
        [HttpGet("Latest/{zoneId}")]
        [Authorize] // 🔒 لازم يوزر مسجل دخول
        public async Task<IActionResult> GetLatestReadings(int zoneId)
        {
            // بنجيب أحدث قراءة لكل جهاز في الزون دي
            var latestReadings = await _context.SensorReadings
                .Include(r => r.Device)
                .Where(r => r.Device.ZoneId == zoneId)
                .GroupBy(r => r.DeviceId)
                .Select(g => g.OrderByDescending(r => r.CreatedAt).FirstOrDefault())
                .Select(r => new
                {
                    DeviceId = r.DeviceId,
                    DeviceName = r.Device.Name,
                    Type = r.Type.ToString(),
                    Value = r.Value,
                    Time = r.CreatedAt
                })
                .ToListAsync();

            return Ok(latestReadings);
        }


        // 3. Endpoint للموبايل: عرض السجل (تاريخ القراءات) عشان الرسم البياني (Charts)
        [HttpGet("History/{deviceId}")]
        [Authorize]
        public async Task<IActionResult> GetSensorHistory(int deviceId, [FromQuery] int hours = 24)
        {
            var timeLimit = DateTime.UtcNow.AddHours(-hours);

            var history = await _context.SensorReadings
                .Where(r => r.DeviceId == deviceId && r.CreatedAt >= timeLimit)
                .OrderBy(r => r.CreatedAt) // الترتيب من الأقدم للأحدث عشان الرسم البياني
                .Select(r => new
                {
                    Value = r.Value,
                    Time = r.CreatedAt
                })
                .ToListAsync();

            return Ok(history);
        }
    }
}