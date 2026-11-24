namespace CollegeEventManagement.Models
{
    // Links ONE student to ONE event. QrToken is the secret that the student's QR pass carries.
    public class Registration
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int CollegeEventId { get; set; }
        public CollegeEvent CollegeEvent { get; set; }
        public DateTime RegisteredOn { get; set; } = DateTime.Now;
        public string QrToken { get; set; }

        // Present once the student has been checked in at the event
        public Attendance Attendance { get; set; }
        public bool Attended => Attendance != null;
    }
}
