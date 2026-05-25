using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace HealthMate.Infrastructure.Repositories.AdminRepos
{
	public class AdminRepo : IAdminRepo
	{
		private readonly HealthMateContext _context;
        public AdminRepo(HealthMateContext context)
        {
			_context = context;    
        }

        public Patient GetPatientWithApplicationUserData(int id)
		{
			return _context.Patients.Include(p => p.ApplicationUser).FirstOrDefault(p => p.Patient_Id == id);
		}
	}
}
