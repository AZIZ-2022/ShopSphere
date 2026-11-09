using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Web.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Layer registrations.
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<ECommerce.Web.Infrastructure.IMoneyFormatter, ECommerce.Web.Infrastructure.MoneyFormatter>();
builder.Services.AddScoped<ECommerce.Web.Infrastructure.ICartOwnerAccessor, ECommerce.Web.Infrastructure.CartOwnerAccessor>();

builder.Services.AddControllersWithViews();

// Identity uses cookie auth; point the paths at the Account controller.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;

    // AJAX endpoints under /api/* get a 401/403 instead of an HTML redirect.
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

var app = builder.Build();

// Unhandled exceptions: JSON for /api/*, a friendly page for everything else.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiResponse.Fail("An unexpected error occurred."));
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(
            "An unexpected error occurred. Check application logs."
        );
    }
}));

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Non-success status codes with no body (e.g. 404) render the friendly error page
// while keeping the original URL and status code. /api/* is skipped below.
app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/error/{0}"));

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Attribute-routed controllers (e.g. /api/*, /preferences/*).
app.MapControllers();

// Optional: create/seed the database on startup. Off by default — the shipped
// database comes from importing Database/ECommerceDB.sql.
var dbInit = app.Configuration.GetSection("DatabaseInitialization");
await DbInitializer.RunAsync(
    app.Services,
    migrate: dbInit.GetValue<bool>("MigrateOnStartup"),
    seed: dbInit.GetValue<bool>("SeedOnStartup"));

app.Run();
