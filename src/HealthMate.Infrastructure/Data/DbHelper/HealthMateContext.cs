using HealthMate.Domain.Aggregates.Condition;
using HealthMate.Domain.Aggregates.Encounter;
using HealthMate.Domain.Aggregates.Observation;
using HealthMate.Domain.Aggregates.Patient;
using HealthMate.Domain.Aggregates.Prescription;
using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace HealthMate.Infrastructure.Data.DbHelper
{
	public class HealthMateContext : IdentityDbContext<ApplicationUser>
	{
        public HealthMateContext(DbContextOptions<HealthMateContext> options) : base(options) { }


		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);
			builder.ApplyConfigurationsFromAssembly(typeof(HealthMateContext).Assembly);
		}

		public DbSet<ApplicationUser> ApplicationUsers { get; set; }
		public DbSet<Patient> Patients { get; set; }
        public DbSet<PatientHistory> PatientHistories { get; set; }
		public DbSet<Admin> Admins { get; set; }
		public DbSet<HealthCareProvider> HealthCareProviders { get; set; }
		public DbSet<Condition> Conditions { get; set; }
		public DbSet<Observation> Observations { get; set; }
		public DbSet<Encounter> Encounters { get; set; }
		public DbSet<BodySite> BodySites { get; set; }
		public DbSet<UserFeedback> UserFeedbacks { get; set; }
		public DbSet<UserDiseaseExperience> UserDiseaseExperiences { get; set; }
		public DbSet<Disease> Diseases { get; set; }
		public DbSet<Animal> Animals { get; set; }
        public DbSet<VerificationCode> VerificationCodes { get; set; }
		public DbSet<LabTest> LabTests { get; set; }
		public DbSet<LabTestAttribute> LabTestAttributes { get; set; }
		public DbSet<LabTestResult> LabTestResults { get; set; }
		public DbSet<Prescription> Prescriptions { get; set; }
		public DbSet<PrescriptionMedicine> PrescriptionMedicines { get; set; }
		public DbSet<MedicalImage> MedicalImages { get; set; }
		public DbSet<Medicine> Medicines { get; set; }
		public DbSet<Message> Messages { get; set; }
		public DbSet<MessageAttachment> MessageAttachments { get; set; }
		public DbSet<MentalHealthAssessment> MentalHealthAssessments { get; set; }
		public DbSet<PatientAllergy> PatientAllergies { get; set; }
		public DbSet<SinaSession> SinaSessions { get; set; }
		public DbSet<SinaTurn> SinaTurns { get; set; }

    }
}
