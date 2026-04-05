using Microsoft.AspNetCore.Mvc;
using SkillMatrix.Data.Services;

namespace SkillMatrix.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsultantsController : ControllerBase
    {
        private readonly AdoNetService _adoNetService;

        public ConsultantsController(AdoNetService adoNetService)
        {
            _adoNetService = adoNetService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 9)
        {
            var result = await _adoNetService.GetConsultantsAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var consultant = await _adoNetService.GetConsultantDetailsAsync(id);
            if (consultant == null)
            {
                return NotFound();
            }

            return Ok(consultant);
        }
    }
}