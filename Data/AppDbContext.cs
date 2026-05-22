using AttendanceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<AttendanceRecord> Attendance { get; set; }
        public DbSet<FaceEmbedding> FaceEmbeddings { get; set; }
    }
}
