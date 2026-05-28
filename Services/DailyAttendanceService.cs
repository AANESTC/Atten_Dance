using AttendanceAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendanceAPI.Services
{
    public class DailyAttendanceService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyAttendanceService> _logger;

        public DailyAttendanceService(IServiceProvider serviceProvider, ILogger<DailyAttendanceService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DailyAttendanceService running.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                
                // We evaluate all missing attendance logic (like full-day absents for people who never showed up).
                // Usually this runs at end of day, but we will evaluate it periodically for today.
                // In a real app we might run this at 23:59.
                
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);

                        var users = await context.Users.ToListAsync(stoppingToken);
                        foreach (var user in users)
                        {
                            var record = await context.Attendance.FirstOrDefaultAsync(a => a.UserId == user.Id && a.AttendanceDate == today, stoppingToken);
                            if (record == null)
                            {
                                // User didn't show up at all
                                context.Attendance.Add(new Models.AttendanceRecord
                                {
                                    UserId = user.Id,
                                    AttendanceDate = today,
                                    AttendanceStatus = "Absent",
                                    LateEntryStatus = false
                                });
                            }
                            else
                            {
                                // Evaluate existing record
                                EvaluateAttendanceRecord(record);
                            }
                        }
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing daily attendance.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private void EvaluateAttendanceRecord(Models.AttendanceRecord record)
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
    }
}
