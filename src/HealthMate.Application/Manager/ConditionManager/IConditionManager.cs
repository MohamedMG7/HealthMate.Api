using HealthMate.Application.Conditions.Contracts;

namespace HealthMate.Application.Manager.ConditionManager
{
	public interface IConditionManager
	{
		void AddCondition(ConditionAddDto condition);
		IEnumerable<ConditionReadDto> GetAllConditions();
		ConditionReadDto GetCondition(int conditionId);
		void DeleteCondition(int conditionId);
		Task<PatientDashboardConditionReadDto> getMostRecentSevereOngoingCondition(int patientId);
	}
}
