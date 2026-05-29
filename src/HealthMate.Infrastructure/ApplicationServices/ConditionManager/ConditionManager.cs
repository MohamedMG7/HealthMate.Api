using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Abstractions.Enums;
using HealthMate.Domain.Aggregates.Condition;
using HealthMate.Infrastructure.Repositories.ConditionRepos;
using Microsoft.EntityFrameworkCore;

namespace HealthMate.Application.Manager.ConditionManager
{
	public class ConditionManager : IConditionManager
	{
		private readonly IConditionRepo _conditionRepo;
        public ConditionManager(IConditionRepo conditionRepo)
        {
            _conditionRepo = conditionRepo;
        }

		public void DeleteCondition(int conditionId)
		{
			var condition = _conditionRepo.GetById(conditionId);
			if (condition != null)
			{
				_conditionRepo.Delete(condition);
				_conditionRepo.Save();
			}
			else {
				throw new InvalidOperationException();
			}
			
		}

		public IEnumerable<ConditionReadDto> GetAllConditions()
		{
			var conditions = _conditionRepo.GetAll()
				.Select(x => new
				{
					x.PatientId,
					x.FhirId,
					x.ClinicalStatus,
					x.Severity,
					x.DateRecorded,
					x.Id,
					BodySiteId = EF.Property<int?>(x, "BodySiteId"),
					x.EncounterId,
					x.Note,
					Recorder = EF.Property<ConditionRecorder>(x, "Recorder")
				})
				.ToList();

			var conditionList = conditions.Select(x => new ConditionReadDto
			{
				PaientId = x.PatientId,
				Condition_Fhir_Id = x.FhirId,
				Recorder = (Recorder)(int)x.Recorder,
				ClinicalStatus = x.ClinicalStatus,
				Severity = x.Severity,
				DateRecorded = x.DateRecorded,
				Condition_Id = x.Id,
				BodySiteId = x.BodySiteId,
				EncounterId = x.EncounterId,
				Note = x.Note
			});

			return conditionList;
		}

		public ConditionReadDto GetCondition(int conditionId)
		{
			var condition = _conditionRepo.GetAll()
				.Where(x => x.Id == conditionId)
				.Select(x => new
				{
					x.PatientId,
					x.FhirId,
					x.ClinicalStatus,
					x.Severity,
					x.DateRecorded,
					x.Id,
					BodySiteId = EF.Property<int?>(x, "BodySiteId"),
					x.EncounterId,
					x.Note,
					Recorder = EF.Property<ConditionRecorder>(x, "Recorder")
				})
				.FirstOrDefault();

			if (condition == null) {
				return null;
			}

			ConditionReadDto conditionRead = new ConditionReadDto
			{
				PaientId = condition.PatientId,
				Condition_Fhir_Id = condition.FhirId,
				Recorder = (Recorder)(int)condition.Recorder,
				ClinicalStatus = condition.ClinicalStatus,
				Severity = condition.Severity,
				DateRecorded = condition.DateRecorded,
				Condition_Id = condition.Id,
				BodySiteId = condition.BodySiteId,
				EncounterId = condition.EncounterId,
				Note = condition.Note
			};
			return conditionRead;
		}

		public async Task<PatientDashboardConditionReadDto> getMostRecentSevereOngoingCondition(int patientId){
			return await _conditionRepo.getMostRecentSevereOngoingCondition(patientId);
		}
	}
}
