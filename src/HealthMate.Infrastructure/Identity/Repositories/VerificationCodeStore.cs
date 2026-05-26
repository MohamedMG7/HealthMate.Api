using HealthMate.Application.Abstractions.Identity.Ports;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.Identity.Repositories;

public sealed class VerificationCodeStore : IVerificationCodeStore
{
    private readonly IVerificationCodeRepo _verificationCodes;

    public VerificationCodeStore(IVerificationCodeRepo verificationCodes)
    {
        _verificationCodes = verificationCodes;
    }

    public void AddCode(string userId, string code, DateTime expirationDate, VerificationCodePurpose purpose)
    {
        _verificationCodes.Add(new VerificationCode
        {
            ApplicationUser_Id = userId,
            VerificationCodeDigits = code,
            ExpirationDate = expirationDate,
            Purpose = MapPurpose(purpose)
        });
    }

    public bool IsValid(string userId, string code, VerificationCodePurpose purpose)
    {
        return _verificationCodes.GetByUserIdAndCode(userId, code, MapPurpose(purpose));
    }

    public bool DeleteAllForUser(string userId)
    {
        return _verificationCodes.CleanUserAndUnusedExpiredCodes(userId);
    }

    public void Delete(string userId, string code)
    {
        var verificationCode = _verificationCodes.GetAll()
            .FirstOrDefault(vc => vc.ApplicationUser_Id == userId && vc.VerificationCodeDigits == code);

        if (verificationCode is not null)
        {
            _verificationCodes.Delete(verificationCode);
        }
    }

    public void Save()
    {
        _verificationCodes.Save();
    }

    private static VerificationPurpose MapPurpose(VerificationCodePurpose purpose)
    {
        return purpose switch
        {
            VerificationCodePurpose.EmailConfirmation => VerificationPurpose.EmailConfirmation,
            VerificationCodePurpose.ForgotPassword => VerificationPurpose.ForgotPassword,
            _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null)
        };
    }
}
