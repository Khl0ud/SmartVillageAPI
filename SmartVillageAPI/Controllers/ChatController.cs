using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartVillageAPI.DTOs;
using SmartVillageAPI.Model;
using System.Security.Claims;

namespace SmartVillageAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChatController(AppDbContext context)
        {
            _context = context;
        }

        // 1. إرسال رسالة (عشان تتخزن في الداتا بيز)
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto model)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = model.ReceiverId,
                Message = model.Message,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Message sent and saved", MessageId = chatMessage.Id });
        }

        // 2. الحصول على تاريخ الشات مع شخص معين
        [HttpGet("history/{otherUserId}")]
        public async Task<IActionResult> GetChatHistory(string otherUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var history = await _context.ChatMessages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.FullName,
                    ReceiverId = m.ReceiverId,
                    Message = m.Message,
                    SentAt = m.SentAt,
                    IsRead = m.IsRead,
                    IsMe = m.SenderId == currentUserId
                })
                .ToListAsync();

            return Ok(history);
        }

        // 3. تعليم الرسايل كأنها اتقرأت
        [HttpPost("mark-as-read/{senderId}")]
        public async Task<IActionResult> MarkAsRead(string senderId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var unreadMessages = await _context.ChatMessages
                .Where(m => m.SenderId == senderId && m.ReceiverId == currentUserId && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Messages marked as read" });
        }

        // 4. الحصول على قائمة الشاتات (مثل واجهة واتساب الرئيسية)
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // نجيب كل الرسايل اللي انا طرف فيها (مرسل او مستلم)
            var messages = await _context.ChatMessages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            // نجمعهم حسب الشخص التاني
            var conversations = messages
                .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Select(g => new
                {
                    UserId = g.Key,
                    FullName = g.First().SenderId == currentUserId ? g.First().Receiver.FullName : g.First().Sender.FullName,
                    LastMessage = g.First().Message,
                    LastMessageTime = g.First().SentAt,
                    UnreadCount = g.Count(m => m.ReceiverId == currentUserId && !m.IsRead)
                })
                .ToList();

            return Ok(conversations);
        }

        // 5. الحصول على قائمة كل المستخدمين (عشان تبدأ شات جديد)
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var users = await _context.Users
                .Where(u => u.Id != currentUserId)
                .Select(u => new
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email
                })
                .ToListAsync();

            return Ok(users);
        }

        // 6. جلب كل الرسائل اللي وصلتني (المستلمة)
        [HttpGet("received")]
        public async Task<IActionResult> GetReceivedMessages()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var messages = await _context.ChatMessages
                .Where(m => m.ReceiverId == currentUserId)
                .OrderByDescending(m => m.SentAt)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.FullName,
                    ReceiverId = m.ReceiverId,
                    Message = m.Message,
                    SentAt = m.SentAt,
                    IsRead = m.IsRead
                })
                .ToListAsync();

            return Ok(messages);
        }
    }
}
