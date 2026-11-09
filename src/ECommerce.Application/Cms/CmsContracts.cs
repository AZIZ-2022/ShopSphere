using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Cms;

// -------------------------------------------------------------------- Sliders

public sealed class SliderEditModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? MobileImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? ButtonText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Optional scheduling window; leave blank for no bound on that side.</summary>
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
}

public interface ISliderAdminService
{
    Task<IReadOnlyList<SliderEditModel>> ListAsync(CancellationToken ct = default);
    Task<SliderEditModel?> GetAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(SliderEditModel model, CancellationToken ct = default);
    Task<Result> UpdateAsync(SliderEditModel model, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

// --------------------------------------------------------------- Testimonials

public sealed class TestimonialEditModel
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? PhotoUrl { get; set; }
    public string Message { get; set; } = string.Empty;
    public byte Rating { get; set; } = 5;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public interface ITestimonialAdminService
{
    Task<IReadOnlyList<TestimonialEditModel>> ListAsync(CancellationToken ct = default);
    Task<TestimonialEditModel?> GetAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(TestimonialEditModel model, CancellationToken ct = default);
    Task<Result> UpdateAsync(TestimonialEditModel model, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

// ------------------------------------------------------------------- Settings

public sealed class SiteSettingItem
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
}

public interface ISiteSettingsAdminService
{
    Task<IReadOnlyList<SiteSettingItem>> ListAsync(CancellationToken ct = default);
    Task<Result> SaveAsync(IReadOnlyDictionary<string, string?> values, CancellationToken ct = default);
}

// ---------------------------------------------------------------- Subscribers

public sealed class SubscriberListItem
{
    public int Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime SubscribedAt { get; init; }
}

public interface ISubscriberAdminService
{
    Task<PagedResult<SubscriberListItem>> ListAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result> ToggleAsync(int id, CancellationToken ct = default);
    Task<string> ExportCsvAsync(CancellationToken ct = default);
}

// ------------------------------------------------------------ Contact messages

public sealed class ContactMessageListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class ContactSubmitRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string Message { get; init; } = string.Empty;
}

public interface IContactMessageService
{
    Task<Result> SubmitAsync(ContactSubmitRequest request, CancellationToken ct = default);
    Task<(IReadOnlyList<ContactMessageListItem> Items, int UnreadCount)> ListAsync(CancellationToken ct = default);
    Task<Result> MarkReadAsync(int id, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
