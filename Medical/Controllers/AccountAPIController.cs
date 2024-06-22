using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using Medical.Data.Entities;
using Medical.Models;
using Medical.Helper;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Azure.Core;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Medical.Repositories;
using Medical.Specifications.PatientMedical_;
using Medical.Specifications;

namespace Medical.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountAPIController : ControllerBase
    {
        private readonly PatientMedicalRepo _patientMedicalRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountAPIController> _logger;

        public AccountAPIController(PatientMedicalRepo patientMedicalRepo,UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, ILogger<AccountAPIController> logger)
        {
            _patientMedicalRepo = patientMedicalRepo;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginVM loginVM)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.FindByNameAsync(loginVM.UserName);
                    if (user != null)
                    {
                        var result = await _userManager.CheckPasswordAsync(user, loginVM.Password);
                        if (result)
                        {
                            await _signInManager.SignInAsync(user, false);
                            return Ok(new { Message = "Login successful", token = GenerateJwtToken(user), RedirectUrl = Url.Action("Index", "Home") });
                        }
                    }
                    return Unauthorized(new { Message = "Invalid username or password." });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during login.");
                    return StatusCode(500, new { Message = "Something went wrong. Please try again." });
                }
            }

            return BadRequest(ModelState);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { Message = "Logout successful" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody]CreateUserVM createUserVM)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userExists = await _userManager.FindByNameAsync(createUserVM.UserName);
                    if (userExists != null)
                    {
                        return Conflict(new { Message = "User already exists." });
                    }

                    var user = new ApplicationUser
                    {
                        UserName = createUserVM.UserName,
                        Email = createUserVM.Email,
                        NationalId = createUserVM.NationalId,
                        Name = createUserVM.Name,
                        PhoneNumber = createUserVM.PhoneNumber,
                        UserRole = createUserVM.Role,
                        Address = createUserVM.Address,
                        BirthDate = createUserVM.BirthDate,
                        Gender = createUserVM.Gender,
                        Specification = createUserVM.Specification,
                    };

                    var result = await _userManager.CreateAsync(user, createUserVM.Password);
                    if (result.Succeeded)
                    {
                        var path = _configuration["images:upload"];
                        if (createUserVM.Image != null)
                        {
                            user.ImagePath = DocumentSettings.SaveFile(user.Name, createUserVM.Image, path);
                        }

                        var qrCodeImageUrl = GenerateQrCodeImage(user.Id);
                        user.QR = DocumentSettings.SaveFile(user.Name, qrCodeImageUrl, path);
                        await _userManager.UpdateAsync(user);

                        await AssignUserRoleAsync(user, createUserVM.Role);
                        return Ok(new { Message = $"User {user.Name} added successfully" });
                    }
                    else
                    {
                        return BadRequest(result.Errors);
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database update exception.");
                    return Conflict(new { Message = "A user with this information already exists." });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during user creation.");
                    return StatusCode(500, new { Message = "Something went wrong. Please try again." });
                }
            }

            return BadRequest(ModelState);
        }

        [HttpPut("edit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, EditUserVM model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.FindByIdAsync(id.ToString());
                    if (user == null)
                    {
                        return NotFound();
                    }

                    UpdateUserProperties(user, model);

                    if (model.Image != null)
                    {
                        var path = _configuration["images:upload"];
                        if (!string.IsNullOrEmpty(user.ImagePath))
                        {
                            DocumentSettings.RemoveFile(Path.Combine(path, user.ImagePath));
                        }
                        user.ImagePath = DocumentSettings.SaveFile(user.Name, model.Image, path);
                    }

                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        var resetResult = await _userManager.ResetPasswordAsync(user, token, model.Password);

                        if (!resetResult.Succeeded)
                        {
                            return BadRequest(resetResult.Errors);
                        }
                    }

                    var updateResult = await _userManager.UpdateAsync(user);

                    if (updateResult.Succeeded)
                    {
                        return Ok(new { Message = $"User {user.Name} updated successfully" });
                    }
                    else
                    {
                        return BadRequest(updateResult.Errors);
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database update exception.");
                    return Conflict(new { Message = "A user with this information already exists." });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during user update.");
                    return StatusCode(500, new { Message = "Something went wrong. Please try again." });
                }
            }

            return BadRequest(ModelState);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound();
                }

                if (!string.IsNullOrEmpty(user.ImagePath))
                {
                    var path = _configuration["images:upload"];
                    DocumentSettings.RemoveFile(Path.Combine(path, user.ImagePath));
                }

                var deleteResult = await _userManager.DeleteAsync(user);
                if (deleteResult.Succeeded)
                {
                    return Ok(new { Message = "User deleted successfully" });
                }
                else
                {
                    return BadRequest(deleteResult.Errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during user deletion.");
                return StatusCode(500, new { Message = "Something went wrong. Please try again." });
            }
        }

        [HttpGet("profile/{id}")]
        public async Task<IActionResult> Profile(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            if (user.UserRole == UserRole.Patient)
            {
                var PatientModel = await PatientProfile(id);
                if(PatientModel == null)
                    return NotFound();
                return Ok(PatientModel);
            }

            var model = new ApplicationUserVM(user);
            return Ok(model);
        }

        private async Task<PatientUserVM?> PatientProfile(int patientId)
        {
            var patient = await _userManager.FindByIdAsync(patientId.ToString());

            if (patient == null)
                return null;

            var specs = new BaseGlobalSpecs<PatientMedicalNavigations, PatientMedicalSearch>
            {
                Search = new() { PatientId = patientId, Type = "Disease" }
            };
            var medicals = await _patientMedicalRepo.GetAllAsync(new(specs));

            var patientModel = new PatientUserVM(new(patient), medicals.ToList());

            return patientModel; // Return JSON response
        }

        [HttpGet("accessdenied")]
        public IActionResult AccessDenied()
        {
            return Forbid();
        }

        private async Task AssignUserRoleAsync(ApplicationUser user, UserRole role)
        {
            switch (role)
            {
                case UserRole.Admin:
                    await _userManager.AddToRoleAsync(user, "Admin");
                    break;
                case UserRole.Doctor:
                    await _userManager.AddToRoleAsync(user, "Doctor");
                    break;
                case UserRole.Patient:
                    await _userManager.AddToRoleAsync(user, "Patient");
                    break;
            }
        }

        private void UpdateUserProperties(ApplicationUser user, EditUserVM model)
        {
            user.Name = model.Name;
            user.Email = model.Email;
            user.NationalId = model.NationalId;
            user.PhoneNumber = model.PhoneNumber;
            user.UserName = model.UserName;
            user.Address = model.Address;
            user.BirthDate = model.BirthDate;
            user.Gender = model.Gender;
            user.Specification = model.Specification;
            user.UpdatedAt = DateTime.UtcNow;
        }

        private void AddErrorsToModelState(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        private byte[] GenerateQrCodeImage(int userId)
        {
            string url = Url.Action("Profile", "Account", new { id = userId }, Request.Scheme);
            using QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            using BitmapByteQRCode qrCode = new BitmapByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }
        private string GenerateJwtToken(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);

            var claims = new List<Claim>
            {
                new Claim("Id", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                 new Claim(ClaimTypes.Role, user.UserRole.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _configuration["baseUrl"], // Specify the issuer
                Expires = DateTime.UtcNow.AddDays(7), // Token expiry (adjust as needed)
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

    }
}
