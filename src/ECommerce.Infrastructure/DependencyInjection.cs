using ECommerce.Application.Catalog;
using ECommerce.Application.Catalog.Admin;
using ECommerce.Application.Cms;
using ECommerce.Application.Common.Audit;
using ECommerce.Application.Common.Features;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Home;
using ECommerce.Application.Navigation;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Interceptors;
using ECommerce.Infrastructure.Services;
using ECommerce.Application.Admin;
using ECommerce.Application.Cart;
using ECommerce.Application.Orders;
using ECommerce.Application.Payments;
using ECommerce.Application.Wishlist;
using ECommerce.Infrastructure.Services.Admin;
using ECommerce.Infrastructure.Services.Catalog;
using ECommerce.Infrastructure.Services.Cms;
using ECommerce.Infrastructure.Services.Orders;
using ECommerce.Infrastructure.Services.Payments;
using ECommerce.Infrastructure.Services.Shopping;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<IEmailSender, DevEmailSender>();
        services.AddScoped<ISiteSettingsService, SiteSettingsService>();
        services.AddScoped<INavigationService, NavigationService>();
        services.AddScoped<INewsletterService, NewsletterService>();

        services.AddScoped<ICatalogQueryService, CatalogQueryService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ICategoryAdminService, CategoryAdminService>();
        services.AddScoped<IBrandAdminService, BrandAdminService>();
        services.AddScoped<IProductAdminService, ProductAdminService>();

        services.AddScoped<IHomePageService, HomePageService>();
        services.AddScoped<IHomeSectionAdminService, HomeSectionAdminService>();
        services.AddScoped<ISliderAdminService, SliderAdminService>();
        services.AddScoped<ITestimonialAdminService, TestimonialAdminService>();
        services.AddScoped<ISiteSettingsAdminService, SiteSettingsAdminService>();
        services.AddScoped<ISubscriberAdminService, SubscriberAdminService>();
        services.AddScoped<IContactMessageService, ContactMessageService>();
        services.AddScoped<ICmsPageService, CmsPageService>();
        services.AddScoped<ICmsPageAdminService, CmsPageAdminService>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();
        services.AddScoped<IFeatureFlagAdminService, FeatureFlagAdminService>();
        services.AddScoped<IConfigAuditService, ConfigAuditService>();

        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IWishlistService, WishlistService>();

        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<ICouponAdminService, CouponAdminService>();
        services.AddScoped<OrderService>();
        services.AddScoped<IOrderService>(sp => sp.GetRequiredService<OrderService>());
        services.AddScoped<IOrderAdminService, OrderAdminService>();

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IUserAdminService, UserAdminService>();

        var paymentGateway = configuration["Payment:Gateway"] ?? "Fake";
        if (string.Equals(paymentGateway, "SslCommerz", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IPaymentGateway, SslCommerzPaymentGateway>();
        }
        else
        {
            services.AddScoped<IPaymentGateway, FakePaymentGateway>();
        }
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;

                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddClaimsPrincipalFactory<AdditionalUserClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        return services;
    }
}
