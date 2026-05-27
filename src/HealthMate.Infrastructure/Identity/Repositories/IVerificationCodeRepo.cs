using HealthMate.Infrastructure.Data.Models;
using HealthMate.Application.Abstractions.Enums;
using HealthMate.Infrastructure.Repositories;

namespace HealthMate.Infrastructure.Identity.Repositories
{
	public interface IVerificationCodeRepo : IGenericRepository<VerificationCode>
	{
		bool GetByUserIdAndCode(string userId, string confirmationCode, VerificationPurpose expectedPurpose);
		bool CleanUserAndUnusedExpiredCodes(string userId);
	}
}
