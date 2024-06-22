using Medical.Models;
using Medical.Repositories;
using Medical.Specifications;
using Medical.Specifications.ApplicationUser_;
using Medical.Specifications.PatientMedical_;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Medical.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsAPIController : ControllerBase
    {
        private readonly ApplicationUserRepo _userRepo;

        public PatientsAPIController(ApplicationUserRepo userRepo)
        {
            _userRepo = userRepo;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> Index([FromQuery] BaseGlobalSpecs<ApplicationUserNavigations, ApplicationUserSearch> specs)
        {
            specs.Search ??= new() { UserRole = UserRole.Patient };

            var specification = new ApplicationUserSpecifications(specs);

            var entities = await _userRepo.GetAllAsync(specification, specs.Pagination);
            return Ok(entities); // Return JSON response
        }

    }
}
