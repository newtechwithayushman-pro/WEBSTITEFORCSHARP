using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.Controllers
{
    // Shared base class: gives every controller easy access to the signed-in user.
    public abstract class AppController : Controller
    {
        protected int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        protected bool IsAdmin => User.IsInRole("Admin");
    }
}
