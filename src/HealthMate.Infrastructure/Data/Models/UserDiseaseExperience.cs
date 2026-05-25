
namespace HealthMate.Infrastructure.Data.Models
{
	public class UserDiseaseExperience
	{
        //weak enitity
        //composite pk userId and diseaseId
        //Link Application User 
        public ApplicationUser ApplicationUser { get; set; }
        public string ApplicationUserId { get; set; }


        //Link Disease
        public Disease Disease { get; set; }
        public int DiseaseId { get; set; }

        public string Experince { get; set; }
    }
}
