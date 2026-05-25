using System.Diagnostics;
using HealthMate.Infrastructure.DTO.MachineLearningDto;
using HealthMate.Infrastructure.Repositories.ObservationRepos;
using System.Text.Json;

using HealthMate.Infrastructure.Repositories.PatientRepos;
using HealthMate.Application.Manager.UtilityManager;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace HealthMate.Application.Manager.MachineLearningManager{
    public class MachineLearningManager : IMachineLearningManager{
        
        private readonly IObservationRepo _observationRepo;
        
        
        public MachineLearningManager(IObservationRepo observationRepo)
        {
            _observationRepo = observationRepo;
            
        }
        private string GetPythonScriptPath(string scriptName)
        {
            // Construct the relative path to the Python script
            var baseDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")); // Go up to the 'src' directory
            var scriptPath = Path.Combine(baseDirectory, "HealthMate.Application", "MLModels", scriptName);
            return scriptPath;
        }

        public async Task<MachineLearningResponse> CheckDiabetes(int patientId){
            
            // get data from DB

            // calculate Pedigree => 
            
            return new MachineLearningResponse{Animea = false};
        }

        public async Task<MachineLearningResponse> CheckAnimea(int patientId){
            // Get recent CBC test data
            var cbcData = await _observationRepo.GetRecentCBCTestForML(patientId);

            // Serialize data to JSON
            var jsonData = JsonSerializer.Serialize(new {
                HB = cbcData.Hemoglobin,
                RBC = cbcData.RedBloodCells,
                PCV = cbcData.PackedCellVolume,
                MCH = cbcData.MeanCorpuscularHemoglobin,
                MCHC = cbcData.MeanCorpuscularHemoglobinConcentration
            });

            // Create temporary input file
            var inputFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(inputFile, jsonData);

            try {
                var scriptPath = GetPythonScriptPath("Animea.py"); 

                var processStartInfo = new ProcessStartInfo {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\" {inputFile}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // Capture standard error
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                using var process = Process.Start(processStartInfo);
                using var reader = process.StandardOutput;
                using var errorReader = process.StandardError; // Read standard error
                var result = await reader.ReadToEndAsync();
                var error = await errorReader.ReadToEndAsync(); // Capture any errors

                if (!string.IsNullOrEmpty(error))
                {
                    return new MachineLearningResponse {
                        Animea = false
                    };
                }

                // Parse prediction result and convert to boolean
                var prediction = result.Trim() == "0";

                return new MachineLearningResponse {
                    Animea = prediction
                };
            }
            catch (Exception ex)
            {
                return new MachineLearningResponse {
                    Animea = false,
                };
            }
        }
    }

    public class MachineLearningHelper{
        private readonly IPatientRepo _patientRepo;
        private readonly IUtilityManager _utilityManager;
        public MachineLearningHelper(IUtilityManager utilityManager, IPatientRepo patientRepo)
        {
            _patientRepo = patientRepo;
            _utilityManager = utilityManager;
        }
        public async Task<double> CalculateDiabetesPedigreeFunction(int countOfImmediateRelatives, int countOfSecondDegreeRelatives, int countOfThirdDegreeRelatives, int patientId){

            // equation used DPF = (1 * count immediate relatives + 0.5 * second degree relatives + third degree relatives) / Age
            // immediate relatives are people that share approximatly 50% of genes with the patient like(parents, siblings, sons, daughters)
            // second relatives are people that share approximatly 25% of genes with patient like(grandparents, aunts, uncles, half_siblings, nices, nephews, grand children)
            // others are third degree relatives like (cousins, great grandparents, great grandchildren)

            // get patient Age
            var age = await _patientRepo.GetPatientAge(patientId);
            var ageInYears = _utilityManager.CalculateAgeReturnYearsOnly(age);

            // calculate DPF
            var result = (1 * countOfImmediateRelatives + 0.5 * countOfSecondDegreeRelatives + 0.25 * countOfThirdDegreeRelatives) / ageInYears;
            return result;
        }

        public async Task<double> CalculateBMI(int patientId)
        {
            // Get only weight and height fields
            var patientData = await _patientRepo.GetAll()
                .Where(p => p.Patient_Id == patientId)
                .Select(p => new { p.Weight, p.Height })
                .FirstOrDefaultAsync();
            
            if (patientData == null || !patientData.Weight.HasValue || !patientData.Height.HasValue)
                return 0;

            // Convert height from cm to meters
            double heightInMeters = patientData.Height.Value / 100.0;
            
            // Calculate BMI: weight (kg) / (height (m))^2
            double bmi = patientData.Weight.Value / (heightInMeters * heightInMeters);
            
            return Math.Round(bmi, 2); // Round to 2 decimal places
        }
    }
}