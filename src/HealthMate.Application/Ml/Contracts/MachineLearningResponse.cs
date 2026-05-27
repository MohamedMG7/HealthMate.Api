namespace HealthMate.Application.Ml.Contracts;

public class MachineLearningResponse
{
    public bool Animea { get; set; }
    public double? Confidence { get; set; }
    public string? ModelVersion { get; set; }
}
