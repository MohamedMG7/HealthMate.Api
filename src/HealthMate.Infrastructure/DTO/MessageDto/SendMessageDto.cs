namespace HealthMate.Infrastructure.DTO.MessageDto{
    public class SendMessageDto{
        public string ReceiverId { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string Body { get; set; } = null!;
        public List<AttachmentDto>? Attachments { get; set; } = new();
        
    }
}