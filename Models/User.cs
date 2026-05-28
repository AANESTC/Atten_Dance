using System.ComponentModel.DataAnnotations;

namespace AttendanceAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BioID { get; set; } = string.Empty;
        public string Department { get; set; } = "Engineering";
        public string Role { get; set; } = string.Empty;
        public string Gender { get; set; } = "Not Specified";
    }
}
