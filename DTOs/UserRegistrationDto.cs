namespace AttendanceAPI.DTOs
{
    public class UserRegistrationDto
    {
        public string Name { get; set; } = string.Empty;
        public string BioID { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
    }
}
