using HealthMate.Infrastructure.DTO.ConditionDto;
using HealthMate.Infrastructure.DTO.PatientDto.HumanPatientDtos;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Repositories;
using HealthMate.Infrastructure.Repositories.ConditionRepos;

namespace HealthMate.Application.Manager.ConditionManager
{
	public class ConditionManager : IConditionManager
	{
		private readonly IConditionRepo _conditionRepo;
        public ConditionManager(IConditionRepo conditionRepo)
        {
            _conditionRepo = conditionRepo;
        }

		public void AddCondition(ConditionAddDto condition)
		{
			var Condition = new Condition { 
				BodySiteId = condition.BodySite,
				Disease_Id = condition.DiseasesId,
				ClinicalStatus = condition.ClinicalStatus,
				DateRecorded = condition.DateRecorded,
				EncounterId = condition.Encounter_Id,
				Note = condition.Note,
				Recorder = condition.Recorder,
				PaientId = condition.Patient_Id,
				Severity = condition.Severity,
			};

			_conditionRepo.Add(Condition);
			_conditionRepo.Save();
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
			var conditions = _conditionRepo.GetAll().ToList();

			var conditionList = conditions.Select(x => new ConditionReadDto
			{
				PaientId = x.PaientId,
				Condition_Fhir_Id = x.Condition_Fhir_Id,
				Recorder = x.Recorder,
				ClinicalStatus = x.ClinicalStatus,
				Severity = x.Severity,
				DateRecorded = x.DateRecorded,
				Condition_Id = x.Condition_Id,
				BodySiteId = x.BodySiteId,
				EncounterId = x.EncounterId,
				Note = x.Note
			});

			return conditionList;
		}

		public ConditionReadDto GetCondition(int conditionId)
		{
			var condition = _conditionRepo.GetById(conditionId);

			if (condition == null) {
				return null;
			}

			ConditionReadDto conditionRead = new ConditionReadDto
			{
				PaientId = condition.PaientId,
				Condition_Fhir_Id = condition.Condition_Fhir_Id,
				Recorder = condition.Recorder,
				ClinicalStatus = condition.ClinicalStatus,
				Severity = condition.Severity,
				DateRecorded = condition.DateRecorded,
				Condition_Id = condition.Condition_Id,
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
