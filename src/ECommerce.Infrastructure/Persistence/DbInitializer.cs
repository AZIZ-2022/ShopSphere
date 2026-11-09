using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence;

/// <summary>
/// Optional startup helper. Disabled by default (see <c>DatabaseInitialization</c> in appsettings)
/// because the delivered database is created by importing <c>Database/ECommerceDB.sql</c>.
/// Both operations are safe to run against that database: migrations become a no-op and
/// seeding is skipped when data already exists.
/// </summary>
public static class DbInitializer
{
    public static async Task RunAsync(IServiceProvider services, bool migrate, bool seed)
    {
        if (!migrate && !seed) return;

        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        var context = sp.GetRequiredService<ApplicationDbContext>();

        if (migrate)
        {
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
        }

        if (seed)
        {
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
            await ApplicationDbContextSeed.SeedAsync(context, userManager, roleManager, logger);
        }
    }
}
