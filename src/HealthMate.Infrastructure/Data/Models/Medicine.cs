

namespace HealthMate.Infrastructure.Data.Models
{
	public class Medicine
	{
        public int Id { get; set; }
        public string Description { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ActiveIngrediantes { get; set; } = null!;
        public string UsedToCure { get; set; } = null!;
        public ICollection<PatientMedicine> PatientMedicines { get; set; } = new HashSet<PatientMedicine>();


    }
}
