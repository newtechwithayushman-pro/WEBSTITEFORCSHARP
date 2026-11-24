using CollegeEventManagement.Models;
using CollegeEventManagement.Services;
using Microsoft.AspNetCore.Identity;

namespace CollegeEventManagement.Data
{
    // Adds sample (demo) data the first time the app runs.
    // Dates are relative to "today", so there are always upcoming events and a few months of history for the charts.
    public static class SeedData
    {
        public static void Initialize(AppDbContext db)
        {
            if (db.Users.Any()) return;   // Already seeded

            var hasher = new PasswordHasher<User>();
            User Make(string name, string email, string password, string role, string dept)
            {
                var u = new User { FullName = name, Email = email, Role = role, Department = dept, JoinedOn = DateTime.Now.AddDays(-160) };
                u.PasswordHash = hasher.HashPassword(u, password);
                return u;
            }

            var admin = Make("Campus Admin", "admin@campusly.edu", "admin123", Roles.Admin, "Other");
            var orgTech = Make("Neha Kapoor", "tech@campusly.edu", "organizer123", Roles.Organizer, "Computer Science");
            var orgCulture = Make("Arjun Malhotra", "culture@campusly.edu", "organizer123", Roles.Organizer, "Design and Arts");
            var orgSports = Make("Harpreet Gill", "sports@campusly.edu", "organizer123", Roles.Organizer, "Other");
            var students = new List<User>
            {
                Make("Rahul Sharma", "rahul@campusly.edu", "student123", Roles.Student, "Computer Science"),
                Make("Priya Singh", "priya@campusly.edu", "student123", Roles.Student, "Electronics"),
                Make("Aman Verma", "aman@campusly.edu", "student123", Roles.Student, "Mechanical"),
                Make("Simran Kaur", "simran@campusly.edu", "student123", Roles.Student, "Design and Arts"),
                Make("Karan Mehta", "karan@campusly.edu", "student123", Roles.Student, "Management"),
                Make("Ananya Gupta", "ananya@campusly.edu", "student123", Roles.Student, "Computer Science"),
                Make("Vikram Rao", "vikram@campusly.edu", "student123", Roles.Student, "Civil"),
                Make("Meera Nair", "meera@campusly.edu", "student123", Roles.Student, "Sciences"),
                Make("Rohan Das", "rohan@campusly.edu", "student123", Roles.Student, "Electronics"),
                Make("Ishita Jain", "ishita@campusly.edu", "student123", Roles.Student, "Management"),
                Make("Gurpreet Kaur", "gurpreet@campusly.edu", "student123", Roles.Student, "Computer Science"),
                Make("Dev Patel", "dev@campusly.edu", "student123", Roles.Student, "Mechanical"),
            };
            db.Users.AddRange(admin, orgTech, orgCulture, orgSports);
            db.Users.AddRange(students.ToArray());
            db.SaveChanges();

            var today = DateTime.Today;
            CollegeEvent E(string title, string cat, string host, User org, string venue, int day, int hour, int mins, int cap, int deadlineDay, string desc, string req, string sched)
            {
                var date = today.AddDays(day).AddHours(hour);
                return new CollegeEvent
                {
                    Title = title, Category = cat, Host = host, OrganizerId = org.Id, Location = venue, EventDate = date,
                    DurationMinutes = mins, Capacity = cap, RegistrationDeadline = today.AddDays(deadlineDay).AddHours(23),
                    Description = desc, Requirements = req, Schedule = sched, CreatedOn = date.AddDays(-30)
                };
            }

            var events = new List<CollegeEvent>
            {
                // ---- past events (history for the charts and certificates) ----
                E("HackNova 24-Hour Hackathon", "Hackathons", "Technical Club", orgTech, "Innovation Lab", -138, 9, 1440, 60, -140, "A full day and night of building. Teams of up to four ship a working prototype and demo it to a panel of mentors.", "Laptop and charger. Teams of 2 to 4.", "09:00 Kick-off and problem statements\n13:00 Mentor round\n21:00 Midnight check-in\n09:00 Demos and judging"),
                E("Intro to Machine Learning Workshop", "Workshops", "Technical Club", orgTech, "Computer Lab 2", -116, 14, 180, 40, -118, "Hands-on beginner session: train, evaluate and improve your first model in Python.", "Basic Python. Bring a laptop.", null),
                E("Freshers' Cultural Evening", "Cultural", "Cultural Committee", orgCulture, "Open Air Theatre", -98, 18, 180, 250, -100, "Music, dance and drama to welcome the new batch.", null, null),
                E("Inter-Department Football League", "Sports", "Sports Council", orgSports, "College Ground", -80, 8, 300, 120, -82, "Eight departments, one trophy. Knock-out format over a single day.", "Sports shoes. Team lists submitted in advance.", null),
                E("Startup Pitch Day", "Entrepreneurship", "Entrepreneurship Cell", orgCulture, "Seminar Hall A", -62, 11, 240, 90, -64, "Student founders pitch to alumni investors and get live feedback.", null, "11:00 Welcome\n11:30 Pitches\n14:00 Investor Q and A"),
                E("RoboRace Qualifiers", "Robotics", "Robotics Society", orgTech, "Mechanical Workshop", -47, 10, 300, 50, -49, "Line-following robots compete for a place in the inter-college finals.", "Robot must be under 2 kg.", null),
                E("Digital Art Exhibition", "Arts", "Department of Design and Arts", orgCulture, "Art Gallery", -33, 10, 360, 150, -35, "Student digital art, animation and photography on display.", null, null),
                E("Cloud Computing Seminar", "Seminars", "Technical Club", orgTech, "Seminar Hall B", -19, 15, 120, 80, -21, "An industry engineer explains how modern applications are deployed and scaled in the cloud.", null, null),
                E("Annual Sports Relay", "Sports", "Sports Council", orgSports, "College Ground", -8, 8, 240, 120, -10, "Inter-department relay races on the college track.", "Sports shoes.", null),

                // ---- upcoming events ----
                E("Open Mic Evening", "Cultural", "Cultural Committee", orgCulture, "Library Lawn", 1, 17, 150, 8, 0, "Poetry, stand-up and acoustic sets. Intimate seating, so only eight seats.", null, "17:00 Doors open\n17:30 First set\n19:00 Wrap-up"),
                E(".NET Web Development Workshop", "Workshops", "Technical Club", orgTech, "Computer Lab 2", 4, 14, 180, 40, 2, "Build an MVC application end to end: controllers, Razor views and Entity Framework Core.", "Laptop with Visual Studio 2022 and the .NET 8 SDK.", "14:00 MVC fundamentals\n15:00 Razor and tag helpers\n16:00 Database with EF Core"),
                E("Annual Tech Fest", "Technical", "Technical Club", orgTech, "Main Auditorium", 7, 10, 480, 150, 5, "Coding contests, robotics demos and project exhibitions across the day. Teams of up to three can enter the contests.", "College ID card.", "10:00 Inauguration\n11:00 Coding contest\n13:30 Project exhibition\n16:00 Prize distribution"),
                E("Cybersecurity Seminar", "Seminars", "Technical Club", orgTech, "Seminar Hall B", 10, 11, 120, 80, 8, "How real attacks work and how to defend against them, with a live demo.", null, null),
                E("Cultural Night", "Cultural", "Cultural Committee", orgCulture, "Open Air Theatre", 14, 18, 240, 300, 12, "Music, dance and drama performances by students, with an open stage for last-minute acts.", null, "18:00 Opening act\n19:00 Dance showcase\n20:00 Drama\n21:00 Headline band"),
                E("Career Guidance Workshop", "Workshops", "Training and Placement Cell", orgTech, "Seminar Hall A", 17, 11, 180, 60, 15, "Resume tips and mock interviews with industry experts.", "Printed resume.", null),
                E("Inter-College Cricket Match", "Sports", "Sports Council", orgSports, "College Ground", 22, 9, 420, 200, 20, "Friendly cricket match against a neighbouring college. Come and cheer for the home team.", null, null),
                E("Robo Wars", "Robotics", "Robotics Society", orgTech, "Mechanical Workshop", 28, 10, 360, 60, 25, "Combat robots, head to head. Weight classes from 1 kg to 8 kg.", "Safety briefing is mandatory for participants.", null),
                E("Entrepreneurship Summit", "Entrepreneurship", "Entrepreneurship Cell", orgCulture, "Main Auditorium", 35, 10, 360, 120, 32, "Founders, investors and students on building something from nothing.", null, null),
                E("HackNova 2.0: 24-Hour Hackathon", "Hackathons", "Technical Club", orgTech, "Innovation Lab", 49, 9, 1440, 80, 45, "Build a working prototype in 24 hours with mentors on call. Teams of up to four, any stack, demos to a judging panel.", "Laptop and charger. Teams of 2 to 4.", "09:00 Kick-off and problem statements\n13:00 Mentor round\n21:00 Midnight check-in\n09:00 Demos and judging"),
                E("Design Sprint Weekend", "Arts", "Department of Design and Arts", orgCulture, "Art Gallery", 42, 9, 720, 50, 40, "Two intense days of rapid prototyping for social-impact ideas.", "Bring sketching material.", null),
            };
            db.CollegeEvents.AddRange(events.ToArray());
            db.SaveChanges();

            // ---- registrations, attendance and notifications ----
            var rnd = new Random(2026);
            double Chance(User s) => s.Email.StartsWith("rahul") ? 0.9 : s.Email.StartsWith("priya") ? 0.8 : 0.5;
            var rahulNotIn = new HashSet<string> { "Open Mic Evening", ".NET Web Development Workshop", "Cybersecurity Seminar", "Career Guidance Workshop", "Inter-College Cricket Match", "Entrepreneurship Summit", "Design Sprint Weekend" };

            foreach (var ev in events)
            {
                bool past = ev.EventDate < DateTime.Now;
                IEnumerable<User> chosen;
                if (ev.Title == "Open Mic Evening")
                    chosen = students.Where(s => !s.Email.StartsWith("rahul")).OrderBy(s => rnd.Next()).Take(8).ToList();   // full event
                else
                    chosen = students.Where(s => !(s.Email.StartsWith("rahul") && rahulNotIn.Contains(ev.Title)) && rnd.NextDouble() < (past ? Chance(s) + 0.15 : Chance(s))).ToList();

                foreach (var s in chosen.Take(ev.Capacity))
                {
                    var reg = new Registration
                    {
                        UserId = s.Id, CollegeEventId = ev.Id, QrToken = TokenService.NewToken(),
                        RegisteredOn = past ? ev.EventDate.AddDays(-rnd.Next(3, 15)).AddHours(rnd.Next(0, 10)) : DateTime.Now.AddDays(-rnd.Next(1, 60)).AddHours(-rnd.Next(0, 12))
                    };
                    if (reg.RegisteredOn > DateTime.Now) reg.RegisteredOn = DateTime.Now.AddHours(-rnd.Next(1, 40));
                    db.Registrations.Add(reg);
                    db.SaveChanges();

                    if (past && rnd.NextDouble() < (s.Email.StartsWith("rahul") ? 0.85 : 0.78))
                    {
                        db.Attendances.Add(new Attendance { RegistrationId = reg.Id, CheckedInAt = ev.EventDate.AddMinutes(rnd.Next(-10, 40)), Method = rnd.NextDouble() < 0.85 ? "QR" : "Manual", CheckedInById = ev.OrganizerId });
                        db.Notifications.Add(new Notification { UserId = s.Id, Type = "attendance", EventId = ev.Id, Title = "Attendance marked", Message = "You were checked in at " + ev.Title + ".", CreatedOn = ev.EventDate, IsRead = true, Link = "/Student/Activities" });
                    }
                    else if (!past)
                    {
                        db.Notifications.Add(new Notification { UserId = s.Id, Type = "registration", EventId = ev.Id, Title = "Registration confirmed", Message = "You're registered for " + ev.Title + ". Your QR pass is ready.", CreatedOn = reg.RegisteredOn, IsRead = rnd.NextDouble() < 0.5, Link = "/Student/Pass/" + reg.Id });
                    }
                }
                db.SaveChanges();
            }

            // One welcome-style notice for each organiser
            foreach (var o in new[] { orgTech, orgCulture, orgSports })
                db.Notifications.Add(new Notification { UserId = o.Id, Type = "info", Title = "Welcome to CAMPUSLY", Message = "Create events, scan QR passes and watch registrations from your dashboard.", Link = "/Organizer/Dashboard" });
            db.SaveChanges();
        }
    }
}
