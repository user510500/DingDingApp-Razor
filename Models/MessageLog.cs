namespace DingDingApp.Models
{
    public class MessageLog
    {
        public int Id { get; set; }
        public string MessageType { get; set; } = string.Empty; // "all" 或 "specific"
        public string? TargetUserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

