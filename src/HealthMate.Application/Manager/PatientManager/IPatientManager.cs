using HealthMate.Application.Patients.Contracts;

namespace HealthMate.Application.Manager.PatientManager
{
    public interface IPatientManager
	{
		void AddAnimalPatient(AnimalPatientAddDto AnimalPatient);

		Task<patientDashboardDto> GetPatientDashboardData(int patientId, int periodInDays);
		Task<HumanPatientMobileDashboard> GetMobilePatientDashboardDataAsync(int patientId);
	}
}
