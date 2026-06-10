using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartVillageAPI.Model;
using SmartVillageAPI.Services;
using System.Security.Claims;

namespace SmartVillageAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ParkingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMqttService _mqttService;

        public ParkingController(AppDbContext context, IMqttService mqttService)
        {
            _context = context;
            _mqttService = mqttService;
        }

        // 1. عرض بيانات الداشبورد (رصيد، عدد الأماكن الفاضية)
        [HttpGet("Dashboard/{zoneId}")]
        public async Task<IActionResult> GetDashboard(int zoneId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);

            // نجيب الـ 3 ركنات بتوعك
            var spots = await _context.Devices.Where(d => d.ZoneId == zoneId).ToListAsync();

            int totalSpots = spots.Count;
            int available = spots.Count(s => s.CurrentState == "0");
            int occupied = spots.Count(s => s.CurrentState == "1");
            int reserved = spots.Count(s => s.CurrentState == "Reserved");

            return Ok(new
            {
                WalletBalance = user.WalletBalance,
                TotalSpaces = totalSpots, // هترجع 3
                Available = available,
                Occupied = occupied,
                Reserved = reserved
            });
        }

        // 2. حجز ركنة (New Reservation)
        [HttpPost("Reserve")]
        public async Task<IActionResult> ReserveSpot([FromBody] ReservationDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);

            // مثلاً الحجز بخصم 10 دولار
            if (user.WalletBalance < 10)
                return BadRequest(new { Message = "Insufficient wallet balance. Please add funds." });

            var spot = await _context.Devices.FindAsync(request.DeviceId);
            if (spot == null || spot.CurrentState != "Available")
                return BadRequest(new { Message = "Spot is not available." });

            // خصم الفلوس وتغيير حالة الركنة
            user.WalletBalance -= 10;
            spot.CurrentState = "Reserved";

            var reservation = new ParkingReservation
            {
                UserId = userId,
                DeviceId = request.DeviceId,
                PlateNumber = request.PlateNumber,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };

            _context.ParkingReservations.Add(reservation);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Spot reserved successfully!", NewBalance = user.WalletBalance });
        }

        // 3. عرض حجوزاتي (My Bookings)
        [HttpGet("MyBookings")]
        public async Task<IActionResult> GetMyBookings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookings = await _context.ParkingReservations
                .Include(p => p.Device)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.StartTime)
                .Select(p => new {
                    BookingId = p.Id,
                    SpotName = p.Device.Name,
                    PlateNumber = p.PlateNumber,
                    StartTime = p.StartTime,
                    EndTime = p.EndTime,
                    Status = p.Status
                })
                .ToListAsync();

            return Ok(bookings);
        }

        // 4. زرار (Find My Car) - تشغيل الإضاءة أو الإنذار
        [HttpPost("FindMyCar/{deviceId}")]
        public async Task<IActionResult> FindMyCar(int deviceId)
        {
            // بتبعت أمر للـ ESP32 عشان ينور اللمبة اللي فوق الركنة المحددة
            string topic = $"FadiSmartVillage2026/parking/find/{deviceId}";
            await _mqttService.PublishAsync(topic, "FLASH_LIGHT_AND_SOUND");

            return Ok(new { Message = "Look for the flashing lights!" });
        }

        // 5. شحن المحفظة (Add Funds)
        [HttpPost("Wallet/AddFunds")]
        public async Task<IActionResult> AddFunds([FromBody] double amount)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);

            user.WalletBalance += amount;
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Successfully added ${amount}", NewBalance = user.WalletBalance });
        }
    }

    public class ReservationDto
    {
        public int DeviceId { get; set; }
        public string? PlateNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
