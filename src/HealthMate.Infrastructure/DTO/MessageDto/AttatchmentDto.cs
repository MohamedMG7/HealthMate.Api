using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.DTO.MessageDto{
    public class AttachmentDto
    {
        public AttatchmentType AttatchmentType { get; set; }
        public string AttatchmentId { get; set; } = null!;
    }
}