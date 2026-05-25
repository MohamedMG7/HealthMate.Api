namespace HealthMate.Infrastructure.DTO.MessageDto{

    public class MessageDetailsDto{
         public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public List<AttachmentDto> Attachments { get; set; } = new();
    }
   
}