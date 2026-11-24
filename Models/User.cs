using System.ComponentModel.DataAnnotations;

namespace CollegeEventManagement.Models
{
    // A person who can log in. Role is "Student", "Organizer" or "Admin".
    public class User
    {
        public int Id { get; set; }
        [Required, StringLength(80)] public string FullName { get; set; }
        [Required, StringLength(120)] public string Email { get; set; }
        [Required] public string PasswordHash { get; set; }   // Hashed with ASP.NET Core's PasswordHasher
        [Required] public string Role { get; set; }
        [StringLength(80)] public string Department { get; set; }
        public DateTime JoinedOn { get; set; } = DateTime.Now;
    }

    public static class Roles
    {
        public const string Student = "Student";
        public const string Organizer = "Organizer";
        public const string Admin = "Admin";
        public static readonly string[] All = { Student, Organizer, Admin };
    }
}
