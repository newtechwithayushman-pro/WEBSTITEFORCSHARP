using CollegeEventManagement.Data;
using CollegeEventManagement.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Enable MVC (Controllers + Views)
builder.Services.AddControllersWithViews();

// The QR scanner page sends the anti-forgery token in this header when it calls the check-in API
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

// Database: "Sqlite" (default, no installation needed) or "SqlServer" (LocalDB). Set in appsettings.json.
var provider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString(provider);
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (provider == "SqlServer") options.UseSqlServer(connectionString);
    else options.UseSqlite(connectionString);
});

builder.Services.AddScoped<NotificationService>();

// Cookie login: users who are not logged in are sent to /Account/Login
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Home/Error/403";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Create the database (if missing) and add sample data on first run.
// If this fails, show a readable page instead of silently shutting down.
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedData.Initialize(db);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "CAMPUSLY could not open its database.");
    var html = "<!doctype html><meta charset=utf-8><title>CAMPUSLY - database error</title>" +
        "<body style='font-family:system-ui,sans-serif;max-width:720px;margin:4rem auto;padding:0 1rem;line-height:1.6'>" +
        "<h1>CAMPUSLY couldn't open its database</h1>" +
        $"<p>Provider in use: <b>{System.Net.WebUtility.HtmlEncode(provider)}</b>.</p>" +
        "<p>Fix: open <code>appsettings.json</code> and set <code>\"DatabaseProvider\": \"Sqlite\"</code> (needs nothing installed), " +
        "or install SQL Server LocalDB and check the <code>SqlServer</code> connection string. Then run again.</p>" +
        $"<pre style='white-space:pre-wrap;background:#f3f3f3;padding:1rem;border-radius:8px'>{System.Net.WebUtility.HtmlEncode(ex.Message)}</pre></body>";
    app.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html);
    });
    app.Run();
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Friendly page for 404 / 403 etc. (handled by HomeController.Error)
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.UseStaticFiles();      // Serves wwwroot (css, js, fonts, libraries)
app.UseRouting();
app.UseAuthentication();   // Who is the user?
app.UseAuthorization();    // Are they allowed in?

// Default page: public home page
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
