using CollegeEventManagement.Data;
using CollegeEventManagement.Models;
using CollegeEventManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollegeEventManagement.Controllers
{
    // Event management for organisers (their own events) and admins (every event).
    [Authorize(Roles = "Organizer,Admin")]
    public class OrganizerController : AppController
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notifier;
        public OrganizerController(AppDbContext db, NotificationService notifier) { _db = db; _notifier = notifier; }

        private CollegeEvent Find(int id) => _db.EventsFull().FirstOrDefault(e => e.Id == id);
        private bool CanManage(CollegeEvent ev) => IsAdmin || ev.OrganizerId == CurrentUserId;

        // ---------------- dashboard ----------------
        public IActionResult Dashboard()
        {
            var uid = CurrentUserId;
            var all = _db.EventsFull().ToList();
            var events = (IsAdmin ? all : all.Where(e => e.OrganizerId == uid).ToList()).OrderBy(e => e.EventDate).ToList();
            var regs = events.SelectMany(e => e.Registrations).ToList();
            var pastRegs = events.Where(e => e.IsPast && !e.IsCancelled).SelectMany(e => e.Registrations).ToList();

            var vm = new OrganizerDashboardVm
            {
                Events = events,
                IsAdminView = IsAdmin,
                UpcomingCount = events.Count(e => !e.IsPast && !e.IsCancelled),
                TotalRegistrations = regs.Count,
                TotalCheckIns = regs.Count(r => r.Attended),
                AttendancePercent = Analytics.Percent(pastRegs.Count(r => r.Attended), pastRegs.Count)
            };

            // Registered vs checked-in for finished events (upcoming ones have no check-ins yet)
            foreach (var e in events.Where(e => e.IsPast && !e.IsCancelled && e.Registrations.Count > 0).OrderByDescending(e => e.EventDate).Take(8).Reverse())
            {
                vm.PerEvent.Labels.Add(Analytics.Short(e.Title));
                vm.PerEvent.A.Add(e.Registrations.Count);
                vm.PerEvent.B.Add(e.Registrations.Count(r => r.Attended));
            }

            // Registrations per day, last 14 days
            for (int i = 13; i >= 0; i--)
            {
                var day = DateTime.Today.AddDays(-i);
                vm.Trend.Labels.Add(day.ToString("dd MMM"));
                vm.Trend.A.Add(regs.Count(r => r.RegisteredOn.Date == day));
            }
            return View(vm);
        }

        // ---------------- create / edit / delete ----------------
        [HttpGet]
        public IActionResult Create() => View(new CollegeEvent());

        // Bind lists exactly which fields a form may set (protects OrganizerId, IsCancelled, ...)
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Title,Description,Location,EventDate,DurationMinutes,Category,Capacity,RegistrationDeadline,Host,Requirements,Schedule")] CollegeEvent model)
        {
            Validate(model, requireFuture: true);
            if (!ModelState.IsValid) return View(model);

            model.OrganizerId = CurrentUserId;
            _db.CollegeEvents.Add(model);
            _db.SaveChanges();
            TempData["Success"] = "Event published. Students can register now.";
            return RedirectToAction("Participants", new { id = model.Id });
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var ev = Find(id);
            if (ev == null) return NotFound();
            if (!CanManage(ev)) return Forbid();
            return View(ev);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Edit([Bind("Id,Title,Description,Location,EventDate,DurationMinutes,Category,Capacity,RegistrationDeadline,Host,Requirements,Schedule")] CollegeEvent model)
        {
            var ev = Find(model.Id);
            if (ev == null) return NotFound();
            if (!CanManage(ev)) return Forbid();

            Validate(model, requireFuture: false);
            if (model.Capacity < ev.Registrations.Count)
                ModelState.AddModelError(nameof(model.Capacity), $"{ev.Registrations.Count} students are already registered, so capacity can't be lower than that.");
            if (!ModelState.IsValid) { model.Registrations = ev.Registrations; return View(model); }

            bool whenOrWhereChanged = ev.EventDate != model.EventDate || ev.Location != model.Location;

            // Copy the edited values onto the saved event
            ev.Title = model.Title; ev.Description = model.Description; ev.Location = model.Location;
            ev.EventDate = model.EventDate; ev.DurationMinutes = model.DurationMinutes; ev.Category = model.Category;
            ev.Capacity = model.Capacity; ev.RegistrationDeadline = model.RegistrationDeadline; ev.Host = model.Host;
            ev.Requirements = model.Requirements; ev.Schedule = model.Schedule;
            _db.SaveChanges();

            if (whenOrWhereChanged)
                _notifier.NotifyAttendees(ev, "update", "Event details changed", $"{ev.Title} is now {ev.EventDate:ddd, dd MMM 'at' hh:mm tt} in {ev.Location}.", "/Events/Details/" + ev.Id);

            TempData["Success"] = "Event updated.";
            return RedirectToAction(IsAdmin ? "Events" : "Dashboard", IsAdmin ? "Admin" : "Organizer");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var ev = Find(id);
            if (ev == null) return NotFound();
            if (!CanManage(ev)) return Forbid();

            _notifier.NotifyAttendees(ev, "cancelled", "Event removed", $"{ev.Title} has been removed by the organiser.", null);
            _db.CollegeEvents.Remove(ev);   // its registrations and attendance records are deleted with it
            _db.SaveChanges();
            TempData["Success"] = "Event deleted.";
            return RedirectToAction(IsAdmin ? "Events" : "Dashboard", IsAdmin ? "Admin" : "Organizer");
        }

        // Cancel an event (students are told) or bring it back
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ToggleCancel(int id)
        {
            var ev = Find(id);
            if (ev == null) return NotFound();
            if (!CanManage(ev)) return Forbid();

            ev.IsCancelled = !ev.IsCancelled;
            _db.SaveChanges();
            if (ev.IsCancelled) _notifier.NotifyAttendees(ev, "cancelled", "Event cancelled", $"{ev.Title} has been cancelled.", "/Events/Details/" + ev.Id);
            else _notifier.NotifyAttendees(ev, "update", "Event is back on", $"{ev.Title} has been reinstated.", "/Events/Details/" + ev.Id);
            TempData["Success"] = ev.IsCancelled ? "Event cancelled. Registered students were notified." : "Event reinstated.";
            return RedirectToAction(IsAdmin ? "Events" : "Dashboard", IsAdmin ? "Admin" : "Organizer");
        }

        // ---------------- participants ----------------
        public IActionResult Participants(int id)
        {
            var ev = Find(id);
            if (ev == null) return NotFound();
            if (!CanManage(ev)) return Forbid();
            return View(new ParticipantsVm
            {
                Event = ev,
                Registrations = ev.Registrations.OrderBy(r => r.RegisteredOn).ToList(),
                CheckedIn = ev.Registrations.Count(r => r.Attended)
            });
        }

        public IActionResult ExportCsv(int id)
        {
            var ev = Find(id);
            if (ev == null) return NotFound();
            if (!CanManage(ev)) return Forbid();

            var rows = new List<List<string>> { new List<string> { "No", "Name", "Email", "Department", "Registered on", "Status", "Checked in at", "Method" } };
            int n = 1;
            foreach (var r in ev.Registrations.OrderBy(r => r.RegisteredOn))
                rows.Add(new List<string>
                {
                    (n++).ToString(), r.User.FullName, r.User.Email, r.User.Department ?? "",
                    r.RegisteredOn.ToString("yyyy-MM-dd HH:mm"), r.Attended ? "Attended" : "Registered",
                    r.Attended ? r.Attendance.CheckedInAt.ToString("yyyy-MM-dd HH:mm") : "", r.Attended ? r.Attendance.Method : ""
                });
            return File(Csv.ToBytes(rows), "text/csv", $"participants-event-{ev.Id}.csv");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RemoveParticipant(int id)
        {
            var reg = _db.RegistrationsFull().FirstOrDefault(r => r.Id == id);
            if (reg == null) return NotFound();
            var ev = Find(reg.CollegeEventId);
            if (!CanManage(ev)) return Forbid();

            _db.Registrations.Remove(reg);
            _db.SaveChanges();
            _notifier.Notify(reg.UserId, "update", "Registration removed", $"The organiser removed your registration for {ev.Title}.", "/Events/Details/" + ev.Id, ev.Id);
            TempData["Success"] = $"{reg.User.FullName} was removed from the event.";
            return RedirectToAction("Participants", new { id = ev.Id });
        }

        // ---------------- attendance ----------------
        public IActionResult Scanner(int id)
        {
            var ev = Find(id);
            if (ev == null) return NotFound();
            if (!CanManage(ev)) return Forbid();
            return View(ev);
        }

        // Called by the scanner page (and its manual-entry box). Returns JSON.
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CheckIn(int eventId, string token)
        {
            var ev = Find(eventId);
            if (ev == null) return Json(new { ok = false, code = "not-found", message = "Event not found." });
            if (!CanManage(ev)) return Json(new { ok = false, code = "forbidden", message = "You can't manage this event." });
            if (ev.IsCancelled) return Json(new { ok = false, code = "cancelled", message = "This event is cancelled." });

            var normalized = TokenService.Normalize(token);
            if (normalized.Length == 0) return Json(new { ok = false, code = "invalid", message = "Enter or scan a pass code." });

            var reg = ev.Registrations.FirstOrDefault(r => r.QrToken == normalized);
            if (reg == null)
            {
                bool otherEvent = _db.Registrations.Any(r => r.QrToken == normalized);
                return Json(new
                {
                    ok = false,
                    code = otherEvent ? "wrong-event" : "invalid",
                    message = otherEvent ? "This pass belongs to a different event." : "This code isn't a valid CAMPUSLY pass."
                });
            }

            if (reg.Attended)
                return Json(new { ok = false, code = "duplicate", message = $"{reg.User.FullName} was already checked in at {reg.Attendance.CheckedInAt:hh:mm tt}.", student = reg.User.FullName, time = reg.Attendance.CheckedInAt.ToString("hh:mm tt") });

            try
            {
                _db.Attendances.Add(new Attendance { RegistrationId = reg.Id, Method = "QR", CheckedInById = CurrentUserId });
                _db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // The unique index on RegistrationId stops a double check-in even if two scans arrive together
                return Json(new { ok = false, code = "duplicate", message = $"{reg.User.FullName} was already checked in.", student = reg.User.FullName });
            }

            _notifier.Notify(reg.UserId, "attendance", "Attendance marked", $"You were checked in at {ev.Title}.", "/Student/Activities", ev.Id);
            int checkedIn = ev.Registrations.Count(r => r.Attended || r.Id == reg.Id);   // counts this check-in whether or not EF has linked it yet
            return Json(new
            {
                ok = true, code = "ok", message = $"{reg.User.FullName} checked in.", student = reg.User.FullName,
                department = reg.User.Department, time = DateTime.Now.ToString("hh:mm tt"), checkedIn, total = ev.Registrations.Count
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ManualCheckIn(int id)
        {
            var reg = _db.RegistrationsFull().FirstOrDefault(r => r.Id == id);
            if (reg == null) return NotFound();
            var ev = Find(reg.CollegeEventId);
            if (!CanManage(ev)) return Forbid();

            if (ev.IsCancelled) TempData["Error"] = "This event is cancelled.";
            else if (reg.Attended) TempData["Error"] = $"{reg.User.FullName} is already checked in.";
            else
            {
                _db.Attendances.Add(new Attendance { RegistrationId = reg.Id, Method = "Manual", CheckedInById = CurrentUserId });
                _db.SaveChanges();
                _notifier.Notify(reg.UserId, "attendance", "Attendance marked", $"You were checked in at {ev.Title}.", "/Student/Activities", ev.Id);
                TempData["Success"] = $"{reg.User.FullName} checked in.";
            }
            return RedirectToAction("Participants", new { id = ev.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult UndoCheckIn(int id)
        {
            var reg = _db.RegistrationsFull().FirstOrDefault(r => r.Id == id);
            if (reg == null) return NotFound();
            var ev = Find(reg.CollegeEventId);
            if (!CanManage(ev)) return Forbid();

            if (reg.Attended)
            {
                _db.Attendances.Remove(reg.Attendance);
                _db.SaveChanges();
                TempData["Success"] = $"Check-in undone for {reg.User.FullName}.";
            }
            return RedirectToAction("Participants", new { id = ev.Id });
        }

        // Checks that attributes alone cannot express
        private void Validate(CollegeEvent m, bool requireFuture)
        {
            if (!EventCategories.All.Contains(m.Category))
                ModelState.AddModelError(nameof(m.Category), "Choose one of the listed categories.");
            if (requireFuture && m.EventDate <= DateTime.Now)
                ModelState.AddModelError(nameof(m.EventDate), "Pick a date and time in the future.");
            if (m.RegistrationDeadline > m.EventDate)
                ModelState.AddModelError(nameof(m.RegistrationDeadline), "The deadline must be on or before the event starts.");
            if (requireFuture && m.RegistrationDeadline < DateTime.Now)
                ModelState.AddModelError(nameof(m.RegistrationDeadline), "Pick a deadline in the future, or no one can register.");
        }
    }
}
