using Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CompanyEmployees.Controllers
{
    [ApiVersion("2.0")]
    [Route("api/v{v:apiversion}/companies")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v2")]
    public class CompaniesV2Controller : ControllerBase
    {
        private readonly IRepositoryManager _repository;
        public CompaniesV2Controller(IRepositoryManager repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            var companies = await _repository.Company.GetAllCompaniesAsync(trackChanges:
           false);
            return Ok(companies);
        }
    }
}
