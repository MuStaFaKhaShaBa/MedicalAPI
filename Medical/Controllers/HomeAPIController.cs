using Medical.Data.Entities;
using Medical.Models;
using Medical.Repositories;
using Medical.Specifications.ApplicationUser_;
using Medical.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Medical.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HomeAPIController : ControllerBase
    {
        private readonly ILogger<HomeAPIController> _logger;
        private readonly ApplicationUserRepo _userRepo;

        public HomeAPIController(ILogger<HomeAPIController> logger, ApplicationUserRepo repo)
        {
            _logger = logger;
            _userRepo = repo;
        }

        [HttpGet("index")]
        [Authorize]
        public async Task<ActionResult> Index()
        {
            try
            {
                if (User.IsInRole("Admin"))
                {
                    var users = await _userRepo.GetAllAsync(new ApplicationUserSpecifications(new()));
                    var user = users?.FirstOrDefault(u => u.UserName == User.Identity?.Name);

                    var adminModel = new HomeAdminsVM()
                    {
                        User = user,
                        TotalAdmins = users.Count(u => u.UserRole == UserRole.Admin),
                        TotalDoctors = users.Count(u => u.UserRole == UserRole.Doctor),
                        TotalPatients = users.Count(u => u.UserRole == UserRole.Patient),
                    };

                    return Ok(adminModel);
                }
                else if (User.IsInRole("Doctor"))
                {
                    return Ok("Doctor's Dashboard"); // Replace with actual content for doctors
                }
                else if (User.IsInRole("Patient"))
                {
                    var specs = new BaseGlobalSpecs<ApplicationUserNavigations, ApplicationUserSearch>()
                    {
                        Search = new() { UserName = User.Identity.Name },
                        Navigations = new() { EnableMedicalsReports = true }
                    };

                    var patient = await _userRepo.GetAsync(new(specs));

                    if (patient == null) return NotFound();

                    return Ok(patient);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in Index action.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("privacy")]
        public IActionResult Privacy()
        {
            return Ok("Privacy content here"); // Replace with actual privacy content
        }

        [HttpGet("error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var errorViewModel = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            return StatusCode(500, errorViewModel);
        }
    }
}
