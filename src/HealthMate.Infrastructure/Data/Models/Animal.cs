using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Infrastructure.Enums;

namespace HealthMate.Infrastructure.Data.Models
{
	public class Animal
	{
        public int Animal_Id { get; set; }
        public string Animal_Fhir_Id { get; set; } = null!;
        public DateOnly BirthDate { get; set; }
        public string Name { get; set; } = null!;
        public Gender Gender { get; set; }
        public string Species { get; set; } = null!;
        public string Breed { get; set; } = null!;

        //connect with patient (owner)
        public Patient Patient { get; set; } = null!;
        public int Owner_Id { get; set; }

    }
}
