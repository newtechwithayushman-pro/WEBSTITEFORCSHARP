using System.Text;
using CollegeEventManagement.Data;
using CollegeEventManagement.Models;
using CollegeEventManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.Controllers
{
    // Event discovery is public. Registering needs a signed-in Student.
    public class EventsController : AppController
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notifier;
        public EventsController(AppDbContext db, NotificationService notifier) { _db = db; _notifier = notifier; }

        // Explore: every non-cancelled event. Search, filters and sorting run in the browser (explore.js).
        public IActionResult Index()
        {
            var events = _db.EventsFull().Where(e => !e.IsCancelled).OrderBy(e => e.EventDate).ToList();

            // Ids of events this student already joined (to show a badge)
            var mine = new List<int>();
            if (User.IsInRole(Roles.Student))
            {
                var uid = CurrentUserId;
                mine = events.Where(e => e.Registrations.Any(r => r.UserId == uid)).Select(e => e.Id).ToList();
            }
            ViewBag.RegisteredIds = mine;
            return View(events);
        }

        // Details of one event
        public IActionResult Details(int id)
        {
            var ev = _db.EventsFull().FirstOrDefault(e => e.Id == id);
            if (ev == null) return NotFound();

            Registration mine = null;
            if (User.IsInRole(Roles.Student))
            {
                var uid = CurrentUserId;
                mine = ev.Registrations.FirstOrDefault(r => r.UserId == uid);
            }
            ViewBag.MyRegistration = mine;
            ViewBag.IsRegistered = mine != null;
            ViewBag.Similar = _db.EventsFull().Where(e => e.Category == ev.Category && e.Id != ev.Id && !e.IsCancelled)
                .ToList().Where(e => e.EventDate >= DateTime.Now).OrderBy(e => e.EventDate).Take(3).ToList();
            return View(ev);
        }

        // Register the student for an event
        [Authorize(Roles = Roles.Student), HttpPost, ValidateAntiForgeryToken]
        public IActionResult Register(int id)
        {
            var ev = _db.EventsFull().FirstOrDefault(e => e.Id == id);
            if (ev == null) return NotFound();
            var uid = CurrentUserId;

            if (ev.IsCancelled) TempData["Error"] = "This event has been cancelled.";
            else if (ev.EventDate <= DateTime.Now) TempData["Error"] = "Registration is closed because this event has already started.";
            else if (ev.Registrations.Any(r => r.UserId == uid)) TempData["Error"] = "You are already registered for this event.";   // prevent duplicates
            else if (ev.RegistrationDeadline < DateTime.Now) TempData["Error"] = "The registration deadline for this event has passed.";
            else if (ev.IsFull) TempData["Error"] = "This event is full. Check back in case someone cancels.";
            else
            {
                int seatsTaken = ev.Registrations.Count + 1;   // read before adding: EF links the new row into ev.Registrations
                var reg = new Registration { UserId = uid, CollegeEventId = id, QrToken = TokenService.NewToken() };
                _db.Registrations.Add(reg);
                _db.SaveChanges();

                _notifier.Notify(uid, "registration", "Registration confirmed", $"You're registered for {ev.Title}. Your QR pass is ready.", "/Student/Pass/" + reg.Id, ev.Id);
                _notifier.Notify(ev.OrganizerId, "info", "New registration", $"{User.Identity.Name} registered for {ev.Title} ({seatsTaken} of {ev.Capacity}).", "/Organizer/Participants/" + ev.Id, ev.Id);
                TempData["Success"] = "You're registered. Your QR pass is ready in My Activities.";
            }
            return RedirectToAction("Details", new { id });
        }

        // Cancel the student's own registration
        [Authorize(Roles = Roles.Student), HttpPost, ValidateAntiForgeryToken]
        public IActionResult Unregister(int id)
        {
            var uid = CurrentUserId;
            var reg = _db.RegistrationsFull().FirstOrDefault(r => r.UserId == uid && r.CollegeEventId == id);
            if (reg == null) TempData["Error"] = "You are not registered for this event.";
            else if (reg.Attended) TempData["Error"] = "You've already been checked in, so this registration can't be cancelled.";
            else if (reg.CollegeEvent.EventDate <= DateTime.Now) TempData["Error"] = "This event has already started, so the registration can't be cancelled.";
            else
            {
                _db.Registrations.Remove(reg);
                _db.SaveChanges();
                _notifier.Notify(uid, "info", "Registration cancelled", $"Your seat for {reg.CollegeEvent.Title} was released.", "/Events/Details/" + id, id);
                TempData["Success"] = "Registration cancelled. Your seat is open again.";
            }
            return RedirectToAction("Details", new { id });
        }

        // Downloads an .ics file so the event can be added to any calendar app
        public IActionResult Calendar(int id)
        {
            var ev = _db.CollegeEvents.Find(id);
            if (ev == null) return NotFound();

            static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r", "").Replace("\n", "\\n");
            const string fmt = "yyyyMMdd'T'HHmmss";

            var sb = new StringBuilder();
            sb.Append("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//CAMPUSLY//EN\r\nBEGIN:VEVENT\r\n");
            sb.Append($"UID:event-{ev.Id}@campusly\r\n");
            sb.Append($"DTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}\r\n");
            sb.Append($"DTSTART:{ev.EventDate.ToString(fmt)}\r\n");
            sb.Append($"DTEND:{ev.EventDate.AddMinutes(ev.DurationMinutes).ToString(fmt)}\r\n");
            sb.Append($"SUMMARY:{Esc(ev.Title)}\r\n");
            sb.Append($"LOCATION:{Esc(ev.Location)}\r\n");
            sb.Append($"DESCRIPTION:{Esc(ev.Description)}\r\n");
            sb.Append("END:VEVENT\r\nEND:VCALENDAR\r\n");

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/calendar", $"event-{ev.Id}.ics");
        }
    }
}
