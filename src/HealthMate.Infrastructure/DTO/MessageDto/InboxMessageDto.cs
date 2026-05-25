using System.Text.Json.Serialization;

namespace HealthMate.Infrastructure.DTO.MessageDto{
    public class InboxMessageDto
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Date => CreatedAt.ToString("yyyy-MM-dd"); // read only 
        [JsonIgnore]
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}