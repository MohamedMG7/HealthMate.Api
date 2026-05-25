

namespace HealthMate.Infrastructure.Data.Models
{
	public class HealthCareProvider
	{
        public int HealthCareProvider_Id { get; set; }
        public DateTime DateOnJoin { get; set; }
        public string Specialization { get; set; } = null!;
        public string Degree { get; set; } = null!;

        public string Governorate { get; set; } = null!;
		public string City { get; set; } = null!;
		public string? Street { get; set; }

		// Link Application User for account handling one to one relationship
		public ApplicationUser ApplicationUser { get; set; } = null!;
        public string ApplicationUserId { get; set; } = null!;

        public bool IsActive { get; set; }


        //Link With Encounter
        public ICollection<Encounter> Encounters { get; set; } = new HashSet<Encounter>(); // a healthcare provider can do multiple encounter every encounter have just one healthcare provider
    }
}
