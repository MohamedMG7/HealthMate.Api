using HealthMate.Application.Observations.Contracts;
using Microsoft.EntityFrameworkCore;
using HealthMate.Infrastructure.Repositories.ObservationRepos;
using HealthMate.Application.Patients.Contracts;
using HealthMate.Application.Manager.UtilityManager;
using HealthMate.Infrastructure.Data.DbHelper;
using DomainGender = HealthMate.Domain.Common.Enums.Gender;

namespace HealthMate.Application.Manager.ObservationManager
{
	public class ObservationManager : IObservationManager
	{
		private readonly IObservationRepo _observationRepo;
		private readonly IUtilityManager _utilityManager;
        private readonly IDbContextFactory<HealthMateContext> _contextFactory;

        public ObservationManager(IUtilityManager utilityManager,IObservationRepo observationRepo, IDbContextFactory<HealthMateContext> contextFactory)
        {
            _observationRepo = observationRepo;
			_utilityManager = utilityManager;
            _contextFactory = contextFactory;
        }

		#region CRUD
		public void DeleteObservation(int ObservationId)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<ObservationReadDto> GetAllObservations()
		{
			var observations = _observationRepo.GetAll().ToList();
			var context = (HealthMateContext)_observationRepo.GetContext();
			var patientIds = observations
				.Select(o => o.PatientId)
				.Distinct()
				.ToArray();
			var patients = context.Patients
				.Where(patient => patientIds.Contains(patient.Id))
				.ToDictionary(patient => patient.Id);
			var patientUserIds = observations
				.Select(o => patients.TryGetValue(o.PatientId, out var patient) ? patient.ApplicationUserId : null)
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Distinct()
				.ToArray();
			var patientUsers = context.Users
				.Where(user => patientUserIds.Contains(user.Id))
				.ToDictionary(user => user.Id);
			var bodySiteIds = observations
				.Select(o => o.BodySiteId)
				.OfType<int>()
				.Distinct()
				.ToArray();
			var bodySites = context.BodySites
				.Where(bodySite => bodySiteIds.Contains(bodySite.BodySite_Id))
				.ToDictionary(bodySite => bodySite.BodySite_Id);

			var observationList = observations.Select(x => new ObservationReadDto
			{
				Observation_Id = x.Id,
				Observation_Fhir_Id = x.FhirId,
				Category = x.Category,
				Code = x.Code,
				CodeDisplayName= x.CodeDisplayName,
				DateOfObservation= x.DateOfObservation,
				Interpertation= x.Interpretation,
				ValueQuanitity= x.ValueQuantity,
				ValueUnit = x.ValueUnit,
				PatientName = patients.TryGetValue(x.PatientId, out var patient) && patient.ApplicationUserId is not null && patientUsers.TryGetValue(patient.ApplicationUserId, out var user) ? user.First_Name : "No Data",
				BodySiteName = x.BodySiteId.HasValue && bodySites.TryGetValue(x.BodySiteId.Value, out var bodySite) ? bodySite.DisplayName : "No Data",
			});

			return observationList;
		}

		public ObservationReadDto GetObservation(int observationId)
		{
			var observation = _observationRepo.GetById(observationId);

			if (observation == null)
			{
				return null;
			}

			ObservationReadDto observationRead = new ObservationReadDto
			{
				Observation_Id = observation.Id,
				Observation_Fhir_Id = observation.FhirId,
				Category = observation.Category,
				Code = observation.Code,
				CodeDisplayName = observation.CodeDisplayName,
				DateOfObservation = observation.DateOfObservation,
				Interpertation = observation.Interpretation,
				ValueQuanitity = observation.ValueQuantity,
				ValueUnit = observation.ValueUnit,
				PatientName = "No Data",
				BodySiteName = GetBodySiteName(observation.BodySiteId)
			};
			return observationRead;
		}

		private string? GetBodySiteName(int? bodySiteId)
		{
			if (!bodySiteId.HasValue)
			{
				return null;
			}

			var context = (HealthMateContext)_observationRepo.GetContext();
			return context.BodySites
				.Where(bodySite => bodySite.BodySite_Id == bodySiteId.Value)
				.Select(bodySite => bodySite.DisplayName)
				.FirstOrDefault();
		}

		#endregion
	
