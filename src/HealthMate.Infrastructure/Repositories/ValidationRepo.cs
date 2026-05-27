using HealthMate.Application.Abstractions.Validation;
using HealthMate.Domain.Aggregates.Patient.ValueObjects;
using HealthMate.Infrastructure.Data.DbHelper;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Repositories{
    public class ValidationRepo : IValidationRepo{
        private readonly HealthMateContext _context;
        public ValidationRepo(HealthMateContext context)
        {
            _context = context;
        }

        public async Task<bool> CheckPatientId(int PatientId){
            var result = await _context.Patients.AnyAsync(p => p.Id == PatientId);
            return result;
        }

        public async Task<bool> CheckPatientNationalId(string PatientNationalId){
            var nationalId = NationalId.FromTrusted(PatientNationalId);
            var result = await _context.Patients.AnyAsync(p => p.NationalId == nationalId);
            return result;
        }

        public async Task<bool> CheckHealthcareProviderId(int HealthCareProviderId){
            var result = await _context.HealthCareProviders.AnyAsync(p => p.HealthCareProvider_Id == HealthCareProviderId);
            return result;
        }
    }
}
