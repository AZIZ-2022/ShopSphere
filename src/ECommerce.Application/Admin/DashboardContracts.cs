using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders;

namespace ECommerce.Application.Admin;

public sealed class DashboardMetrics
{
    public int TotalOrders { get; init; }
    public int PendingOrders { get; init; }
    public int OrdersToday { get; init; }
    public decimal RevenuePaid { get; init; }
    public decimal RevenueToday { get; init; }
    public int TotalProducts { get; init; }
    public int LowStockCount { get; init; }
    public int TotalCustomers { get; init; }
    public int NewCustomersThisWeek { get; init; }
}

public sealed class SalesPoint
{
    public DateOnly Date { get; init; }
    public decimal Revenue { get; init; }
    public int Orders { get; init; }
}

public sealed class TopProductRow
{
    public int ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int UnitsSold { get; init; }
    public decimal Revenue { get; init; }
}

public sealed class LowStockRow
{
    public int ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public bool IsActive { get; init; }
}

public sealed class DashboardDto
{
    public required DashboardMetrics Metrics { get; init; }
    public IReadOnlyList<SalesPoint> Sales { get; init; } = Array.Empty<SalesPoint>();
    public IReadOnlyList<OrderListItemDto> RecentOrders { get; init; } = Array.Empty<OrderListItemDto>();
    public IReadOnlyList<TopProductRow> TopProducts { get; init; } = Array.Empty<TopProductRow>();
    public IReadOnlyList<LowStockRow> LowStock { get; init; } = Array.Empty<LowStockRow>();
}

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken ct = default);
}

// ---------------------------------------------------------------- Users

public sealed class AdminUserListItem
{
    public string Id { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; }
    public bool IsAdmin { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public int OrderCount { get; init; }
}

public sealed class UserAdminFilter
{
    public string? Search { get; init; }
    public string? Role { get; init; }   // "Admin" | "Customer" | null
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public interface IUserAdminService
{
    Task<PagedResult<AdminUserListItem>> ListAsync(UserAdminFilter filter, CancellationToken ct = default);
    Task<Result> SetActiveAsync(string userId, bool active, string actingUserId, CancellationToken ct = default);
    Task<Result> SetAdminRoleAsync(string userId, bool isAdmin, string actingUserId, CancellationToken ct = default);
}