		#region Heart Rate
		public async Task<HeartRateDto> GetAverageHeartrateInXTime(int patientId, int periodInDays)
		{
			// Step 1: Get the heart rate readings
			var heartRates = await _observationRepo.GetHeartRateReadingsInXTime(patientId, periodInDays);

			// Step 2: Calculate the average
			var average = heartRates.Any() ? (int)heartRates.Average(h => h.HeartRateValue) : 0;

			// Step 3: Determine if the data is updated
			bool isUpdated = IsHeartRateDataUpdated(heartRates,7); // the last reading should be equal or closer than 7 days

			// Step 4: Determine if Data is Suff
			bool isSufficient = IsHeartRateDataSufficient(heartRates,10);

			// Step 5: Get Patient Age
			int patientAge = await GetPatientAgeByPatientId(patientId);
			
			// Step 5: Determine if HeartRate Data is normal or not
			bool isNormal = await IsHeartRateAverageNormal(patientAge ,average);
			return new HeartRateDto
			{
				average = average,
				IsNormal = isNormal,
				IsUpdated = isUpdated,
				IsSufficient = isSufficient
			};
		}

		private bool IsHeartRateDataSufficient(List<HeartRateValueAndDateDto> heartRates, int numberOfSufficientReadings)
		{
			if (!heartRates.Any())
				return false;

			return heartRates.Count >= numberOfSufficientReadings;
		}

		private bool IsHeartRateDataUpdated(List<HeartRateValueAndDateDto> heartRates, int daysFromTheLastReading)
		{
			if (!heartRates.Any())
				return false;

			var lastReadingDate = DateTime.Parse(heartRates.Last().Date);
			return (DateTime.UtcNow - lastReadingDate).TotalDays <= daysFromTheLastReading;
		}
		

		// i may add another funciton in other time to see if the data is consistant by checking the gap between the readings

		private async Task<bool> IsHeartRateAverageNormal(int patientAge, int heartRate_Average)
		{
			// Consider readings are in resting physiological mode
			// Consider there is no medical condition affecting these readings

			if (patientAge < 0)
			{
				throw new ArgumentException("Patient age cannot be negative");
			}

			
			if (patientAge <= 1) // Newborns to 1 year
			{
				return heartRate_Average >= 80 && heartRate_Average <= 160;
			}
			else if (patientAge <= 3) // 1-3 years
			{
				return heartRate_Average >= 80 && heartRate_Average <= 130;
			}
			else if (patientAge <= 5) // 3-5 years
			{
				return heartRate_Average >= 80 && heartRate_Average <= 120;
			}
			else if (patientAge <= 12) // 5-12 years
			{
				return heartRate_Average >= 75 && heartRate_Average <= 118;
			}
			else // Adults (>12 years)
			{
				return heartRate_Average >= 60 && heartRate_Average <= 100;
			}
		}
		#endregion
		
		#region Hemoglobin
		public async Task<HemoglobinDto> GetHemoglobinDataInXTime(int patientId, int periodInDays)
		{
			// Step 1: Get the hemoglobin readings
			var hemoglobinData = await _observationRepo.GetHemoglobinDataInXTime(patientId, periodInDays);

			// Step 2: get hemoglobin readings
			var hemoglobinReadings = hemoglobinData
				.Select(h => new HemoglobinValueAndDateDto
				{
					HemoglobinValue = h.HemoglobinValue,
					Date = h.Date 
				})
				.ToList();

			// Step 3: Get hemoglobin Average
			decimal hemoglobinAverage = hemoglobinReadings.Any() ? hemoglobinReadings.Average(h => h.HemoglobinValue) : 0;

			// Step 4: Determine if the data is updated
			bool isUpdated = IsHemoglobinDataUpdated(hemoglobinData, 7); // the last reading should be equal or closer than 7 days

			// Step 5: Determine if Data is Sufficient
			bool isSufficient = IsHemoglobinDataSufficient(hemoglobinData, 10);

			// Step 6: get patient Gender
			string patientGender = await GetPatientGenderByPatientId(patientId);

			// Step 7: get patient Age
			int patientAge = await GetPatientAgeByPatientId(patientId);

			// Step 8: Determine if Hemoglobin Data is normal or not
			bool isNormal = IsHemoglobinAverageNormal(hemoglobinAverage, patientAge, patientGender);

			return new HemoglobinDto
			{
				hemoglobinReadings = hemoglobinReadings,
				IsNormal = isNormal,
				IsUpdated = isUpdated
			};
		}

