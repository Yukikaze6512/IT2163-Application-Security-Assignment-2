using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ApplicationSecurity.Data;
using ApplicationSecurity.Models;
using ApplicationSecurity.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

// Configure MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString!, ServerVersion.AutoDetect(connectionString!)));

// Data Protection (for NRIC encryption)
builder.Services.AddDataProtection();

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings - min 12 chars, upper, lower, digit, special char
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;

    // Lockout settings - 3 failed attempts, lockout for 15 minutes
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/Errors/403";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Register application services
builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddHttpClient<ReCaptchaService>();
builder.Services.AddSingleton<EmailService>();

// Anti-forgery (CSRF protection)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// Apply pending migrations and clear session table on startup (in-memory sessions are lost on restart)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    // Clear UserSessions so dashboard does not show stale "active" sessions after app restart
    db.UserSessions.ExecuteDelete();
}

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Custom error pages for status codes (404, 403, etc.)
app.UseStatusCodePagesWithReExecute("/StatusCode/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Custom middleware: session validation + password age check
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Skip middleware for certain paths
        if (path == "/logout" || path.StartsWith("/statuscode") || path == "/error")
        {
            await next();
            return;
        }

        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
        var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        
        var user = await userManager.GetUserAsync(context.User);

        if (user == null)
        {
            await signInManager.SignOutAsync();
            context.Response.Redirect("/Login");
            return;
        }

        // Check session validity (Active UserSession)
        var sessionId = context.Session.GetString("SessionId");
        if (string.IsNullOrEmpty(sessionId))
        {
            // Session expired
            await signInManager.SignOutAsync();
            context.Response.Redirect("/Login?message=session_expired");
            return;
        }

        var userSession = await dbContext.UserSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == user.Id && s.IsActive);

        if (userSession == null)
        {
            // Session invalid or logged out elsewhere (if we forced logout)
            // But here we allow multiple, so we just check if THIS session is active.
            await signInManager.SignOutAsync();
            context.Session.Clear();
            context.Response.Redirect("/Login?message=session_expired");
            return;
        }

        // Update LastActiveAt
        userSession.LastActiveAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        // Check maximum password age (must change every 90 days)
        if (path != "/changepassword")
        {
            if (user.LastPasswordChangeDate.HasValue)
            {
                var passwordAge = DateTime.UtcNow - user.LastPasswordChangeDate.Value;
                if (passwordAge.TotalDays > 90)
                {
                    context.Response.Redirect("/ChangePassword?message=password_expired");
                    return;
                }
            }
        }
    }

    await next();
});

app.MapRazorPages();

app.Run();
