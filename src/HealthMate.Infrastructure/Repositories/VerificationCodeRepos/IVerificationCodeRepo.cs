using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.Repositories.VerificationCodeRepo
{
	public interface IVerificationCodeRepo : IGenericRepository<VerificationCode>
	{
		bool GetByUserIdAndCode(string userId, string confirmationCode, VerificationPurpose expectedPurpose);
		bool CleanUserAndUnusedExpiredCodes(string userId);
	}
}
