namespace CollegeEventManagement.Models
{
    // One row per checked-in registration (a registration can only be checked in once).
    public class Attendance
    {
        public int Id { get; set; }
        public int RegistrationId { get; set; }
        public Registration Registration { get; set; }
        public DateTime CheckedInAt { get; set; } = DateTime.Now;
        public string Method { get; set; } = "QR";      // "QR" or "Manual"
        public int? CheckedInById { get; set; }          // organiser/admin who recorded it
    }
}
