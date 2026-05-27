using HealthMate.Application.Prescriptions.Contracts.Medicines;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MedicineController : ControllerBase{

        private readonly IMedicineRepo _medicineRepo;
        public MedicineController(IMedicineRepo medicineRepo)
        {
            _medicineRepo = medicineRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMedicineNameAndId(){
            var result = await _medicineRepo.getMedicineNameAndId();
            return Ok(result);
        }
    }

}
