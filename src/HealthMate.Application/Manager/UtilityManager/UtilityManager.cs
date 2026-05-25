using System.Globalization;

namespace HealthMate.Application.Manager.UtilityManager
{
	public class UtilityManager : IUtilityManager
	{
		public int CalculateAgeReturnYearsOnly(DateOnly birthDate)
		{
			var today = DateOnly.FromDateTime(DateTime.Today); 
			int age = today.Year - birthDate.Year;
			if (birthDate > today.AddYears(-age))
			{
				age--;
			}
			return age;
		}
		
		public List<int> ExctractSystolicAndDiastolic(decimal bloodPressure){
			// Convert the decimal to string
			string bloodPressureStr = bloodPressure.ToString("0.000", CultureInfo.InvariantCulture);
			
			// Split the string by the decimal point
			string[] parts = bloodPressureStr.Split('.');
			
			// Parse the parts to integers
			int systolic = int.Parse(parts[0]);
			int diastolic = int.Parse(parts[1]);
			
			// Return as list
			return new List<int> { systolic, diastolic };
		}
	}
}