		private bool IsHemoglobinDataSufficient(List<HemoglobinValueAndDateDto> hemoglobinData, int numberOfSufficientReadings)
		{
			if (!hemoglobinData.Any())
				return false;

			return hemoglobinData.Count >= numberOfSufficientReadings;
		}

		private bool IsHemoglobinDataUpdated(List<HemoglobinValueAndDateDto> hemoglobinData, int daysFromTheLastReading)
		{
			if (!hemoglobinData.Any())
				return false;

			var lastReadingDate = DateTime.Parse(hemoglobinData.Last().Date);
			return (DateTime.UtcNow - lastReadingDate).TotalDays <= daysFromTheLastReading;
		}

		private bool IsHemoglobinAverageNormal(decimal hemoglobinAverage, int ageInYears, string Gender)
		{
			double hb = (double)hemoglobinAverage;

			if (ageInYears < 1)
				return hb >= 13.5 && hb <= 20.0;
			else if (ageInYears < 5)
				return hb >= 10.0 && hb <= 17.0;
			else if (ageInYears < 6)
				return hb >= 11.5 && hb <= 13.5;
			else if (ageInYears < 12)
				return hb >= 11.5 && hb <= 15.5;
			else if (ageInYears < 18)
				return Gender == "Male" ? hb >= 13.0 && hb <= 16.0 : hb >= 12.0 && hb <= 16.0;
			else
			{
				if (Gender == "Male")
					return hb >= 13.8 && hb <= 17.2;
				else if (Gender == "Female")
					return hb >= 12.1 && hb <= 15.1;
				else
					return false;
			}
		}
		#endregion

		#region Glucose Level
		public async Task<GlucoseLevelDto> GetAverageGlucoseInXTime(int patientId, int periodInDays)
		{
			// Step 1: Get the glucose readings
			var glucoseReadings = await _observationRepo.GetGlucoseReadingsInXTime(patientId, periodInDays);

			// Step 2: Calculate the average
			int average = glucoseReadings.Any() ? (int)Math.Floor(glucoseReadings.Average(g => g.GlucoseLevelValue)) : 0;

			// Step 3: Check if data is updated (last reading within 2 days)
			bool isUpdated = IsGlucoseDataUpdated(glucoseReadings, 2);

			// Step 4: Check if data is sufficient (at least 10 readings)
			bool isSufficient = IsGlucoseDataSufficient(glucoseReadings, 10);

			// Step 5: Get patient age
			int ageInYears = await GetPatientAgeByPatientId(patientId);

			// Step 6: Check if glucose average is within the normal range (fasting)
			bool isNormal = IsGlucoseAverageNormal(average, ageInYears);

			// Step 7: Return result
			return new GlucoseLevelDto
			{
				average = average,
				IsUpdated = isUpdated,
				IsSufficient = isSufficient,
				IsNormal = isNormal
			};
		}


		private bool IsGlucoseDataUpdated(List<GlucoseLevelValueAndDateDto> readings, int maxDays)
		{
			if (!readings.Any()) return false;

			var lastDate = DateTime.Parse(readings.Last().Date);
			return (DateTime.UtcNow - lastDate).TotalDays <= maxDays;
		}

		private bool IsGlucoseDataSufficient(List<GlucoseLevelValueAndDateDto> readings, int minCount)
		{
			return readings.Count >= minCount;
		}

		private bool IsGlucoseAverageNormal(decimal average, int ageInYears)
		{
			if (ageInYears < 1) 
				return average >= 50 && average <= 100;
			else 
				return average >= 70 && average <= 100;
		}

		
		#endregion

