using System.Security.Claims;
using CollegeEventManagement.Data;
using CollegeEventManagement.Models;
using CollegeEventManagement.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.Controllers
{
    // Sign in, sign up, profile and notifications
    public class AccountController : AppController
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notifier;
        private readonly PasswordHasher<User> _hasher = new PasswordHasher<User>();
        public AccountController(AppDbContext db, NotificationService notifier) { _db = db; _notifier = notifier; }

        // Each role has its own home page
        private IActionResult HomeFor(string role) => role switch
        {
            Roles.Admin => RedirectToAction("Dashboard", "Admin"),
            Roles.Organizer => RedirectToAction("Dashboard", "Organizer"),
            _ => RedirectToAction("Dashboard", "Student")
        };

        private async Task SignInAsync(User user)
        {
            // Store the user's id, name and role inside the login cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated) return HomeFor(User.IsInRole(Roles.Admin) ? Roles.Admin : User.IsInRole(Roles.Organizer) ? Roles.Organizer : Roles.Student);
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);

            // Find the user by email, then check the typed password against the stored hash
            var email = model.Email.Trim();
            var user = _db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null ||
                _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Email or password is incorrect. Check both and try again.");
                return View(model);
            }

            await SignInAsync(user);
            TempData["Success"] = $"Welcome back, {user.FullName.Split(' ')[0]}.";

            // Go back to the page the user wanted (only if it is a local URL)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
            return HomeFor(user.Role);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View(new RegisterViewModel());
        }

        // New accounts are always students. Organisers and admins are created by an administrator.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!EventCategories.Departments.Contains(model.Department ?? ""))
                ModelState.AddModelError(nameof(model.Department), "Choose one of the listed departments.");
            var email = (model.Email ?? "").Trim();
            if (ModelState.IsValid && _db.Users.Any(u => u.Email == email))
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists. Try signing in.");
            if (!ModelState.IsValid) return View(model);

            var user = new User { FullName = model.FullName.Trim(), Email = email, Role = Roles.Student, Department = model.Department };
            user.PasswordHash = _hasher.HashPassword(user, model.Password);
            _db.Users.Add(user);
            _db.SaveChanges();

            await SignInAsync(user);
            TempData["Success"] = "Your account is ready. Find an event and save a seat.";
            return RedirectToAction("Dashboard", "Student");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "You have been signed out.";
            return RedirectToAction("Index", "Home");
        }

        // ----- Profile -----
        [Authorize, HttpGet]
        public IActionResult Profile()
        {
            var user = _db.Users.Find(CurrentUserId);
            if (user == null) return NotFound();
            ViewBag.Email = user.Email; ViewBag.Role = user.Role; ViewBag.Joined = user.JoinedOn;
            return View(new ProfileViewModel { FullName = user.FullName, Department = user.Department });
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = _db.Users.Find(CurrentUserId);
            if (user == null) return NotFound();
            ViewBag.Email = user.Email; ViewBag.Role = user.Role; ViewBag.Joined = user.JoinedOn;

            if (!EventCategories.Departments.Contains(model.Department ?? ""))
                ModelState.AddModelError(nameof(model.Department), "Choose one of the listed departments.");

            bool changingPassword = !string.IsNullOrEmpty(model.NewPassword);
            if (changingPassword &&
                (string.IsNullOrEmpty(model.CurrentPassword) ||
                 _hasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword) == PasswordVerificationResult.Failed))
                ModelState.AddModelError(nameof(model.CurrentPassword), "Enter your current password to set a new one.");

            if (!ModelState.IsValid) return View(model);

            user.FullName = model.FullName.Trim();
            user.Department = model.Department;
            if (changingPassword) user.PasswordHash = _hasher.HashPassword(user, model.NewPassword);
            _db.SaveChanges();

            await SignInAsync(user);   // refresh the name stored in the cookie
            TempData["Success"] = changingPassword ? "Profile and password updated." : "Profile updated.";
            return RedirectToAction("Profile");
        }

        // ----- Notifications (in-app only) -----
        [Authorize]
        public IActionResult Notifications()
        {
            var uid = CurrentUserId;
            if (User.IsInRole(Roles.Student)) _notifier.SyncStudent(uid);
            var list = _db.Notifications.Where(n => n.UserId == uid).OrderByDescending(n => n.CreatedOn).Take(100).ToList();
            return View(list);
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public IActionResult MarkRead(int id, string returnUrl = null)
        {
            var uid = CurrentUserId;
            var n = _db.Notifications.FirstOrDefault(x => x.Id == id && x.UserId == uid);
            if (n != null) { n.IsRead = true; _db.SaveChanges(); }
            if (n != null && !string.IsNullOrEmpty(n.Link) && Url.IsLocalUrl(n.Link)) return LocalRedirect(n.Link);
            return RedirectToAction("Notifications");
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public IActionResult MarkAllRead()
        {
            var uid = CurrentUserId;
            foreach (var n in _db.Notifications.Where(x => x.UserId == uid && !x.IsRead).ToList()) n.IsRead = true;
            _db.SaveChanges();
            TempData["Success"] = "All notifications marked as read.";
            return RedirectToAction("Notifications");
        }
    }
}
