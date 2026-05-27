namespace HealthMate.Application.Admin.Contracts{
    public class TrafficReportDto{
        // 1. Patient Overview
        public int TotalPatients { get; set; }
        public Dictionary<int, int> NewPatientsPerYear { get; set; } = new(); // Year -> Count
        public double AveragePatientsPerMonth { get; set; }
        public double RepeatVisitRate { get; set; } // avg. encounters per patient
        public Dictionary<string, int> PatientAgeGroups { get; set; } = new(); // e.g. "18-25": 12
        public Dictionary<string, int> GenderDistribution { get; set; } = new(); // "Male": 10, "Female": 5
        public Dictionary<string, int> LocationDistribution { get; set; } = new(); // "Cairo": 5, "Alexandria": 3

        // 2. Encounter Analytics
        public int TotalEncounters { get; set; }
        public Dictionary<int, int> EncountersPerYear { get; set; } = new(); // Year -> Count
        public Dictionary<string, int> EncountersPerMonth { get; set; } = new(); // "2025-01": 23
        public double AverageEncounterDurationInMinutes { get; set; }
        public List<string> MostCommonVisitReasons { get; set; } = new();
        public double AverageConditionsPerEncounter { get; set; }

        // 3. Condition & Disease Insights
        public Dictionary<string, int> SeverityDistribution { get; set; } = new(); // e.g., "Mild": 10
        public Dictionary<string, int> ClinicalStatusDistribution { get; set; } = new(); // "Active": 14, "Resolved": 5
        public Dictionary<string, int> MostAffectedBodySites { get; set; } = new(); // "Lungs": 8
    }
}