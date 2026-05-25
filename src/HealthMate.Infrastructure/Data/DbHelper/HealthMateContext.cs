using HealthMate.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Reflection.Emit;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace HealthMate.Infrastructure.Data.DbHelper
{
	public class HealthMateContext : IdentityDbContext<ApplicationUser>
	{
        public HealthMateContext(DbContextOptions<HealthMateContext> options) : base(options) { }


		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);




			#region Table Configuration

			// Table UserDiseaseExperience
			builder.Entity<UserDiseaseExperience>().HasKey(sc => new { sc.ApplicationUserId, sc.DiseaseId }); // composite pk userId and DiseaseId
			builder.Entity<UserDiseaseExperience>().HasOne(sc => sc.Disease).WithOne().HasForeignKey<UserDiseaseExperience>(sc => sc.DiseaseId).OnDelete(DeleteBehavior.NoAction); // every experince include only one disease
			builder.Entity<UserDiseaseExperience>().Property(sc => sc.Experince).IsRequired(true);

			// Table Patient
			builder.Entity<Patient>().HasKey(sc => sc.Patient_Id); //set Table PK for internal Database quiries 
			builder.Entity<Patient>().Property(sc => sc.Patient_Fhir_Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()::text"); //create GUID for Fhir Resoruce
            builder.Entity<Patient>().Property(sc => sc.LastUpdated).HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
            builder.Entity<Patient>().Property(sc => sc.RowVersion).HasConversion<long>().HasColumnType("bigint").HasDefaultValue(1L).IsConcurrencyToken();
            builder.Entity<Patient>().Property(sc => sc.IsDeleted).HasDefaultValue(false);
            builder.Entity<Patient>().Property(sc => sc.DeletedAt).HasColumnType("timestamp with time zone");
            builder.Entity<Patient>().HasIndex(sc => sc.Patient_Fhir_Id).HasDatabaseName("IX_Patients_Patient_Fhir_Id_Active").HasFilter("\"IsDeleted\" = false");

	
			builder.Entity<Patient>().HasMany(sc => sc.Conditions).WithOne(c => c.Patient).HasForeignKey(c => c.PaientId).OnDelete(DeleteBehavior.NoAction); // one patient has many Conditions
			builder.Entity<Patient>().HasMany(sc => sc.Observations).WithOne(c => c.Patient).HasForeignKey(sc => sc.PatientId).OnDelete(DeleteBehavior.NoAction); // one Patient has many Observations
			builder.Entity<Patient>().HasMany(sc => sc.Encounters).WithOne(c => c.Patient).HasForeignKey(sc => sc.PatientId).OnDelete(DeleteBehavior.NoAction); // one Patient has many Encounters
			builder.Entity<Patient>().HasOne(sc => sc.ApplicationUser).WithOne().HasForeignKey<Patient>(sc => sc.ApplicationUserId).IsRequired(false).OnDelete(DeleteBehavior.NoAction); // every patient should have one account
			builder.Entity<Patient>().HasMany(sc => sc.Animals).WithOne(c => c.Patient).HasForeignKey(a => a.Owner_Id).IsRequired(true).OnDelete(DeleteBehavior.NoAction); // every Patient Can Have Multiple Animals

			// Table ApplicationUser
			builder.Entity<ApplicationUser>().HasKey(sc => sc.Id);
			builder.Entity<ApplicationUser>().HasMany(sc => sc.UserFeedbacks).WithOne(c => c.ApplicationUser).HasForeignKey(sc => sc.ApplicationUser_Id).IsRequired(true).OnDelete(DeleteBehavior.NoAction);// every Application User can write more than one Feedback
			builder.Entity<ApplicationUser>().HasMany(sc => sc.UserDiseaseExperiences).WithOne(c => c.ApplicationUser).HasForeignKey(sc => sc.ApplicationUserId).IsRequired(true).OnDelete(DeleteBehavior.NoAction); // every application user can add many experinces only one for one disease (handle this in line 26) composite pk makes it unique


			// Table Admin
			builder.Entity<Admin>().HasKey(sc => sc.Admin_Id); // set Table PK 
			builder.Entity<Admin>().HasOne(sc => sc.ApplicationUser).WithOne().HasForeignKey<Admin>(sc => sc.ApplicationUserId).IsRequired(true).OnDelete(DeleteBehavior.NoAction); //every admin has one account

			// Table Condition
			builder.Entity<Condition>().HasKey(sc => sc.Condition_Id);
			builder.Entity<Condition>().Property(sc => sc.Condition_Fhir_Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()::text"); // create GUID for Fhir Resource
			builder.Entity<Condition>().HasOne(sc => sc.BodySite).WithMany(bs => bs.Conditions)
			.HasForeignKey(sc => sc.BodySiteId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);
			builder.Entity<Condition>().HasOne(sc => sc.Disease).WithMany(sc => sc.Conditions).HasForeignKey(sc => sc.Disease_Id).IsRequired(true).OnDelete(deleteBehavior: DeleteBehavior.NoAction); // one to many relationship a condition will only have one disease but a disease can be in many conditions

			// Table Observation
			builder.Entity<Observation>().HasKey(sc => sc.Observation_Id);
			builder.Entity<Observation>().Property(sc => sc.Observation_Fhir_Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()::text"); // create GUID for Fhir Resource
			builder.Entity<Observation>().HasOne(sc => sc.BodySite).WithOne().HasForeignKey<Observation>(sc => sc.BodySiteId).IsRequired(false).OnDelete(deleteBehavior: DeleteBehavior.NoAction); // an observation can have one body site
			builder.Entity<Observation>().Property(p => p.ValueQuanitity).HasColumnType("numeric(18,3)");

			// Table Encounter
			builder.Entity<Encounter>().HasKey(sc => sc.Encounter_Id);
			builder.Entity<Encounter>().Property(sc => sc.Encounter_Fhir_Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()::text"); //create GUID for Fhir Resource
			builder.Entity<Encounter>().HasOne(sc => sc.HealthCareProvider).WithMany(sc => sc.Encounters).HasForeignKey(sc => sc.HealthCareProviderId).IsRequired(false).OnDelete(DeleteBehavior.NoAction);// a healthcare provider can do multiple encounter every encounter have just one healthcare provider
			builder.Entity<Encounter>().HasMany(sc => sc.Conditions).WithOne(sc => sc.Encounter).HasForeignKey(sc => sc.EncounterId).IsRequired(false).OnDelete(DeleteBehavior.NoAction); // encounter can have many conditions in it

			// Table HealthCareProvider
			builder.Entity<HealthCareProvider>().HasKey(sc => sc.HealthCareProvider_Id);
			builder.Entity<HealthCareProvider>().HasOne(sc => sc.ApplicationUser).WithOne().HasForeignKey<HealthCareProvider>(sc => sc.ApplicationUserId).IsRequired(true).OnDelete(DeleteBehavior.NoAction); // health care provider should have one account

			// Table BodySite
			builder.Entity<BodySite>().HasKey(sc => sc.BodySite_Id);

			// Table Disease
			builder.Entity<Disease>().HasKey(sc => sc.Disease_Id);

			//Table UserFeedback
			builder.Entity<UserFeedback>().HasKey(sc => sc.UserFeedback_Id);

			//Table Animal
			builder.Entity<Animal>().HasKey(sc => sc.Animal_Id);
			builder.Entity<Animal>().Property(sc => sc.Animal_Fhir_Id).IsRequired(true).HasDefaultValueSql("gen_random_uuid()::text");

			//Table VerificationCode
			builder.Entity<VerificationCode>().HasKey(sc => new { sc.ApplicationUser_Id, sc.VerificationCodeDigits });
			builder.Entity<VerificationCode>().HasOne(sc => sc.ApplicationUser).WithOne().HasForeignKey<VerificationCode>(sc => sc.ApplicationUser_Id);
			builder.Entity<VerificationCode>().Property(sc => sc.VerificationCodeDigits).IsRequired(true);

			//Table LabTest
			builder.Entity<LabTest>().HasKey(sc => sc.LabTestId);
			builder.Entity<LabTest>().HasOne(sc => sc.patient).WithMany(sc => sc.LabTests).HasForeignKey(sc => sc.patientId).IsRequired(true).OnDelete(DeleteBehavior.NoAction);

			//Table LabTestAttribute
			builder.Entity<LabTestAttribute>().HasKey(sc => sc.Id);
			builder.Entity<LabTestAttribute>().HasMany(sc => sc.LabTestResults).WithOne(sc => sc.LabTestAttribute).HasForeignKey(sc => sc.LabTestAttributeId).IsRequired(true).OnDelete(DeleteBehavior.NoAction);

			//Table LabTestResults
			builder.Entity<LabTestResult>().HasKey(sc => sc.Id);
			builder.Entity<LabTestResult>().HasOne(sc => sc.LabTest).WithMany(sc => sc.LabTestResults).HasForeignKey(sc => sc.LabTestId).IsRequired(true).OnDelete(DeleteBehavior.NoAction);
			
			//Table PatientMedicine
			builder.Entity<PatientMedicine>().HasKey(pm => pm.PatientMedicineId);
			
			builder.Entity<PatientMedicine>().HasOne(pm => pm.Patient).WithMany(p => p.PatientMedicines)
			.HasForeignKey(pm => pm.PatientId).OnDelete(DeleteBehavior.NoAction);
			builder.Entity<PatientMedicine>().HasOne(pm => pm.Medicine).WithMany(m => m.PatientMedicines)
			.HasForeignKey(pm => pm.MedicineId).OnDelete(DeleteBehavior.NoAction);

			//Table MedicalImages
			builder.Entity<MedicalImage>().HasKey(mi => mi.MedicalImageId);
			builder.Entity<MedicalImage>().HasOne(mi => mi.patient).WithMany(mi => mi.MedicalImages).HasForeignKey(mi => mi.paitentId).IsRequired(true).OnDelete(DeleteBehavior.NoAction);
			#endregion

			//Table Prescription
			builder.Entity<Prescription>()
			.HasOne(p => p.Encounter)
			.WithMany(e => e.Prescriptions)
			.HasForeignKey(p => p.EncounterId)
			.OnDelete(DeleteBehavior.NoAction);

			// Medicine Table
			builder.Entity<Medicine>().HasKey(m => m.Id);

			// Message Table
			builder.Entity<Message>().HasKey(m => m.Id);
			builder.Entity<Message>().HasMany(m => m.Attachments).WithOne(a => a.Message).HasForeignKey(a => a.MessageId).IsRequired(true).OnDelete(DeleteBehavior.NoAction);
			
			// Message Attachment Table
			builder.Entity<MessageAttachment>().HasKey(a => a.Id);
			builder.Entity<MessageAttachment>().HasOne(a => a.Message).WithMany(m => m.Attachments).HasForeignKey(a => a.MessageId).IsRequired(true).OnDelete(DeleteBehavior.NoAction);
			builder.Entity<MessageAttachment>().Property(a => a.AttatchmentType).IsRequired(true);
			builder.Entity<MessageAttachment>().Property(a => a.AttatchmentId).IsRequired(true);

			// Table MentalHealthAssessment
			builder.Entity<MentalHealthAssessment>().HasKey(m => m.Id);

			builder.Entity<MentalHealthAssessment>()
				.HasOne(m => m.Patient)
				.WithMany(p => p.MentalHealthAssessments)
				.HasForeignKey(m => m.patientId)
				.IsRequired(true)
				.OnDelete(DeleteBehavior.NoAction); 

			// Table PatientAllergy
			builder.Entity<PatientAllergy>().HasKey(a => a.Id);
			builder.Entity<PatientAllergy>()
				.HasOne(a => a.Patient)
				.WithMany(p => p.Allergies)
				.HasForeignKey(a => a.PatientId)
				.OnDelete(DeleteBehavior.NoAction);
			builder.Entity<PatientAllergy>().HasIndex(a => new { a.PatientId, a.IsActive });
			builder.Entity<PatientAllergy>().Property(a => a.Substance).IsRequired();

            // Table PatientHistory
            builder.Entity<PatientHistory>().HasKey(h => h.History_Id);
            builder.Entity<PatientHistory>().ToTable("PatientHistory");
            builder.Entity<PatientHistory>().Property(h => h.OperationType)
                .HasConversion(
                    v => v == PatientHistoryOperation.Create ? "C" : v == PatientHistoryOperation.Delete ? "D" : "U",
                    v => v == "C" ? PatientHistoryOperation.Create : v == "D" ? PatientHistoryOperation.Delete : PatientHistoryOperation.Update)
                .HasColumnType("character(1)");
            builder.Entity<PatientHistory>().Property(h => h.LastUpdated).HasColumnType("timestamp with time zone");
            builder.Entity<PatientHistory>().Property(h => h.RecordedAt).HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
            builder.Entity<PatientHistory>().Property(h => h.DeletedAt).HasColumnType("timestamp with time zone");
            builder.Entity<PatientHistory>().Property(h => h.RowVersion).HasConversion<long>().HasColumnType("bigint");
            builder.Entity<PatientHistory>().HasIndex(h => new { h.Patient_Fhir_Id, h.RowVersion });
            builder.Entity<PatientHistory>().HasIndex(h => new { h.Patient_Fhir_Id, h.RecordedAt });

			// Table SinaSession
			builder.Entity<SinaSession>().HasKey(s => s.Id);
			builder.Entity<SinaSession>()
				.HasOne(s => s.Patient)
				.WithMany()
				.HasForeignKey(s => s.PatientId)
				.OnDelete(DeleteBehavior.NoAction);
			builder.Entity<SinaSession>()
				.HasOne(s => s.HealthCareProvider)
				.WithMany()
				.HasForeignKey(s => s.HealthCareProviderId)
				.OnDelete(DeleteBehavior.NoAction);
			builder.Entity<SinaSession>().HasIndex(s => new { s.PatientId, s.HealthCareProviderId, s.Status });

			// Table SinaTurn
			builder.Entity<SinaTurn>().HasKey(t => t.Id);
			builder.Entity<SinaTurn>()
				.HasOne(t => t.Session)
				.WithMany(s => s.Turns)
				.HasForeignKey(t => t.SessionId)
				.OnDelete(DeleteBehavior.Cascade);
			builder.Entity<SinaTurn>().HasIndex(t => new { t.SessionId, t.OrdinalIndex }).IsUnique();
			builder.Entity<SinaTurn>().Property(t => t.Content).IsRequired();

			// Data seeding. ConcurrencyStamp must be a static literal because the
			// IdentityRole constructor otherwise assigns a new Guid every time the
			// model is built, which triggers EF Core's PendingModelChangesWarning
			// (the model would be non-deterministic across builds).
			builder.Entity<IdentityRole>().HasData(
				new IdentityRole { Id = "2", Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "static-admin-concurrency-stamp" },
				new IdentityRole { Id = "0", Name = "Patient", NormalizedName = "PATIENT", ConcurrencyStamp = "static-patient-concurrency-stamp" },
				new IdentityRole { Id = "1", Name = "HealthCareProvider", NormalizedName = "HEALTHCAREPROVIDER", ConcurrencyStamp = "static-healthcareprovider-concurrency-stamp" }
			);
			
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
		public DbSet<PatientMedicine> PatientMedicines { get; set; }
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
