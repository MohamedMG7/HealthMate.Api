using HealthMate.Application.Abstractions.Enums;

namespace HealthMate.Application.Messaging.Contracts{
    public class AttachmentDto
    {
        public AttatchmentType AttatchmentType { get; set; }
        public string AttatchmentId { get; set; } = null!;
    }
}