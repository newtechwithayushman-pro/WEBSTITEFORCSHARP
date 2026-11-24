using System.Security.Claims;
using CollegeEventManagement.Data;
using CollegeEventManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace CollegeEventManagement.ViewComponents
{
    // The bell icon with the unread badge and the latest notifications. Used by both layouts.
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly AppDbContext _db;
        public NotificationBellViewComponent(AppDbContext db) { _db = db; }

        public IViewComponentResult Invoke()
        {
            var claim = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claim == null) return Content("");
            int uid = int.Parse(claim);

            ViewBag.Unread = _db.Notifications.Count(n => n.UserId == uid && !n.IsRead);
            var recent = _db.Notifications.Where(n => n.UserId == uid).OrderByDescending(n => n.CreatedOn).Take(5).ToList();
            return View(recent);
        }
    }
}
