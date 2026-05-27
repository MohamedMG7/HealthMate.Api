using HealthMate.Application.Abstractions.Enums;

namespace HealthMate.Application.MentalHealth.Contracts;

public class CreateAssessmentDto
{
    public AssessmentType AssessmentType { get; set; }
    public string? EncodedAnswers { get; set; } = null!;
    public int Score { get; set; }
}

public class AssessmentResultDto
{
    public AssessmentType AssessmentType { get; set; }
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MentalStatusDto
{
    public string Mood { get; set; } = null!;
    public string Anxiety { get; set; } = null!;
}
