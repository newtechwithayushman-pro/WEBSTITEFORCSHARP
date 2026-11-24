using CollegeEventManagement.Data;
using CollegeEventManagement.Models;
using CollegeEventManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.Controllers
{
    // Campus-wide oversight. Only Admins can open these pages.
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : AppController
    {
        private readonly AppDbContext _db;
        public AdminController(AppDbContext db) { _db = db; }

        public IActionResult Dashboard()
        {
            var events = _db.EventsFull().ToList();
            var users = _db.Users.ToList();
            var regs = events.SelectMany(e => e.Registrations).ToList();
            var pastRegs = events.Where(e => e.IsPast && !e.IsCancelled).SelectMany(e => e.Registrations).ToList();

            var vm = new AdminDashboardVm
            {
                Students = users.Count(u => u.Role == Roles.Student),
                Organizers = users.Count(u => u.Role == Roles.Organizer),
                Events = events.Count,
                UpcomingEvents = events.Count(e => !e.IsPast && !e.IsCancelled),
                Registrations = regs.Count,
                CheckIns = regs.Count(r => r.Attended),
                AttendancePercent = Analytics.Percent(pastRegs.Count(r => r.Attended), pastRegs.Count),
                Monthly = Analytics.Monthly(regs)
            };
            foreach (var c in EventCategories.All)
            {
                var inCat = events.Where(e => e.Category == c && !e.IsCancelled).ToList();
                vm.ByCategory.Labels.Add(c);
                vm.ByCategory.A.Add(inCat.Count);
                vm.ByCategory.B.Add(inCat.Sum(e => e.Registrations.Count));
            }
            foreach (var e in events.Where(e => !e.IsCancelled).OrderByDescending(e => e.Registrations.Count).Take(6))
            {
                vm.TopEvents.Labels.Add(Analytics.Short(e.Title, 22));
                vm.TopEvents.A.Add(e.Registrations.Count);
            }
            foreach (var role in Roles.All)
            {
                vm.Roles.Labels.Add(role + "s");
                vm.Roles.A.Add(users.Count(u => u.Role == role));
            }
            foreach (var r in regs.OrderByDescending(r => r.RegisteredOn).Take(8))
                vm.Recent.Add(new TimelineItem { When = r.RegisteredOn, Kind = "registered", Text = $"{r.User.FullName} registered for {events.First(e => e.Id == r.CollegeEventId).Title}", EventId = r.CollegeEventId });
            return View(vm);
        }

        // ---------------- users ----------------
        public IActionResult Users()
        {
            ViewBag.OwnedCounts = _db.CollegeEvents.ToList().GroupBy(e => e.OrganizerId).ToDictionary(g => g.Key, g => g.Count());
            ViewBag.RegCounts = _db.Registrations.ToList().GroupBy(r => r.UserId).ToDictionary(g => g.Key, g => g.Count());
            return View(_db.Users.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToList());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult SetRole(int id, string role)
        {
            var user = _db.Users.Find(id);
            if (user == null) return NotFound();
            if (!Roles.All.Contains(role)) TempData["Error"] = "Choose a valid role.";
            else if (user.Id == CurrentUserId) TempData["Error"] = "You can't change your own role.";
            else if (user.Role == Roles.Organizer && role == Roles.Student && _db.CollegeEvents.Any(e => e.OrganizerId == id))
                TempData["Error"] = $"{user.FullName} still owns events. Delete or reassign them before changing this role.";
            else
            {
                user.Role = role;
                _db.SaveChanges();
                TempData["Success"] = $"{user.FullName} is now {(role == Roles.Student ? "a" : "an")} {role.ToLower()}. The change applies the next time they sign in.";
            }
            return RedirectToAction("Users");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            var user = _db.Users.Find(id);
            if (user == null) return NotFound();
            if (user.Id == CurrentUserId) TempData["Error"] = "You can't delete your own account.";
            else if (_db.CollegeEvents.Any(e => e.OrganizerId == id)) TempData["Error"] = $"{user.FullName} still owns events. Delete them first.";
            else
            {
                _db.Users.Remove(user);   // their registrations and notifications go with them
                _db.SaveChanges();
                TempData["Success"] = "User deleted.";
            }
            return RedirectToAction("Users");
        }

        // ---------------- events and reports ----------------
        public IActionResult Events() => View(_db.EventsFull().ToList().OrderByDescending(e => e.EventDate).ToList());

        public IActionResult Reports()
        {
            var events = _db.EventsFull().ToList();
            ViewBag.Users = _db.Users.Count();
            return View(events);
        }

        public IActionResult EventsCsv()
        {
            var rows = new List<List<string>> { new List<string> { "Id", "Title", "Category", "Host", "Organizer", "Date", "Venue", "Capacity", "Registered", "Checked in", "Status" } };
            foreach (var e in _db.EventsFull().ToList().OrderBy(e => e.EventDate))
                rows.Add(new List<string> { e.Id.ToString(), e.Title, e.Category, e.Host, e.Organizer?.FullName ?? "", e.EventDate.ToString("yyyy-MM-dd HH:mm"), e.Location, e.Capacity.ToString(), e.Registrations.Count.ToString(), e.Registrations.Count(r => r.Attended).ToString(), e.Status });
            return File(Csv.ToBytes(rows), "text/csv", "campusly-events.csv");
        }

        public IActionResult RegistrationsCsv()
        {
            var rows = new List<List<string>> { new List<string> { "Event", "Student", "Email", "Department", "Registered on", "Attended" } };
            foreach (var e in _db.EventsFull().ToList().OrderBy(e => e.EventDate))
                foreach (var r in e.Registrations.OrderBy(r => r.RegisteredOn))
                    rows.Add(new List<string> { e.Title, r.User.FullName, r.User.Email, r.User.Department ?? "", r.RegisteredOn.ToString("yyyy-MM-dd HH:mm"), r.Attended ? "Yes" : "No" });
            return File(Csv.ToBytes(rows), "text/csv", "campusly-registrations.csv");
        }

        public IActionResult AttendanceCsv()
        {
            var rows = new List<List<string>> { new List<string> { "Event", "Student", "Checked in at", "Method" } };
            foreach (var e in _db.EventsFull().ToList().OrderBy(e => e.EventDate))
                foreach (var r in e.Registrations.Where(r => r.Attended).OrderBy(r => r.Attendance.CheckedInAt))
                    rows.Add(new List<string> { e.Title, r.User.FullName, r.Attendance.CheckedInAt.ToString("yyyy-MM-dd HH:mm"), r.Attendance.Method });
            return File(Csv.ToBytes(rows), "text/csv", "campusly-attendance.csv");
        }
    }
}
