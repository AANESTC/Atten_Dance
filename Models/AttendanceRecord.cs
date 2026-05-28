using System;

namespace AttendanceAPI.Models
{
    public class AttendanceRecord
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public DateTime AttendanceDate { get; set; }
        public TimeSpan? MorningCheckIn { get; set; }
        public TimeSpan? LunchCheckOut { get; set; }
        public TimeSpan? AfternoonCheckIn { get; set; }
        public TimeSpan? EveningCheckOut { get; set; }
        public string AttendanceStatus { get; set; } = string.Empty;
        public double? TotalWorkingHours { get; set; }
        public bool LateEntryStatus { get; set; }
    }
}
