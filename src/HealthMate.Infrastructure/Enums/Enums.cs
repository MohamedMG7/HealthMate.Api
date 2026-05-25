namespace HealthMate.Infrastructure.Enums
{
	
		//terminology.hl7.org/6.0.2/CodeSystem-condition-clinical.html code system
		public enum ClinicalStatus
		{
			Active = 0, // still ongoing
			Resolved = 1 //not any more a concern
		}

		public enum Recorder
		{
			Patient = 0,
			HealthCareProvider = 1
		}

		public enum Severity
		{
			Severe = 0,
			Moderate = 1,
			Mild = 2
		}

		public enum UserType
		{
			Patient = 0,
			HealthCareProvider = 1,
			Admin = 2,
		}

		public enum Gender
		{
			Male = 0,
			Female = 1
		}
		
		public enum ObservationCategory
		{
			Clinical = 0, // related to clinical findings
			Laboratory = 1, // related to laboratory
			Procedure = 2, //related to precedure findings
			VitalSigns = 3, // heart rate blood pressure
			Other = 4
		}

		public enum FeedBack_Category
		{
			Technical_Issue = 0,
			Improvements = 1
		}

		public enum VerificationPurpose{
			EmailConfirmation = 0,
			ForgotPassword = 1
		}

		public enum AttatchmentType{
			MedicalImage = 1,
			Labtest = 2,
			Prescription = 3,
			Observation = 4, 
			Medicine = 5

		}

		/// <summary>
		/// Enum for supported types of mental health assessments.
		/// </summary>
		public enum AssessmentType
		{
			PHQ9 = 1, // Patient Health Questionnaire-9 (Depression)
			GAD7 = 2  // Generalized Anxiety Disorder-7 (Anxiety)
		}

		

}
