using System.ComponentModel.DataAnnotations;

namespace CollegeEventManagement.Models
{
    // A college event that students can register for.
    public class CollegeEvent
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; }

        [Required, StringLength(2000)]
        public string Description { get; set; }

        [Required, StringLength(100), Display(Name = "Venue")]
        public string Location { get; set; }

        [Required, Display(Name = "Date & time")]
        public DateTime EventDate { get; set; } = DateTime.Today.AddDays(14).AddHours(10);

        [Range(15, 1440, ErrorMessage = "Duration must be between 15 minutes and 24 hours.")]
        public int DurationMinutes { get; set; } = 120;

        // One of EventCategories.All (used for filters, colours and charts)
        [Required, StringLength(30)]
        public string Category { get; set; } = "Technical";

        [Range(1, 5000, ErrorMessage = "Capacity must be between 1 and 5000.")]
        public int Capacity { get; set; } = 100;

        [Required, Display(Name = "Registration deadline")]
        public DateTime RegistrationDeadline { get; set; } = DateTime.Today.AddDays(12).AddHours(23);

        // The club, department or committee shown as the organiser
        [Required, StringLength(100), Display(Name = "Organizer / host")]
        public string Host { get; set; }

        [StringLength(1000)] public string Requirements { get; set; }

        // One item per line, e.g. "10:00 Opening ceremony"
        [StringLength(1500)] public string Schedule { get; set; }

        public bool IsCancelled { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        // The organiser account that owns this event
        public int OrganizerId { get; set; }
        public User Organizer { get; set; }

        public List<Registration> Registrations { get; set; } = new List<Registration>();

        // ----- Read-only helpers (not stored). Load Registrations first. -----
        public DateTime EndDate => EventDate.AddMinutes(DurationMinutes);
        public int SeatsLeft => Math.Max(0, Capacity - Registrations.Count);
        public bool IsFull => Registrations.Count >= Capacity;
        public bool IsPast => EndDate < DateTime.Now;
        public bool RegistrationOpen => !IsCancelled && !IsFull && RegistrationDeadline >= DateTime.Now && EventDate > DateTime.Now;

        // Colour family for the status badge: ok, warn, danger, info or muted
        public string StatusTone => Status switch
        {
            "Open" => "ok", "Closing soon" => "warn", "Full" => "danger", "Cancelled" => "danger", "Ongoing" => "info", _ => "muted"
        };

        // A single label for the UI
        public string Status
        {
            get
            {
                if (IsCancelled) return "Cancelled";
                if (IsPast) return "Completed";
                if (EventDate <= DateTime.Now) return "Ongoing";
                if (IsFull) return "Full";
                if (RegistrationDeadline < DateTime.Now) return "Registration closed";
                if (RegistrationDeadline <= DateTime.Now.AddDays(2)) return "Closing soon";
                return "Open";
            }
        }
    }
}
