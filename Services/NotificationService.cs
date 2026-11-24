using CollegeEventManagement.Data;
using CollegeEventManagement.Models;

namespace CollegeEventManagement.Services
{
    // Creates in-app notifications. (These are shown inside the site only: no e-mail, SMS or push.)
    public class NotificationService
    {
        private readonly AppDbContext _db;
        public NotificationService(AppDbContext db) { _db = db; }

        public void Notify(int userId, string type, string title, string message, string link = null, int? eventId = null)
        {
            _db.Notifications.Add(new Notification { UserId = userId, Type = type, Title = title, Message = message, Link = link, EventId = eventId });
            _db.SaveChanges();
        }

        // Tell everyone registered for an event (e.g. date changed, event cancelled)
        public void NotifyAttendees(CollegeEvent ev, string type, string title, string message, string link)
        {
            foreach (var userId in ev.Registrations.Select(r => r.UserId).Distinct())
                _db.Notifications.Add(new Notification { UserId = userId, Type = type, Title = title, Message = message, Link = link, EventId = ev.Id });
            _db.SaveChanges();
        }

        // Time-based notifications, created once when a student opens their pages:
        //  - a reminder for events starting within 48 hours
        //  - "certificate available" after an attended event has finished
        public void SyncStudent(int userId)
        {
            var regs = _db.RegistrationsFull().Where(r => r.UserId == userId).ToList();
            var existing = _db.Notifications.Where(n => n.UserId == userId && n.EventId != null).ToList();
            bool changed = false;
            var now = DateTime.Now;

            foreach (var r in regs)
            {
                var ev = r.CollegeEvent;
                if (ev.IsCancelled) continue;

                if (ev.EventDate > now && ev.EventDate <= now.AddHours(48) &&
                    !existing.Any(n => n.Type == "reminder" && n.EventId == ev.Id))
                {
                    _db.Notifications.Add(new Notification
                    {
                        UserId = userId, Type = "reminder", EventId = ev.Id, Link = "/Student/Pass/" + r.Id,
                        Title = "Starting soon: " + ev.Title,
                        Message = $"{ev.EventDate:ddd, dd MMM 'at' hh:mm tt} in {ev.Location}. Keep your QR pass ready."
                    });
                    changed = true;
                }

                if (r.Attended && ev.IsPast && ev.EndDate > now.AddDays(-30) && !existing.Any(n => n.Type == "certificate" && n.EventId == ev.Id))
                {
                    _db.Notifications.Add(new Notification
                    {
                        UserId = userId, Type = "certificate", EventId = ev.Id, Link = "/Student/Certificate/" + r.Id,
                        Title = "Certificate available",
                        Message = $"Your participation certificate for {ev.Title} is ready."
                    });
                    changed = true;
                }
            }
            if (changed) _db.SaveChanges();
        }
    }
}
