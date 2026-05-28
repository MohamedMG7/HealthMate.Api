using HealthMate.Domain.Aggregates.Condition;

namespace HealthMate.Infrastructure.Data.Models
{
	public class ChronicDisease
	{
        public Condition COndition { get; set; }
        public int ConditionId { get; set; }

        public Disease Disease { get; set; }
        public int DiseaseId { get; set; }

        //public Medicaiton Medication { get; set; } // add medication taking for this ongoing condition


    }
}
