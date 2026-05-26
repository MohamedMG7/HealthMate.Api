using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Infrastructure.Data.DbHelper;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Repositories.PatientAllergyRepos;

public class PatientAllergyRepo : GenericRepository<PatientAllergy>, IPatientAllergyRepo
{
    private readonly HealthMateContext context;

    public PatientAllergyRepo(HealthMateContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<PatientAllergy>> GetActiveByPatientAsync(int patientId, CancellationToken ct = default)
    {
        return await context.PatientAllergies
            .AsNoTracking()
            .Where(a => a.PatientId == patientId && a.IsActive)
            .OrderByDescending(a => a.RecordedAt)
            .ToArrayAsync(ct);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var allergy = await context.PatientAllergies.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (allergy is null)
        {
            return;
        }

        allergy.Deactivate();
        await context.SaveChangesAsync(ct);
    }
}
