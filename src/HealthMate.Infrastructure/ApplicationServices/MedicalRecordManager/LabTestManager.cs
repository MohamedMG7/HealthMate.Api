using HealthMate.Application.LabTests.Contracts;
using HealthMate.Infrastructure.Data.Models;
using HealthMate.Infrastructure.Repositories;

namespace HealthMate.Application.Manager.LabTestManager{

    public class LabTestManager : ILabTestManager{
        private readonly IGenericRepository<LabTest> _LabTestRepo;
        private readonly IGenericRepository<LabTestResult> _LabTestResultRepo;
        private readonly ILabTestAttributeRepo _LabTestAttributeRepo;

        public LabTestManager(IGenericRepository<LabTest> LabTestRepo, IGenericRepository<LabTestResult> LabTestResultrepo, ILabTestAttributeRepo labTestAttributeRepo)
        {
            _LabTestRepo = LabTestRepo;
            _LabTestResultRepo = LabTestResultrepo;
            _LabTestAttributeRepo = labTestAttributeRepo;
        }


        public async Task addLabTestAsync(LabTestAddDto LabTestDto)
        {
            using var transaction = await _LabTestRepo.GetContext().Database.BeginTransactionAsync();
            try
            {
                // Create LabTest
                var labTest = new LabTest
                {
                    ImageUrl = "Default.png",
                    LabTestName = LabTestDto.LabTestName,
                    RecordedTime = DateTime.Now,
                    patientId = LabTestDto.patientId,
                    LabTestResults = new List<LabTestResult>()
                };

                // Add LabTest first
                _LabTestRepo.Add(labTest);
                await _LabTestRepo.GetContext().SaveChangesAsync();

				if (LabTestDto.LabTestResult == null)
				{
					throw new ArgumentException("LabTestResult collection is null");
				}

				if (!LabTestDto.LabTestResult.Any())
				{
					throw new ArgumentException("LabTestResult collection is empty");
				}

				// Add Results
				foreach (var result in LabTestDto.LabTestResult)
                {
                    var attributeId = await _LabTestAttributeRepo.GetIdByNameAsync(result.AttributeName);
                    
                    var labTestResult = new LabTestResult
                    {
                        LabTestId = labTest.LabTestId,
                        LabTestAttributeId = attributeId,
                        Value = result.Value
                    };
                    
                    _LabTestResultRepo.Add(labTestResult);
                }
                
                await _LabTestResultRepo.GetContext().SaveChangesAsync();
                
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

    }

}