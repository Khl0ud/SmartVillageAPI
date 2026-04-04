using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartVillageAPI.Dto;       // مسار الـ Dto بناءً على الفولدر بتاعك
using SmartVillageAPI.Hubs;      // مسار الـ Hub
using SmartVillageAPI.Model;     // مسار الـ Model
using SmartVillageAPI.Services;  // مسار الـ Services اللي ضفناها
using System.Security.Claims;

namespace SmartVillageAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   // [Authorize] // 🔒 تأمين الكنترولر: لازم يوزر معاه Token عشان يقدر يتحكم
    public class DevicesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<SmartVillageHub> _hubContext;
        private readonly IMqttService _mqttService; // 📡 خدمة الـ MQTT

        // الـ Constructor
        public DevicesController(AppDbContext context,
                                 IHubContext<SmartVillageHub> hubContext,
                                 IMqttService mqttService)
        {
            _context = context;
            _hubContext = hubContext;
            _mqttService = mqttService;
        }

        // 1. عرض الأجهزة الخاصة بـ Zone معينة
        [HttpGet("ByZone/{zoneId}")]
        public async Task<IActionResult> GetByZone(int zoneId)
        {
            var devices = await _context.Devices
                .Where(d => d.ZoneId == zoneId)
                .Select(d => new
                {
                    Id = d.Id,
                    Name = d.Name,
                    Type = d.Type.ToString(),
                    CurrentState = d.CurrentState,
                    IsActive = d.IsActive
                }).ToListAsync();

            return Ok(devices);
        }

        // 2. التحكم في الأجهزة (ON / OFF / الزوايا)
        [HttpPost("Control/{id}")]
        public async Task<IActionResult> ControlDevice(int id, [FromBody] ControlDeviceDto request)
        {
            // أ. التأكد إن الجهاز موجود
            var device = await _context.Devices.Include(d => d.Zone).FirstOrDefaultAsync(d => d.Id == id);
            if (device == null) return NotFound(new { Message = "Device not found" });

            // ب. التأكد من صلاحية المستخدم
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
          
            // ج. تحديث الداتابيز
            string oldState = device.CurrentState;
            device.CurrentState = request.NewState;

            // د. تسجيل العملية في السجل (Activity Log)
            var log = new ActivityLog
            {
                ActionType = "Device Control",
                Details = $"User changed {device.Name} state from '{oldState}' to '{request.NewState}'",
                DeviceId = device.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);

            await _context.SaveChangesAsync();

            // هـ. 📡 إرسال الأمر للهاردوير عبر MQTT
            // הـ ESP32 المفروض يعمل Subscribe هنا
            string mqttTopic = $"FadiSmartVillage2026/device/{device.Id}";
            await _mqttService.PublishAsync(mqttTopic, request.NewState);

            // و. 📱 إرسال تحديث لحظي للموبايل عبر SignalR
            await _hubContext.Clients.Group($"Zone_{device.ZoneId}").SendAsync("DeviceStateChanged", new
            {
                DeviceId = id,
                NewState = request.NewState,
                UpdatedBy = userId
            });

            return Ok(new { Message = $"Device {device.Name} is now {request.NewState}" });
        }

        // 3. تسجيل الأجهزة أوتوماتيك من الهاردوير (Discovery)
        [HttpPost("AutoRegister")]
        [AllowAnonymous] // 🔓 عشان الـ ESP32 يقدر يسجل نفسه من غير Token
        public async Task<IActionResult> AutoRegister([FromBody] List<CreateDeviceDto> devices)
        {
            if (devices == null || devices.Count == 0)
                return BadRequest(new { Message = "No devices provided." });

            int addedCount = 0;

            foreach (var dev in devices)
            {
                var zoneExists = await _context.Zones.AnyAsync(z => z.Id == dev.ZoneId);
                if (!zoneExists) continue;

                var deviceExists = await _context.Devices
                    .AnyAsync(d => d.Name == dev.Name && d.ZoneId == dev.ZoneId);

                if (!deviceExists)
                {
                    _context.Devices.Add(new Device
                    {
                        Name = dev.Name,
                        Type = (DeviceType)dev.Type,
                        ZoneId = dev.ZoneId,
                        IsActive = true,
                        CurrentState = "OFF"
                    });
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
                return Ok(new { Message = $"{addedCount} new devices registered successfully!" });
            }

            return Ok(new { Message = "All devices are already registered." });
        }
        // 4. التحكم الجماعي في الأجهزة (All On / All Off / Lock All)
        [HttpPost("ControlBulk")]
        public async Task<IActionResult> ControlBulkDevices([FromBody] BulkControlDto request)
        {
            // أ. التأكد إن الموبايل باعت داتا صحيحة
            if (request.DeviceIds == null || !request.DeviceIds.Any())
                return BadRequest(new { Message = "No devices specified." });

            if (string.IsNullOrEmpty(request.NewState))
                return BadRequest(new { Message = "New state is required." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ب. نجيب كل الأجهزة المطلوبة من الداتابيز مرة واحدة
            var devices = await _context.Devices
                .Include(d => d.Zone)
                .Where(d => request.DeviceIds.Contains(d.Id))
                .ToListAsync();

            if (!devices.Any())
                return NotFound(new { Message = "No matching devices found." });

            var logs = new List<ActivityLog>();
            int updatedCount = 0;

            // ج. نعدي عليهم واحد واحد نغير حالته
            foreach (var device in devices)
            {
                // حماية: نتأكد إن اليوزر ده من حقه يتحكم في الجهاز ده
                if (!device.Zone.IsPublic && device.Zone.UserId != userId)
                    continue; // لو معندوش صلاحية، نتجاهل الجهاز ده ونكمل للي بعده

                string oldState = device.CurrentState;
                device.CurrentState = request.NewState;

                // تجهيز اللوج عشان نعرف مين قفل كل حاجة
                logs.Add(new ActivityLog
                {
                    ActionType = "Bulk Control",
                    Details = $"User changed {device.Name} state from '{oldState}' to '{request.NewState}' via Bulk Action",
                    DeviceId = device.Id,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });

                // د. 📡 إرسال أمر MQTT لكل جهاز
                // غيرنا الـ Topic للاسم المميز عشان محدش يتداخل معاك زي ما اتفقنا
                string mqttTopic = $"FadiSmartVillage2026/device/{device.Id}";
                await _mqttService.PublishAsync(mqttTopic, request.NewState);

                // هـ. 📱 إرسال تحديث SignalR للموبايل لكل جهاز
                await _hubContext.Clients.Group($"Zone_{device.ZoneId}").SendAsync("DeviceStateChanged", new
                {
                    DeviceId = device.Id,
                    NewState = request.NewState,
                    UpdatedBy = userId
                });

                updatedCount++;
            }

            // و. حفظ كل التعديلات واللوجز في الداتابيز بخبطة واحدة (Performance Optimization)
            if (logs.Any())
            {
                _context.ActivityLogs.AddRange(logs);
                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = $"Successfully updated {updatedCount} devices to '{request.NewState}'." });
        }
    }
}