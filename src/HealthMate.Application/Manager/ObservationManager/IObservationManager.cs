using HealthMate.Application.Encounters.Contracts;
using HealthMate.Application.Observations.Contracts;
using HealthMate.Application.Patients.Contracts;

namespace HealthMate.Application.Manager.ObservationManager
{
	public interface IObservationManager
	{
		[Obsolete("Use POST /api/Encounter/{encounterId}/observations; will be removed after Slice 5.")]
		void AddObservation(ObservationAddDto observationDto);
		IEnumerable<ObservationReadDto> GetAllObservations();
		ObservationReadDto GetObservation(int observationId);
		void DeleteObservation(int ObservationId);
		Task<HeartRateDto> GetAverageHeartrateInXTime(int patientId, int periodInDays);
		Task<HemoglobinDto> GetHemoglobinDataInXTime(int patientId, int periodInDays);
		Task<GlucoseLevelDto> GetAverageGlucoseInXTime(int patientId, int periodInDays);
		Task<bloodPressureDto> GetAverageBloodPressureInXTime(int patientId, int periodInDays);
	
	}
}
