using CollegeEventManagement.Data;
using CollegeEventManagement.Models;
using CollegeEventManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.Controllers
{
    // Public pages: landing page, about and the error page. No login needed.
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        public HomeController(AppDbContext db) { _db = db; }

        public IActionResult Index()
        {
            var events = _db.EventsFull().ToList();
            var upcoming = events.Where(e => !e.IsCancelled && e.EventDate >= DateTime.Now).OrderBy(e => e.EventDate).ToList();
            var regs = events.SelectMany(e => e.Registrations).ToList();
            var pastRegs = events.Where(e => e.IsPast && !e.IsCancelled).SelectMany(e => e.Registrations).ToList();

            var vm = new HomeVm
            {
                Next = upcoming.FirstOrDefault(),
                Featured = upcoming.Where(e => e.RegistrationOpen).OrderByDescending(e => e.Registrations.Count).ThenBy(e => e.EventDate).Take(3).ToList(),
                Upcoming = upcoming.Take(6).ToList(),
                CategoryCounts = EventCategories.All.ToDictionary(c => c, c => upcoming.Count(e => e.Category == c)),
                Hosts = upcoming.GroupBy(e => e.Host).Select(g => new KeyValuePair<string, int>(g.Key, g.Count())).OrderByDescending(h => h.Value).Take(4).ToList(),
                EventsHosted = events.Count(e => !e.IsCancelled),
                Registrations = regs.Count,
                CheckIns = regs.Count(r => r.Attended),
                Students = _db.Users.Count(u => u.Role == Roles.Student),
                AttendancePercent = Analytics.Percent(pastRegs.Count(r => r.Attended), pastRegs.Count),
                Monthly = Analytics.Monthly(regs)
            };
            return View(vm);
        }

        public IActionResult About() => View();

        // Used for 404 / 403 / unhandled errors (see Program.cs)
        public IActionResult Error(int? id)
        {
            int code = id ?? 500;
            Response.StatusCode = code;
            ViewBag.Code = code;
            ViewBag.Heading = code == 404 ? "We couldn't find that page" : code == 403 ? "You don't have access to that page" : "Something went wrong";
            ViewBag.Message = code == 404
                ? "The event or page may have been removed, or the link is mistyped."
                : code == 403 ? "Sign in with an account that has the right role."
                : "Try again in a moment. If it keeps happening, tell the administrator.";
            return View();
        }
    }
}
