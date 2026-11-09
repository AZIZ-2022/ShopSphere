using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Audit;

/// <summary>The fixed set of config entity types this log understands how to roll back.</summary>
public static class ConfigEntityTypes
{
    public const string SiteSetting = "SiteSetting";
    public const string FeatureFlag = "FeatureFlag";
    public const string Coupon = "Coupon";
    public const string CmsPage = "CmsPage";
    public const string HomeSection = "HomeSection";
}

public sealed class ConfigChangeLogItem
{
    public int Id { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string EntityKey { get; init; } = string.Empty;
    public string? OldValueJson { get; init; }
    public string? NewValueJson { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }

    public bool CanRollback => !string.IsNullOrEmpty(OldValueJson);
}

public interface IConfigAuditService
{
    /// <summary>Records one config change. Call this after loading the old state but before (or right after) saving the new one.</summary>
    Task RecordAsync(string entityType, string entityKey, string? oldValueJson, string? newValueJson, string? changedBy, CancellationToken ct = default);

    Task<PagedResult<ConfigChangeLogItem>> ListAsync(string? entityType, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Re-applies the pre-change snapshot of the given log entry via the owning admin service.</summary>
    Task<Result> RollbackAsync(int logId, string? changedBy, CancellationToken ct = default);
}
