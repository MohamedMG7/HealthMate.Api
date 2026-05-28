using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Encounters.Contracts;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Infrastructure.Repositories;
using System.Diagnostics.Metrics;

namespace HealthMate.Application.Manager.EncounterManager
{
	public class EncounterManager : IEncounterManager
	{
		private readonly IGenericRepository<Encounter> _encounterRepo;
        public EncounterManager(IGenericRepository<Encounter> encounterRepo)
        {
			_encounterRepo = encounterRepo;
        }

		[Obsolete("Use POST /api/Encounter/start; will be removed after Slice 5.")]
		public void AddEncounter(EncounterAddDto encounter)
		{
			var encounterEntity = Encounter.CreateLegacy(
				encounter.Patient_Id,
				encounter.HealthcareProvider_Id,
				encounter.StartDate,
				encounter.EndDate,
				encounter.Location,
				encounter.Reason_To_Visit,
				encounter.Treatment_Plan,
				encounter.Note);

			_encounterRepo.Add(encounterEntity);
			_encounterRepo.Save();
		}

		public void DeleteEncounter(int encounterId)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<EncounterReadDto> GetAllEncounters()
		{
			var encounters = _encounterRepo.GetAll().ToList();

			var encounterList = encounters.Select(x => new EncounterReadDto
			{
				Encounter_Id = x.Id,
				Patient_Id = x.PatientId,
				Encounter_Fhir_Id = x.FhirId,
				HealthcareProvider_Id = x.HealthCareProviderId,
				isDeleted = x.IsDeleted,
				Reason_To_Visit = x.ReasonToVisit.Value,
				StartDate = x.StartDate,
				EndDate = x.EndDate,
				Location = x.Location,
				Treatment_Plan = x.TreatmentPlan,
				Note = x.Note
			});

			return encounterList;
		}

		public EncounterReadDto GetEncounter(int encounterId)
		{
			var encounter = _encounterRepo.GetById(encounterId);

			if (encounter == null)
			{
				return null;
			}

			EncounterReadDto encounterRead = new EncounterReadDto
			{
				Encounter_Id = encounter.Id,
				Patient_Id = encounter.PatientId,
				Encounter_Fhir_Id = encounter.FhirId,
				HealthcareProvider_Id = encounter.HealthCareProviderId,
				isDeleted = encounter.IsDeleted,
				Reason_To_Visit = encounter.ReasonToVisit.Value,
				StartDate = encounter.StartDate,
				EndDate = encounter.EndDate,
				Location = encounter.Location,
				Treatment_Plan = encounter.TreatmentPlan,
				Note = encounter.Note
			};
			return encounterRead;
		}
	}
}
