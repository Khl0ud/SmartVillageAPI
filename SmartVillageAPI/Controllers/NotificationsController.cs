using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartVillageAPI.Dto;
using SmartVillageAPI.Model;
using System.Security.Claims;

namespace SmartVillageAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔒 لازم الموبايل يبعت التوكن عشان نجيب إشعاراته
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. عرض كل الإشعارات لليوزر ده (الأحدث أولاً)
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt) // الجديد يظهر فوق
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // 2. الموبايل بيكلم دي لما اليوزر يدوس على إشعار عشان يخليه "مقروء"
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
                return NotFound(new { Message = "Notification not found" });

            // عشان السكيورتي: نتأكد إن الإشعار ده بتاع الراجل اللي فاتح
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (notification.UserId != userId)
                return Unauthorized(new { Message = "Not your notification" });

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Notification marked as read" });
        }

        // 3. زرار "تحديد الكل كمقروء" (عشان تفضي الشاشة يوم المناقشة لو مليانة داتا تست)
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (!unreadNotifications.Any())
                return Ok(new { Message = "No unread notifications" });

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = $"{unreadNotifications.Count} notifications marked as read." });
        }
    }
}