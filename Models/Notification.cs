namespace CollegeEventManagement.Models
{
    // An in-app notification (shown in the bell menu and the notifications page).
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string Type { get; set; } = "info";   // registration, reminder, attendance, update, cancelled, certificate, info
        public string Title { get; set; }
        public string Message { get; set; }
        public string Link { get; set; }
        public int? EventId { get; set; }            // used to avoid sending the same reminder twice
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public bool IsRead { get; set; }
    }
}
