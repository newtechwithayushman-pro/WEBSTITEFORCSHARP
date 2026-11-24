using CollegeEventManagement.Data;
using CollegeEventManagement.Models;
using CollegeEventManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.Controllers
{
    // Pages for students. Only logged-in Students can open them.
    [Authorize(Roles = Roles.Student)]
    public class StudentController : AppController
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notifier;
        public StudentController(AppDbContext db, NotificationService notifier) { _db = db; _notifier = notifier; }

        public IActionResult Dashboard()
        {
            var uid = CurrentUserId;
            _notifier.SyncStudent(uid);

            var student = _db.Users.Find(uid);
            var regs = _db.RegistrationsFull().Where(r => r.UserId == uid).ToList();
            var live = regs.Where(r => !r.CollegeEvent.IsCancelled).ToList();
            var pastRegs = live.Where(r => r.CollegeEvent.IsPast).ToList();
            var upcomingRegs = live.Where(r => !r.CollegeEvent.IsPast).OrderBy(r => r.CollegeEvent.EventDate).ToList();

            // Recommendations: open events in the categories this student takes part in most
            var score = regs.GroupBy(r => r.CollegeEvent.Category).ToDictionary(g => g.Key, g => g.Sum(r => r.Attended ? 2 : 1));
            var mineIds = regs.Select(r => r.CollegeEventId).ToList();
            var recs = _db.EventsFull().ToList()
                .Where(e => e.RegistrationOpen && !mineIds.Contains(e.Id))
                .OrderByDescending(e => score.ContainsKey(e.Category) ? score[e.Category] : 0).ThenBy(e => e.EventDate)
                .Take(3).ToList();

            // Activity timeline built from real timestamps
            var timeline = new List<TimelineItem>();
            foreach (var r in live)
            {
                timeline.Add(new TimelineItem { When = r.RegisteredOn, Kind = "registered", Text = "Registered for " + r.CollegeEvent.Title, EventId = r.CollegeEventId });
                if (r.Attended) timeline.Add(new TimelineItem { When = r.Attendance.CheckedInAt, Kind = "attended", Text = "Checked in at " + r.CollegeEvent.Title, EventId = r.CollegeEventId });
                if (r.Attended && r.CollegeEvent.IsPast) timeline.Add(new TimelineItem { When = r.CollegeEvent.EndDate, Kind = "certificate", Text = "Certificate earned: " + r.CollegeEvent.Title, EventId = r.CollegeEventId });
            }

            var vm = new StudentDashboardVm
            {
                Student = student,
                TotalRegistrations = live.Count,
                Attended = live.Count(r => r.Attended),
                Upcoming = upcomingRegs.Count,
                Certificates = pastRegs.Count(r => r.Attended),
                AttendancePercent = Analytics.Percent(pastRegs.Count(r => r.Attended), pastRegs.Count),
                NextUp = upcomingRegs.FirstOrDefault(),
                UpcomingRegistrations = upcomingRegs.Take(4).ToList(),
                Recommendations = recs,
                Timeline = timeline.OrderByDescending(t => t.When).Take(8).ToList(),
                Notifications = _db.Notifications.Where(n => n.UserId == uid).OrderByDescending(n => n.CreatedOn).Take(4).ToList(),
                Monthly = Analytics.Monthly(live),
                CalendarEvents = live.Select(r => (object)new { id = r.CollegeEventId, title = r.CollegeEvent.Title, date = r.CollegeEvent.EventDate.ToString("s"), venue = r.CollegeEvent.Location, attended = r.Attended }).ToList()
            };
            foreach (var g in live.GroupBy(r => r.CollegeEvent.Category).OrderByDescending(g => g.Count()))
            {
                vm.ByCategory.Labels.Add(g.Key);
                vm.ByCategory.A.Add(g.Count());
            }
            return View(vm);
        }

        // All of the student's registrations, with QR passes
        public IActionResult Activities()
        {
            var uid = CurrentUserId;
            _notifier.SyncStudent(uid);
            var regs = _db.RegistrationsFull().Where(r => r.UserId == uid).ToList()
                .OrderBy(r => r.CollegeEvent.EventDate).ToList();
            return View(regs);
        }

        // The full-size QR pass for one registration
        public IActionResult Pass(int id)
        {
            var uid = CurrentUserId;
            var reg = _db.RegistrationsFull().FirstOrDefault(r => r.Id == id && r.UserId == uid);
            if (reg == null) return NotFound();
            return View(reg);
        }

        // A printable certificate for an attended, finished event
        public IActionResult Certificate(int id)
        {
            var uid = CurrentUserId;
            var reg = _db.RegistrationsFull().FirstOrDefault(r => r.Id == id && r.UserId == uid);
            if (reg == null) return NotFound();
            if (!reg.Attended || !reg.CollegeEvent.IsPast || reg.CollegeEvent.IsCancelled)
            {
                TempData["Error"] = "Certificates are available after you've been checked in and the event has finished.";
                return RedirectToAction("Activities");
            }
            ViewBag.Code = $"CMPL-{reg.CollegeEventId:D3}-{reg.Id:D4}-{reg.QrToken.Substring(0, 4)}";
            return View(reg);
        }
    }
}
