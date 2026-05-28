namespace HealthMate.Infrastructure.Data.Models
{
	public class BodySite
	{
        public int BodySite_Id { get; set; }
		public string System { get; set; } = null!;
		public string Code { get; set; } = null!;
		public string DisplayName { get; set; } = null!;
	}
}
