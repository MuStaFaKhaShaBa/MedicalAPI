using Medical.Data.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Medical.Models
{
    public class PatientMedicalCreateAPIVM
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public string Type { get; set; }

        public string? Text { get; set; }
        public IFormFile? File { get; set; }
    }
    public class PatientMedicalEditAPIVM : PatientMedicalCreateAPIVM
    {
        public int Id { get; set; }
        public string? FileName { get; set; }
    }


    public class PatientMedicalCreateVM : PatientMedicalCreateAPIVM
    {
        public int Id { get; set; }

        public ApplicationUserVM? Patient { get; set; }
    }
    public class PatientMedicalEditVM : PatientMedicalCreateVM
    {
        public string? FileName { get; set; }
    }
}
