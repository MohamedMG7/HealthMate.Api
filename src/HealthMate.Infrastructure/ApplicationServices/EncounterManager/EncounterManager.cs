using HealthMate.Application.Conditions.Contracts;
using HealthMate.Application.Encounters.Contracts;
using HealthMate.Infrastructure.Data.Models;
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

		public void AddEncounter(EncounterAddDto encounter)
		{
			var Encounter = new Encounter { 
				HealthCareProviderId = encounter.HealthcareProvider_Id,
				PatientId = encounter.Patient_Id,
				Location = encounter.Location,
				Reason_To_Visit = encounter.Reason_To_Visit,
				Note = encounter.Note,
				StartDate = encounter.StartDate,
				EndDate = encounter.EndDate,
				Treatment_Plan = encounter.Treatment_Plan,
			};

			_encounterRepo.Add(Encounter);
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
				Encounter_Id = x.Encounter_Id,
				Patient_Id = x.PatientId,
				Encounter_Fhir_Id = x.Encounter_Fhir_Id,
				HealthcareProvider_Id = x.HealthCareProviderId,
				isDeleted = x.isDeleted,
				Reason_To_Visit = x.Reason_To_Visit,
				StartDate = x.StartDate,
				EndDate = x.EndDate,
				Location = x.Location,
				Treatment_Plan = x.Treatment_Plan,
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
				Encounter_Id = encounter.Encounter_Id,
				Patient_Id = encounter.PatientId,
				Encounter_Fhir_Id = encounter.Encounter_Fhir_Id,
				HealthcareProvider_Id = encounter.HealthCareProviderId,
				isDeleted = encounter.isDeleted,
				Reason_To_Visit = encounter.Reason_To_Visit,
				StartDate = encounter.StartDate,
				EndDate = encounter.EndDate,
				Location = encounter.Location,
				Treatment_Plan = encounter.Treatment_Plan,
				Note = encounter.Note
			};
			return encounterRead;
		}
	}
}
