namespace HealthMate.Application.Observations.Contracts{
    public class DocumentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Path { get; set; } = null!;
        public string RecordedTime { get; set; } = null!;
    }
}
