namespace HealthMate.Infrastructure.DTO.MachineLearningDto{
    public class AnimeaMLDto{
        public int patientId { get; set; }
        public decimal Hemoglobin { get; set; }
        public decimal RedBloodCells { get; set; }
        public decimal PackedCellVolume { get; set; }
        public decimal MeanCorpuscularHemoglobin { get; set; }
        public decimal MeanCorpuscularHemoglobinConcentration { get; set; }
    }
}