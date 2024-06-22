using Medical.Data.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Medical.Models
{
    public class PatientMedicalVM
    {
        public PatientMedicalVM(PatientMedical patientMedical, string baseUrl)
        {
            Id = patientMedical.Id;
            Type = patientMedical.Type;

            Text = patientMedical.Text;
            FileName = Path.Combine(baseUrl, patientMedical.FileName);

            CreatedAt = patientMedical.CreatedAt;
            UpdatedAt = patientMedical.UpdatedAt;
        }

        public int Id { get; set; }
        public string Type { get; set; }

        public string? Text { get; set; }
        public string? FileName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
