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
                
                var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
                var existing = await _context.Attendance.FirstOrDefaultAsync(a => a.UserId == uId && a.AttendanceDate == today);
                
                var user = await _context.Users.FindAsync(uId);

                if (existing != null)
                {
                    return Ok(new { message = "Attendance already marked", userId = uId, userName = user?.Name, status = "Already Marked" });
                }

                var attendance = new AttendanceRecord
                {
                    UserId = uId,
                    AttendanceDate = DateTime.UtcNow,
                    AttendanceTime = DateTime.UtcNow.TimeOfDay,
                    Status = "Present"
                };

                _context.Attendance.Add(attendance);
                await _context.SaveChangesAsync();
                
                return Ok(new { message = "Attendance marked", userId = uId, userName = user?.Name, status = "Marked" });
            }

            return Ok(new { message = "No match found", matched = false });
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendance()
        {
            var records = await _context.Attendance
                .Include(a => a.User)
                .OrderByDescending(a => a.AttendanceDate)
                .ThenByDescending(a => a.AttendanceTime)
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    UserName = a.User!.Name,
                    BioID = a.User.BioID,
                    a.AttendanceDate,
                    a.AttendanceTime,
                    a.Status
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
