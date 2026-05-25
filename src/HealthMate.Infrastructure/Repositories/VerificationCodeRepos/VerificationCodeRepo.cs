using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Enums;
using HealthMate.Infrastructure.Repositories.VerificationCodeRepo;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Repositories.VerificationCodeRepos
{
	public class VerificationCodeRepo : GenericRepository<VerificationCode>,IVerificationCodeRepo
	{
		private readonly HealthMateContext _context;
        public VerificationCodeRepo(HealthMateContext context) : base(context) {
			_context = context;
        }
		public bool GetByUserIdAndCode(string userId, string confirmationCode, VerificationPurpose expectedPurpose)
		{
			var code = _context.VerificationCodes.FirstOrDefault(vc =>
				vc.ApplicationUser_Id == userId &&
				vc.VerificationCodeDigits == confirmationCode &&
				vc.Purpose == expectedPurpose);

			// If no code found, just return false
			if (code == null)
			{
				return false;
			}

			// If code is expired, delete it and return false
			if (code.ExpirationDate < DateTime.UtcNow)
			{
				_context.VerificationCodes.Remove(code);
				_context.SaveChanges();
				return false;
			}

			return true; // Code is valid
		}

		public bool CleanUserAndUnusedExpiredCodes(string userId){
			var userCodes = _context.VerificationCodes
			.Where(vc => vc.ApplicationUser_Id == userId)
			.ToList();

			if (userCodes.Any())
			{
				_context.VerificationCodes.RemoveRange(userCodes);
				_context.SaveChanges();
			}
			return true;
		}

	}
}
