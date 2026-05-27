using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Application.Prescriptions.Contracts.Medicines;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Repositories{
    public class MedicineRepo : IMedicineRepo{
        private readonly HealthMateContext _context;
        public MedicineRepo(HealthMateContext context)
        {
            _context = context;
        }

        public async Task<List<MedicineNameAndIdDto>> getMedicineNameAndId(){
            var result = await _context.Medicines
                .Select(m => new MedicineNameAndIdDto
                {
                    Id = m.Id,
                    Name = m.Name
                })
                .ToListAsync();

            return result;
        }
    }
}
