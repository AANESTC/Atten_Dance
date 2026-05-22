using System;

namespace AttendanceAPI.Models
{
    public class AttendanceRecord
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public DateTime AttendanceDate { get; set; }
        public TimeSpan AttendanceTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
