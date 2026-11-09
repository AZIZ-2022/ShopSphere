using System.Text;
using ECommerce.Application.Cms;
using ECommerce.Application.Common.Audit;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Home;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Cms;

// ==================================================================== Sliders

public sealed class SliderAdminService : ISliderAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IHomePageService _home;

    public SliderAdminService(ApplicationDbContext db, IHomePageService home)
    {
        _db = db;
        _home = home;
    }

    public async Task<IReadOnlyList<SliderEditModel>> ListAsync(CancellationToken ct = default) =>
        await _db.Sliders.AsNoTracking()
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Id)
            .Select(s => new SliderEditModel
            {
                Id = s.Id, Title = s.Title, Subtitle = s.Subtitle, ImageUrl = s.ImageUrl,
                MobileImageUrl = s.MobileImageUrl, LinkUrl = s.LinkUrl, ButtonText = s.ButtonText,
                DisplayOrder = s.DisplayOrder, IsActive = s.IsActive,
                StartsAt = s.StartsAt, EndsAt = s.EndsAt
            })
            .ToListAsync(ct);

    public async Task<SliderEditModel?> GetAsync(int id, CancellationToken ct = default) =>
        (await ListAsync(ct)).FirstOrDefault(s => s.Id == id);

    public async Task<Result<int>> CreateAsync(SliderEditModel m, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(m.Title)) return Result<int>.Failure("Title is required.");
        if (string.IsNullOrWhiteSpace(m.ImageUrl)) return Result<int>.Failure("Image URL is required.");

        var entity = new Slider
        {
            Title = m.Title.Trim(), Subtitle = m.Subtitle?.Trim(), ImageUrl = m.ImageUrl.Trim(),
            MobileImageUrl = m.MobileImageUrl?.Trim(), LinkUrl = m.LinkUrl?.Trim(),
            ButtonText = m.ButtonText?.Trim(), DisplayOrder = m.DisplayOrder, IsActive = m.IsActive,
            StartsAt = m.StartsAt, EndsAt = m.EndsAt
        };
        _db.Sliders.Add(entity);
        await _db.SaveChangesAsync(ct);
        _home.Invalidate();
        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> UpdateAsync(SliderEditModel m, CancellationToken ct = default)
    {
        var e = await _db.Sliders.FirstOrDefaultAsync(x => x.Id == m.Id, ct);
        if (e is null) return Result.Failure("Slider not found.");
        if (string.IsNullOrWhiteSpace(m.Title)) return Result.Failure("Title is required.");
        if (string.IsNullOrWhiteSpace(m.ImageUrl)) return Result.Failure("Image URL is required.");

        e.Title = m.Title.Trim(); e.Subtitle = m.Subtitle?.Trim(); e.ImageUrl = m.ImageUrl.Trim();
        e.MobileImageUrl = m.MobileImageUrl?.Trim(); e.LinkUrl = m.LinkUrl?.Trim();
        e.ButtonText = m.ButtonText?.Trim(); e.DisplayOrder = m.DisplayOrder; e.IsActive = m.IsActive;
        e.StartsAt = m.StartsAt; e.EndsAt = m.EndsAt;
        await _db.SaveChangesAsync(ct);
        _home.Invalidate();
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.Sliders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return Result.Failure("Slider not found.");
        _db.Sliders.Remove(e);
        await _db.SaveChangesAsync(ct);
        _home.Invalidate();
        return Result.Success();
    }
}

// =============================================================== Testimonials

public sealed class TestimonialAdminService : ITestimonialAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IHomePageService _home;

    public TestimonialAdminService(ApplicationDbContext db, IHomePageService home)
    {
        _db = db;
        _home = home;
    }

    public async Task<IReadOnlyList<TestimonialEditModel>> ListAsync(CancellationToken ct = default) =>
        await _db.Testimonials.AsNoTracking()
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Id)
            .Select(t => new TestimonialEditModel
            {
                Id = t.Id, CustomerName = t.CustomerName, Designation = t.Designation, PhotoUrl = t.PhotoUrl,
                Message = t.Message, Rating = t.Rating, DisplayOrder = t.DisplayOrder, IsActive = t.IsActive
            })
            .ToListAsync(ct);

    public async Task<TestimonialEditModel?> GetAsync(int id, CancellationToken ct = default) =>
        (await ListAsync(ct)).FirstOrDefault(t => t.Id == id);

    public async Task<Result<int>> CreateAsync(TestimonialEditModel m, CancellationToken ct = default)
    {
        var err = Validate(m);
        if (err is not null) return Result<int>.Failure(err);

        var e = new Testimonial
        {
            CustomerName = m.CustomerName.Trim(), Designation = m.Designation?.Trim(), PhotoUrl = m.PhotoUrl?.Trim(),
            Message = m.Message.Trim(), Rating = Math.Clamp(m.Rating, (byte)1, (byte)5),
            DisplayOrder = m.DisplayOrder, IsActive = m.IsActive
        };
        _db.Testimonials.Add(e);
        await _db.SaveChangesAsync(ct);
        _home.Invalidate();
        return Result<int>.Success(e.Id);
    }

    public async Task<Result> UpdateAsync(TestimonialEditModel m, CancellationToken ct = default)
    {
        var e = await _db.Testimonials.FirstOrDefaultAsync(x => x.Id == m.Id, ct);
        if (e is null) return Result.Failure("Testimonial not found.");
        var err = Validate(m);
        if (err is not null) return Result.Failure(err);

        e.CustomerName = m.CustomerName.Trim(); e.Designation = m.Designation?.Trim(); e.PhotoUrl = m.PhotoUrl?.Trim();
        e.Message = m.Message.Trim(); e.Rating = Math.Clamp(m.Rating, (byte)1, (byte)5);
        e.DisplayOrder = m.DisplayOrder; e.IsActive = m.IsActive;
        await _db.SaveChangesAsync(ct);
        _home.Invalidate();
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.Testimonials.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return Result.Failure("Testimonial not found.");
        _db.Testimonials.Remove(e);
        await _db.SaveChangesAsync(ct);
        _home.Invalidate();
        return Result.Success();
    }

    private static string? Validate(TestimonialEditModel m)
    {
        if (string.IsNullOrWhiteSpace(m.CustomerName)) return "Customer name is required.";
        if (string.IsNullOrWhiteSpace(m.Message)) return "Message is required.";
        return null;
    }
}