		#region Blood Pressure
		public async Task<bloodPressureDto> GetAverageBloodPressureInXTime(int patientId, int periodInDays)
		{
			// Step 1: Get the BloodPressure readings
			var bloodPressures = await _observationRepo.GetBloodPressureReadingsInXTime(patientId, periodInDays);

			var systolicList = new List<int>();
			var diastolicList = new List<int>();
			

			// Step 2: Use UtilityManager to separate systolic and diastolic values
			foreach (var bp in bloodPressures)
			{
				var values = _utilityManager.ExctractSystolicAndDiastolic(bp.Value);
				systolicList.Add(values[0]);
				diastolicList.Add(values[1]);
			}

			// Step 3: Calculate averages
			var averageSystolic = systolicList.Any() ? (int)systolicList.Average() : 0;
			var averageDiastolic = diastolicList.Any() ? (int)diastolicList.Average() : 0;

			// Step 4: Determine if the data is updated
			bool isUpdated = IsBloodPressureDataUpdated(bloodPressures, periodInDays);

			// Step 5: Determine if the data is sufficient
			bool isSufficient = IsBloodPressureDataSufficient(systolicList, diastolicList);

			// Step 6: get the patient Age
			var patientAge = await GetPatientAgeByPatientId(patientId);

			// Step 7: Determine if the blood pressure is normal
			bool isNormal = IsBloodPressureNormal(averageSystolic, averageDiastolic,patientAge);

			// Return the DTO
			return new bloodPressureDto
			{
				averageSystolic = averageSystolic,
				averageDiastolic = averageDiastolic,
				IsNormal = isNormal,
				IsUpdated = isUpdated,
				IsSufficient = isSufficient
			};
		}

		private bool IsBloodPressureDataUpdated(List<bloodPressureValueAndDateDto> pbDates, int periodInDays)
		{
			if (!pbDates.Any())
				return false;

			// Check if the last reading is within the last day
			var lastReadingDate = DateTime.UtcNow.AddDays(-periodInDays);
			return (DateTime.UtcNow - lastReadingDate).TotalDays <= 7;
		}

		private bool IsBloodPressureDataSufficient(List<int> systolicList, List<int> diastolicList)
		{
			// Check if there are at least 10 readings
			return systolicList.Count >= 10 && diastolicList.Count >= 10;
		}

		private bool IsBloodPressureNormal(int averageSystolic, int averageDiastolic, int age)
		{
			// Define normal blood pressure ranges based on age
			if (age < 1) // Infants (0-1 year)
			{
				return averageSystolic >= 70 && averageSystolic <= 100 &&
					averageDiastolic >= 50 && averageDiastolic <= 70;
			}
			else if (age <= 3) // Toddlers (1-3 years)
			{
				return averageSystolic >= 80 && averageSystolic <= 110 &&
					averageDiastolic >= 50 && averageDiastolic <= 80;
			}
			else if (age <= 5) // Preschoolers (3-5 years)
			{
				return averageSystolic >= 80 && averageSystolic <= 110 &&
					averageDiastolic >= 50 && averageDiastolic <= 80;
			}
			else if (age <= 12) // Children (5-12 years)
			{
				return averageSystolic >= 90 && averageSystolic <= 120 &&
					averageDiastolic >= 60 && averageDiastolic <= 80;
			}
			else if (age <= 18) // Adolescents (12-18 years)
			{
				return averageSystolic >= 100 && averageSystolic <= 120 &&
					averageDiastolic >= 60 && averageDiastolic <= 80;
			}
			else // Adults (>18 years)
			{
				return averageSystolic >= 90 && averageSystolic <= 120 &&
					averageDiastolic >= 60 && averageDiastolic <= 80;
			}
		}
		#endregion

        private async Task<int> GetPatientAgeByPatientId(int patientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var birthDate = await context.Patients
                .Where(patient => patient.Id == patientId)
                .Select(patient => (DateOnly?)patient.BirthDate)
                .FirstOrDefaultAsync();

            if (birthDate is null)
            {
                throw new Exception($"Patient with ID {patientId} not found or birth date not available");
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - birthDate.Value.Year;

            if (today.Month < birthDate.Value.Month ||
                (today.Month == birthDate.Value.Month && today.Day < birthDate.Value.Day))
            {
                age--;
            }

            return age;
        }

        private async Task<string> GetPatientGenderByPatientId(int patientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var genderValue = await context.Patients
                .Where(patient => patient.Id == patientId)
                .Select(patient => (int?)patient.Gender)
                .FirstOrDefaultAsync();

            if (genderValue is null)
            {
                throw new Exception($"Patient with ID {patientId} not found or gender not available");
            }

            return ((DomainGender)genderValue.Value).ToString();
        }

		
	}
}
