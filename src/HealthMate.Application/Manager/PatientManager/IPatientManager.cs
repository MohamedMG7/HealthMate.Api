using HealthMate.Infrastructure.DTO.PatientDto.AnimalPatientDtos;
using HealthMate.Infrastructure.DTO.PatientDto.HumanPatientDtos;

namespace HealthMate.Application.Manager.PatientManager
{
    public interface IPatientManager
	{
		IEnumerable<HumanPatientReadDto> GetAllHumanPatients();
		IEnumerable<VerifiedHumanPatientReadDto> GetAllVerifiedHumanPatients();

		void AddHumanPatient(HumanPatientAddDto HumanPatient);
		void AddAnimalPatient(AnimalPatientAddDto AnimalPatient);

		Task<patientDashboardDto> GetPatientDashboardData(int patientId, int periodInDays);
		Task<HumanPatientMobileDashboard> GetMobilePatientDashboardDataAsync(int patientId);
	}
}