// =================================================================== Settings

public sealed class SiteSettingsAdminService : ISiteSettingsAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly ISiteSettingsService _settings;
    private readonly IHomePageService _home;
    private readonly IConfigAuditService _audit;
    private readonly ICurrentUserService _currentUser;

    public SiteSettingsAdminService(
        ApplicationDbContext db, ISiteSettingsService settings, IHomePageService home,
        IConfigAuditService audit, ICurrentUserService currentUser)
    {
        _db = db;
        _settings = settings;
        _home = home;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SiteSettingItem>> ListAsync(CancellationToken ct = default) =>
        await _db.SiteSettings.AsNoTracking()
            .OrderBy(s => s.Key)
            .Select(s => new SiteSettingItem { Key = s.Key, Value = s.Value, Description = s.Description })
            .ToListAsync(ct);

    public async Task<Result> SaveAsync(IReadOnlyDictionary<string, string?> values, CancellationToken ct = default)
    {
        var rows = await _db.SiteSettings.ToListAsync(ct);
        foreach (var row in rows)
        {
            if (!values.TryGetValue(row.Key, out var newValue)) continue;
            newValue = newValue?.Trim();
            if (newValue == row.Value) continue;

            await _audit.RecordAsync(ConfigEntityTypes.SiteSetting, row.Key, row.Value, newValue, _currentUser.UserName, ct);
            row.Value = newValue;
        }
        await _db.SaveChangesAsync(ct);
        _settings.Invalidate();
        _home.Invalidate();
        return Result.Success();
    }
}

// ================================================================ Subscribers

public sealed class SubscriberAdminService : ISubscriberAdminService
{
    private readonly ApplicationDbContext _db;

    public SubscriberAdminService(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<SubscriberListItem>> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.NewsletterSubscribers.AsNoTracking().OrderByDescending(s => s.SubscribedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new SubscriberListItem
            {
                Id = s.Id, Email = s.Email, IsActive = s.IsActive, SubscribedAt = s.SubscribedAt
            })
            .ToListAsync(ct);
        return new PagedResult<SubscriberListItem>(items, page, pageSize, total);
    }

    public async Task<Result> ToggleAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.NewsletterSubscribers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return Result.Failure("Subscriber not found.");
        e.IsActive = !e.IsActive;
        e.UnsubscribedAt = e.IsActive ? null : DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<string> ExportCsvAsync(CancellationToken ct = default)
    {
        var rows = await _db.NewsletterSubscribers.AsNoTracking()
            .OrderBy(s => s.SubscribedAt)
            .Select(s => new { s.Email, s.IsActive, s.SubscribedAt })
            .ToListAsync(ct);

        var sb = new StringBuilder("Email,IsActive,SubscribedAtUtc\n");
        foreach (var r in rows)
            sb.Append(r.Email).Append(',').Append(r.IsActive).Append(',').Append(r.SubscribedAt.ToString("O")).Append('\n');
        return sb.ToString();
    }
}

// =========================================================== Contact messages

public sealed class ContactMessageService : IContactMessageService
{
    private readonly ApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public ContactMessageService(ApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result> SubmitAsync(ContactSubmitRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? "").Trim();
        var email = (request.Email ?? "").Trim();
        var message = (request.Message ?? "").Trim();

        if (name.Length is < 2 or > 150) return Result.Failure("Please enter your name.");
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            return Result.Failure("Please enter a valid email address.");
        if (message.Length is < 10 or > 2000) return Result.Failure("Please write a message (10–2000 characters).");

        _db.ContactMessages.Add(new ContactMessage
        {
            Name = name,
            Email = email,
            Subject = string.IsNullOrWhiteSpace(request.Subject) ? null : request.Subject.Trim(),
            Message = message,
            IsRead = false,
            CreatedAt = _clock.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<(IReadOnlyList<ContactMessageListItem> Items, int UnreadCount)> ListAsync(CancellationToken ct = default)
    {
        var items = await _db.ContactMessages.AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .Take(200)
            .Select(m => new ContactMessageListItem
            {
                Id = m.Id, Name = m.Name, Email = m.Email, Subject = m.Subject,
                Message = m.Message, IsRead = m.IsRead, CreatedAt = m.CreatedAt
            })
            .ToListAsync(ct);

        var unread = items.Count(m => !m.IsRead);
        return (items, unread);
    }

    public async Task<Result> MarkReadAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return Result.Failure("Message not found.");
        e.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return Result.Failure("Message not found.");
        _db.ContactMessages.Remove(e);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
