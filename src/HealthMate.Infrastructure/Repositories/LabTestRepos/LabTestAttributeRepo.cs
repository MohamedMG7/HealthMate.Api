using HealthMate.Infrastructure.Data.DbHelper;
using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace HealthMate.Infrastructure.Repositories
{
    public class LabTestAttributeRepo : GenericRepository<LabTestAttribute>, ILabTestAttributeRepo
    {
        private readonly HealthMateContext _context;
        public LabTestAttributeRepo(HealthMateContext context) : base(context) {
			_context = context;
        }

        public async Task<int> GetIdByNameAsync(string AttributeName)
        {
            var attributeId = await _context.LabTestAttributes
                .Where(a => a.Name == AttributeName)
                .Select(a => a.Id)
                .FirstOrDefaultAsync();

            if (attributeId == 0)
            {
                throw new KeyNotFoundException($"Attribute '{AttributeName}' not found.");
            }

            return attributeId;
        }
    }
}