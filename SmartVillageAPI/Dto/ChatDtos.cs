namespace SmartVillageAPI.DTOs
{
    public class SendMessageDto
    {
        public string ReceiverId { get; set; }
        public string Message { get; set; }
    }

    public class ChatMessageDto
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string ReceiverId { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsMe { get; set; } // عشان الموبايل يعرف يحط الرسالة يمين ولا شمال
    }
}
