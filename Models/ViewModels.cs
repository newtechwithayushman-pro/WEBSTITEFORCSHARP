using System.ComponentModel.DataAnnotations;

namespace CollegeEventManagement.Models
{
    // Data typed into the login form
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Enter your email."), EmailAddress(ErrorMessage = "Enter a valid email address.")] public string Email { get; set; }
        [Required(ErrorMessage = "Enter your password."), DataType(DataType.Password)] public string Password { get; set; }
    }

    // Data typed into the sign-up form (new accounts are always students)
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Enter your full name."), StringLength(80, MinimumLength = 2)] public string FullName { get; set; }
        [Required(ErrorMessage = "Enter your email."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(120)] public string Email { get; set; }
        [Required(ErrorMessage = "Choose your department.")] public string Department { get; set; }
        [Required(ErrorMessage = "Choose a password."), StringLength(100, MinimumLength = 6, ErrorMessage = "Use at least 6 characters."), DataType(DataType.Password)] public string Password { get; set; }
        [Required(ErrorMessage = "Repeat your password."), Compare(nameof(Password), ErrorMessage = "The passwords don't match."), DataType(DataType.Password), Display(Name = "Confirm password")] public string ConfirmPassword { get; set; }
    }

    // Profile page form
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Enter your full name."), StringLength(80, MinimumLength = 2)] public string FullName { get; set; }
        [Required(ErrorMessage = "Choose your department.")] public string Department { get; set; }
        [DataType(DataType.Password)] public string CurrentPassword { get; set; }
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Use at least 6 characters."), DataType(DataType.Password)] public string NewPassword { get; set; }
    }

    // One bar/point of a chart
    public class ChartSeries
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<int> A { get; set; } = new List<int>();
        public List<int> B { get; set; } = new List<int>();
    }

    public class TimelineItem
    {
        public DateTime When { get; set; }
        public string Kind { get; set; }      // registered, attended, certificate
        public string Text { get; set; }
        public int EventId { get; set; }
    }

    public class StudentDashboardVm
    {
        public User Student { get; set; }
        public int TotalRegistrations { get; set; }
        public int Attended { get; set; }
        public int Upcoming { get; set; }
        public int Certificates { get; set; }
        public int AttendancePercent { get; set; }
        public Registration NextUp { get; set; }
        public List<Registration> UpcomingRegistrations { get; set; } = new List<Registration>();
        public List<CollegeEvent> Recommendations { get; set; } = new List<CollegeEvent>();
        public List<TimelineItem> Timeline { get; set; } = new List<TimelineItem>();
        public List<Notification> Notifications { get; set; } = new List<Notification>();
        public ChartSeries Monthly { get; set; } = new ChartSeries();     // A = registered, B = attended
        public ChartSeries ByCategory { get; set; } = new ChartSeries();  // A = count
        public List<object> CalendarEvents { get; set; } = new List<object>();
    }

    public class OrganizerDashboardVm
    {
        public List<CollegeEvent> Events { get; set; } = new List<CollegeEvent>();
        public int UpcomingCount { get; set; }
        public int TotalRegistrations { get; set; }
        public int TotalCheckIns { get; set; }
        public int AttendancePercent { get; set; }
        public ChartSeries PerEvent { get; set; } = new ChartSeries();   // A = registered, B = checked in
        public ChartSeries Trend { get; set; } = new ChartSeries();      // A = registrations per day
        public bool IsAdminView { get; set; }
    }

    public class AdminDashboardVm
    {
        public int Students { get; set; }
        public int Organizers { get; set; }
        public int Events { get; set; }
        public int UpcomingEvents { get; set; }
        public int Registrations { get; set; }
        public int CheckIns { get; set; }
        public int AttendancePercent { get; set; }
        public ChartSeries Monthly { get; set; } = new ChartSeries();      // A = registered, B = attended
        public ChartSeries ByCategory { get; set; } = new ChartSeries();   // A = events, B = registrations
        public ChartSeries TopEvents { get; set; } = new ChartSeries();    // A = registrations
        public ChartSeries Roles { get; set; } = new ChartSeries();        // A = users
        public List<TimelineItem> Recent { get; set; } = new List<TimelineItem>();
    }

    public class ParticipantsVm
    {
        public CollegeEvent Event { get; set; }
        public List<Registration> Registrations { get; set; } = new List<Registration>();
        public int CheckedIn { get; set; }
    }
}

namespace CollegeEventManagement.Models
{
    public class HomeVm
    {
        public CollegeEvent Next { get; set; }
        public List<CollegeEvent> Featured { get; set; } = new List<CollegeEvent>();
        public List<CollegeEvent> Upcoming { get; set; } = new List<CollegeEvent>();
        public Dictionary<string, int> CategoryCounts { get; set; } = new Dictionary<string, int>();
        public List<KeyValuePair<string, int>> Hosts { get; set; } = new List<KeyValuePair<string, int>>();
        public int EventsHosted { get; set; }
        public int Registrations { get; set; }
        public int CheckIns { get; set; }
        public int Students { get; set; }
        public int AttendancePercent { get; set; }
        public ChartSeries Monthly { get; set; } = new ChartSeries();
    }
}
