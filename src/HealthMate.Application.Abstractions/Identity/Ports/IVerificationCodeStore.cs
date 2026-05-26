namespace HealthMate.Application.Abstractions.Identity.Ports;

public interface IVerificationCodeStore
{
    void AddCode(string userId, string code, DateTime expirationDate, VerificationCodePurpose purpose);
    bool IsValid(string userId, string code, VerificationCodePurpose purpose);
    bool DeleteAllForUser(string userId);
    void Delete(string userId, string code);
    void Save();
}
