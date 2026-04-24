using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SmartVillageAPI.DTOs;
using System.Security.Claims;

namespace SmartVillageAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        // دالة لإرسال رسالة فورية (Real-time)
        public async Task SendMessage(SendMessageDto messageDto)
        {
            var senderId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // نرسل الرسالة للمستلم فقط
            await Clients.User(messageDto.ReceiverId).SendAsync("ReceiveMessage", new ChatMessageDto
            {
                SenderId = senderId,
                ReceiverId = messageDto.ReceiverId,
                Message = messageDto.Message,
                SentAt = DateTime.UtcNow,
                IsRead = false
            });
        }

        // دالة لإخطار الطرف الآخر أن الرسالة اتقرأت
        public async Task MarkAsRead(string senderId)
        {
            var currentUserId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            await Clients.User(senderId).SendAsync("MessagesRead", currentUserId);
        }
    }
}
