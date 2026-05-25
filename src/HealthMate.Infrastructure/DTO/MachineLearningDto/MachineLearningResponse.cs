namespace HealthMate.Infrastructure.DTO.MachineLearningDto;

public class MachineLearningResponse
{
    public bool Animea { get; set; }
    public double? Confidence { get; set; }
    public string? ModelVersion { get; set; }
}
