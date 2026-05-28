using AttendanceAPI.Data;
using AttendanceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AttendanceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public AttendanceController(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("detect")]
        public async Task<IActionResult> Detect([FromForm] IFormFile image)
        {
            if (image == null) return BadRequest("No image uploaded");

            using var content = new MultipartFormDataContent();
            var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            ms.Position = 0;
            var streamContent = new StreamContent(ms);
            streamContent.Headers.Add("Content-Type", image.ContentType);
            content.Add(streamContent, "image", image.FileName);

            var client = _httpClientFactory.CreateClient("AIService");
            var response = await client.PostAsync("/api/ai/detect", content);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DetectionResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result != null && result.Matched && result.UserId.HasValue)
            {
                int uId = result.UserId.Value;
                
                var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
                var existing = await _context.Attendance.FirstOrDefaultAsync(a => a.UserId == uId && a.AttendanceDate == today);
                var user = await _context.Users.FindAsync(uId);

                var currentTime = DateTime.Now.TimeOfDay;
                string message = "";
                string status = "";

                if (existing == null)
                {
                    existing = new AttendanceRecord
                    {
                        UserId = uId,
                        AttendanceDate = today,
                        AttendanceStatus = "Absent",
                        LateEntryStatus = false
                    };
                    _context.Attendance.Add(existing);
                }

                // Check-in limits
                if (currentTime >= new TimeSpan(8, 0, 0) && currentTime <= new TimeSpan(9, 5, 59))
                {
                    if (existing.MorningCheckIn == null)
                    {
                        existing.MorningCheckIn = currentTime;
                        existing.AttendanceStatus = "Present";
                        existing.LateEntryStatus = false;
                        message = "Morning Attendance Marked (Present)";
                        status = "Marked";
                    }
                    else
                    {
                        message = "Attendance Already Marked";
                        status = "Already Marked";
                    }
                }
                else if (currentTime > new TimeSpan(9, 5, 59) && currentTime < new TimeSpan(13, 0, 0))
                {
                    if (existing.MorningCheckIn == null)
                    {
                        existing.MorningCheckIn = currentTime;
                        existing.LateEntryStatus = true;
                        existing.AttendanceStatus = "Late Entry";
                        message = "Morning Attendance Marked (Late Entry)";
                        status = "Marked";
                    }
                    else
                    {
                        message = "Attendance Already Marked";
                        status = "Already Marked";
                    }
                }
                else if (currentTime >= new TimeSpan(13, 0, 0) && currentTime <= new TimeSpan(14, 0, 59))
                {
                    if (existing.LunchCheckOut == null)
                    {
                        existing.LunchCheckOut = currentTime;
                        message = "Lunch Break Check-Out Recorded";
                        status = "Marked";
                    }
                    else if (existing.AfternoonCheckIn == null)
                    {
                        existing.AfternoonCheckIn = currentTime;
                        message = "Afternoon Check-In Recorded";
                        status = "Marked";
                    }
                    else
                    {
                        message = "Attendance Already Marked";
                        status = "Already Marked";
                    }
                }
                else if (currentTime > new TimeSpan(14, 0, 59) && currentTime < new TimeSpan(18, 40, 0))
                {
                    if (existing.AfternoonCheckIn == null && existing.LunchCheckOut != null)
                    {
                        existing.AfternoonCheckIn = currentTime;
                        message = "Afternoon Check-In Recorded (Late)";
                        status = "Marked";
                    }
                    else if (existing.EveningCheckOut == null)
                    {
                        existing.EveningCheckOut = currentTime;
                        message = "Evening Check-Out Recorded (Early Exit)";
                        status = "Marked";
                    }
                    else
                    {
                        message = "Scan out of allowed time range";
                        status = "Invalid Time";
                    }
                }
                else if (currentTime >= new TimeSpan(18, 40, 0))
                {
                    if (existing.EveningCheckOut == null)
                    {
                        existing.EveningCheckOut = currentTime;
                        message = "Evening Check-Out Recorded";
                        status = "Marked";
                    }
                    else
                    {
                        message = "Attendance Already Marked";
                        status = "Already Marked";
                    }
                }
                else
                {
                    return Ok(new { message = "Scan out of allowed time range", userId = uId, userName = user?.Name, status = "Invalid Time" });
                }

                EvaluateAttendanceRecord(existing);
                await _context.SaveChangesAsync();

                return Ok(new { message = message, userId = uId, userName = user?.Name, status = status });
            }

            return Ok(new { message = "No match found", matched = false });
        }

        [HttpPost("evaluate-all")]
        public async Task<IActionResult> EvaluateAll()
        {
            var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
            var records = await _context.Attendance.Where(a => a.AttendanceDate == today).ToListAsync();
            foreach (var r in records)
            {
                EvaluateAttendanceRecord(r);
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Evaluated all for today" });
        }

        private void EvaluateAttendanceRecord(AttendanceRecord record)
        {
            bool hasMorningCheckIn = record.MorningCheckIn != null;
            bool isLateMorning = hasMorningCheckIn && record.MorningCheckIn > new TimeSpan(9, 5, 59);

            bool hasLunchOut = record.LunchCheckOut != null;
            bool hasAfternoonIn = record.AfternoonCheckIn != null;

            bool hasEveningCheckOut = record.EveningCheckOut != null;
            bool isEarlyExit = hasEveningCheckOut && record.EveningCheckOut < new TimeSpan(18, 40, 0);

            bool morningSessionValid = hasMorningCheckIn;
            bool afternoonSessionValid = hasAfternoonIn || (!hasLunchOut && hasEveningCheckOut && record.EveningCheckOut > new TimeSpan(14, 0, 0));

            if (!morningSessionValid && !afternoonSessionValid)
            {
                record.AttendanceStatus = "Absent";
            }
            else if (!morningSessionValid && afternoonSessionValid)
            {
                record.AttendanceStatus = "Half-Day";
            }
            else if (morningSessionValid && !afternoonSessionValid)
            {
                record.AttendanceStatus = "Half-Day";
            }
            else
            {
                if (isEarlyExit)
                {
                    record.AttendanceStatus = "Early Exit";
                }
                else if (!hasEveningCheckOut)
                {
                    record.AttendanceStatus = "Incomplete Session";
                }
                else if (isLateMorning)
                {
                    record.AttendanceStatus = "Late Entry";
                }
                else
                {
                    record.AttendanceStatus = "Present";
                }
            }

            double totalHours = 0;
            if (record.MorningCheckIn.HasValue)
            {
                var outTime = record.LunchCheckOut ?? (record.EveningCheckOut ?? record.MorningCheckIn.Value);
                if (outTime > record.MorningCheckIn.Value)
                    totalHours += (outTime - record.MorningCheckIn.Value).TotalHours;
            }
            if (record.AfternoonCheckIn.HasValue)
            {
                var outTime = record.EveningCheckOut ?? record.AfternoonCheckIn.Value;
                if (outTime > record.AfternoonCheckIn.Value)
                    totalHours += (outTime - record.AfternoonCheckIn.Value).TotalHours;
            }
            record.TotalWorkingHours = Math.Round(totalHours, 2);
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendance()
        {
            var records = await _context.Attendance
                .Include(a => a.User)
                .OrderByDescending(a => a.Id)
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    UserName = a.User!.Name,
                    Department = a.User.Department,
                    Gender = a.User.Gender,
                    BioID = a.User.BioID,
                    a.AttendanceDate,
                    a.MorningCheckIn,
                    a.LunchCheckOut,
                    a.AfternoonCheckIn,
                    a.EveningCheckOut,
                    a.TotalWorkingHours,
                    a.LateEntryStatus,
                    Status = a.AttendanceStatus
                })
                .ToListAsync();

            return Ok(records);
        }
    }

    public class DetectionResponse
    {
        public bool Matched { get; set; }
        public int? UserId { get; set; }
    }
}
