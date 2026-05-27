namespace HealthMate.Application.Messaging.Contracts{
    public class SentMessageDto
    {   
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}