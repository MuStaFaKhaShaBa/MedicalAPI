using Medical.Repositories;
using Medical.Specifications.PatientMedical_;
using Medical.Specifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Medical.Models;
using Medical.Data.Entities;
using Medical.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Medical.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PatientMedicalsAPIController : ControllerBase
    {
        private readonly PatientMedicalRepo _patientMedicalRepo;
        private readonly ApplicationUserRepo _applicationUserRepo;
        private readonly IConfiguration _configuration;

        public PatientMedicalsAPIController(PatientMedicalRepo patientMedicalRepo, ApplicationUserRepo applicationUserRepo, IConfiguration configuration)
        {
            _patientMedicalRepo = patientMedicalRepo;
            _applicationUserRepo = applicationUserRepo;
            _configuration = configuration;
        }

        // GET: api/PatientMedicalsAPI
        [HttpGet("{patientId}")]
        public async Task<IActionResult> Index(int patientId,
            [FromQuery] BaseGlobalSpecs<PatientMedicalNavigations, PatientMedicalSearch>? specs)
        {
            specs ??= new();
            specs.Search ??= new() { PatientId = patientId };
            var specification = new PatientMedicalSpecifications(specs);

            var patient = await _applicationUserRepo.GetByIdAsync(patientId);
            if (patient == null)
            {
                return NotFound($"Patient with ID {patientId} not found.");
            }

            var entities = await _patientMedicalRepo.GetAllAsync(specification, specs.Pagination);
            return Ok(entities);
        }


        [Authorize(Roles = "Admin,Doctor")]
        // POST: api/PatientMedicalsAPI/Create
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] PatientMedicalCreateAPIVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var patientMedical = new PatientMedical
                {
                    PatientId = model.PatientId,
                    Type = model.Type,
                    Text = model.Text,
                };

                if (model.File != null)
                {
                    var fileName = GetValidatedFileName(model.File.FileName);
                    var folderPath = _configuration["reports:upload"];
                    patientMedical.FileName = DocumentSettings.SaveFile(fileName, model.File, folderPath);
                }

                await _patientMedicalRepo.AddAsync(patientMedical);

                if (await _patientMedicalRepo.CommitAsync() > 0)
                {
                    return Ok(new { Message = $"Report {patientMedical.Type}, Added Successfully" });
                }

                return BadRequest("Data Not Valid");
            }
            catch (Exception ex)
            {
                // Log the error (uncomment the line below after adding a logger)
                // _logger.LogError(ex, "Error occurred while creating a patient medical record.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Something Went Wrong");
            }
        }

        private string GetValidatedFileName(string fileName)
        {
            return Path.GetFileName(fileName.Length > 200 ? fileName.Substring(0, 200) : fileName);
        }

        [Authorize(Roles = "Admin,Doctor")]
        // GET: api/PatientMedicalsAPI/Edit/5
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var patientMedical = await _patientMedicalRepo.GetAsync(new(new() { Search = new() { Id = id }, Navigations = new() { EnablePatient = true } }));
            if (patientMedical == null)
            {
                return NotFound();
            }

            var model = new PatientMedicalEditAPIVM
            {
                Id = patientMedical.Id,
                PatientId = patientMedical.PatientId,
                Type = patientMedical.Type,
                Text = patientMedical.Text,
                FileName = patientMedical.FileName
            };

            return Ok(model);
        }

        [Authorize(Roles = "Admin,Doctor")]
        // POST: api/PatientMedicalsAPI/Edit/5
        [HttpPost("edit/{id}")]
        public async Task<IActionResult> Edit(int id, [FromForm] PatientMedicalEditAPIVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var patientMedical = await _patientMedicalRepo.GetByIdAsync(id);
                if (patientMedical == null)
                {
                    return NotFound();
                }

                patientMedical.Type = model.Type;
                patientMedical.Text = model.Text;
                patientMedical.UpdatedAt = DateTime.UtcNow;

                if (model.File != null)
                {
                    var fileName = GetValidatedFileName(model.File.FileName);
                    var folderPath = _configuration["reports:upload"];

                    if (!string.IsNullOrEmpty(patientMedical.FileName))
                        DocumentSettings.RemoveFile(Path.Combine(folderPath, patientMedical.FileName));

                    patientMedical.FileName = DocumentSettings.SaveFile(fileName, model.File, folderPath);
                }

                _patientMedicalRepo.Update(patientMedical);

                if (await _patientMedicalRepo.CommitAsync() > 0)
                {
                    return Ok(new { Message = $"Report {patientMedical.Type}, Updated Successfully" });
                }

                return BadRequest("Data Not Valid");
            }
            catch (Exception ex)
            {
                // Log the error (uncomment the line below after adding a logger)
                // _logger.LogError(ex, "Error occurred while editing a patient medical record.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Something Went Wrong");
            }
        }

        [Authorize(Roles = "Admin,Doctor")]
        // POST: api/PatientMedicalsAPI/Delete/5
        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var patientMedical = await _patientMedicalRepo.GetByIdAsync(id);
                if (patientMedical == null)
                {
                    return NotFound();
                }

                _patientMedicalRepo.Delete(patientMedical);

                if (await _patientMedicalRepo.CommitAsync() > 0)
                {
                    if (!string.IsNullOrEmpty(patientMedical.FileName))
                    {
                        var folderPath = _configuration["reports:upload"];
                        DocumentSettings.RemoveFile(Path.Combine(folderPath, patientMedical.FileName));
                    }
                    return Ok(new { Message = $"Report {patientMedical.Type}, Deleted Successfully" });
                }

                return BadRequest("Data Not Valid");
            }
            catch (Exception ex)
            {
                // Log the error (uncomment the line below after adding a logger)
                // _logger.LogError(ex, "Error occurred while deleting a patient medical record.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Something Went Wrong");
            }
        }
    }
}
