namespace HealthMate.Application.Abstractions.Enums;

public enum ClinicalStatus
{
    Active = 0,
    Resolved = 1
}

public enum Recorder
{
    Patient = 0,
    HealthCareProvider = 1
}

public enum Severity
{
    Severe = 0,
    Moderate = 1,
    Mild = 2
}

public enum UserType
{
    Patient = 0,
    HealthCareProvider = 1,
    Admin = 2
}

public enum Gender
{
    Male = 0,
    Female = 1
}

public enum FeedBack_Category
{
    Technical_Issue = 0,
    Improvements = 1
}

public enum VerificationPurpose
{
    EmailConfirmation = 0,
    ForgotPassword = 1
}

public enum AttatchmentType
{
    MedicalImage = 1,
    Labtest = 2,
    Prescription = 3,
    Observation = 4,
    Medicine = 5
}

public enum AssessmentType
{
    PHQ9 = 1,
    GAD7 = 2
}
