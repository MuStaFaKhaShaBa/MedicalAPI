using Medical.Data.Entities;
using Medical.Models;
using Medical.Repositories;
using Medical.Specifications;
using Medical.Specifications.ApplicationUser_;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Medical.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorsAPIController : ControllerBase
    {
        private readonly ApplicationUserRepo _userRepo;
        private readonly ILogger<DoctorsAPIController> _logger;

        public DoctorsAPIController(ApplicationUserRepo userRepo, ILogger<DoctorsAPIController> logger)
        {
            _userRepo = userRepo;
            _logger = logger;
        }

        [HttpGet("index")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ApplicationUser>>> Index([FromQuery] BaseGlobalSpecs<ApplicationUserNavigations, ApplicationUserSearch> specs)
        {
            try
            {
                specs.Search ??= new() { UserRole = UserRole.Doctor };

                var specification = new ApplicationUserSpecifications(specs);

                var entities = await _userRepo.GetAllAsync(specification, specs.Pagination);
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching doctors.");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
