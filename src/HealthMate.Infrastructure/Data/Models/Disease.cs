
namespace HealthMate.Infrastructure.Data.Models
{
	public class Disease
	{
        public int Disease_Id { get; set; }
        public string Description { get; set; }
        public string Scientific_Name { get; set; }
        public string Display_Name { get; set; }
        public string Code { get; set; }
		public string ICD11_Code { get; set; } // this will store the ICD code for maintaining
	}
}
