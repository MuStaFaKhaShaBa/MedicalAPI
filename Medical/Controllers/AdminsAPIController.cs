using Medical.Data.Entities;
using Medical.Models;
using Medical.Repositories;
using Medical.Specifications;
using Medical.Specifications.ApplicationUser_;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Medical.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminsAPIController : ControllerBase
    {
        private readonly ApplicationUserRepo _userRepo;
        private readonly ILogger<AdminsAPIController> _logger;

        public AdminsAPIController(ApplicationUserRepo userRepo, ILogger<AdminsAPIController> logger)
        {
            _userRepo = userRepo;
            _logger = logger;
        }

        [HttpGet("index")]
        public async Task<ActionResult<IEnumerable<ApplicationUser>>> Index([FromQuery] BaseGlobalSpecs<ApplicationUserNavigations,
            ApplicationUserSearch> specs)
        {
            try
            {
                specs.Search ??= new() { UserRole = UserRole.Admin };

                var specification = new ApplicationUserSpecifications(specs);

                var entities = await _userRepo.GetAllAsync(specification, specs.Pagination);
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching admins.");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
