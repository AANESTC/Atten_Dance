using AttendanceAPI.Data;
using AttendanceAPI.Models;
using AttendanceAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto request)
        {
            if (await _context.Users.AnyAsync(u => u.BioID == request.BioID))
            {
                return BadRequest("User with this BioID already exists.");
            }

            var user = new User
            {
                Name = request.Name,
                BioID = request.BioID,
                Department = request.Department,
                Role = request.Role,
                Gender = request.Gender
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { userId = user.Id, name = user.Name });
        }

        [HttpGet("next-id")]
        public async Task<IActionResult> GetNextId()
        {
            var year = DateTime.Now.Year;
            var maxId = await _context.Users.CountAsync();
            var nextIdStr = $"{(maxId + 1):D4}-{year}";
            return Ok(new { nextId = nextIdStr });
        }
    }
}
