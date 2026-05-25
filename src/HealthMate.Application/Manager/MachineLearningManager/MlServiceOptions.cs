namespace HealthMate.Application.Manager.MachineLearningManager;

public class MlServiceOptions
{
    public const string SectionName = "MlService";

    public string BaseUrl { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
}
