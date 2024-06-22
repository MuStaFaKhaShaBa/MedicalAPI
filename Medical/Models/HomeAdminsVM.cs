using Medical.Data.Entities;

namespace Medical.Models
{
    public class HomeAdminsVM
    {
        public ApplicationUserVM User { get; set; }

        public int TotalAdmins { get; set; }
        public int TotalDoctors { get; set; }

        public int TotalPatients { get; set; }
    }
}
