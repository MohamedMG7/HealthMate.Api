namespace HealthMate.Infrastructure.DTO.ObservationDto{
    public class bloodPressureDto{
        public int averageSystolic { get; set; }
        public int averageDiastolic { get; set; }
        //public List<int> systolicReadings { get; set; } = null!;
        //public List<int> diastolicReadings { get; set; } = null!;
        public bool IsNormal { get; set; }
        public bool IsUpdated { get; set; }
        public bool IsSufficient { get; set; }
    }

    public class bloodPressureValueAndDateDto{
        public decimal Value { get; set; }
        public string Date { get; set; } = null!;
    }

    public class SystolicAndDiastolic{
        public int Systolic { get; set; }
        public int Diastolic { get; set; }
    }
}