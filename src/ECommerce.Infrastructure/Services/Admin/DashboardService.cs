using ECommerce.Application.Admin;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Admin;

public sealed class DashboardService : IDashboardService
{
    private const int LowStockThreshold = 5;

    private readonly ApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public DashboardService(ApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<DashboardDto> GetAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var today = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var weekAgo = today.AddDays(-7);
        var seriesStart = today.AddDays(-13);

        var metrics = new DashboardMetrics
        {
            TotalOrders = await _db.Orders.CountAsync(ct),
            PendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending, ct),
            OrdersToday = await _db.Orders.CountAsync(o => o.PlacedAt >= today, ct),
            RevenuePaid = await _db.Orders.Where(o => o.PaymentStatus == PaymentStatus.Paid)
                .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m,
            RevenueToday = await _db.Orders.Where(o => o.PlacedAt >= today && o.PaymentStatus == PaymentStatus.Paid)
                .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m,
            TotalProducts = await _db.Products.CountAsync(ct),
            LowStockCount = await _db.Products.CountAsync(p => p.IsActive && p.StockQuantity <= LowStockThreshold, ct),
            TotalCustomers = await _db.Users.CountAsync(ct),
            NewCustomersThisWeek = await _db.Users.CountAsync(u => u.CreatedAt >= weekAgo, ct)
        };

        var rawSales = await _db.Orders
            .Where(o => o.PlacedAt >= seriesStart)
            .GroupBy(o => o.PlacedAt.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount), Orders = g.Count() })
            .ToListAsync(ct);

        var salesByDay = rawSales.ToDictionary(x => DateOnly.FromDateTime(x.Date));
        var sales = Enumerable.Range(0, 14)
            .Select(i => DateOnly.FromDateTime(seriesStart.AddDays(i)))
            .Select(d => salesByDay.TryGetValue(d, out var v)
                ? new SalesPoint { Date = d, Revenue = v.Revenue, Orders = v.Orders }
                : new SalesPoint { Date = d, Revenue = 0, Orders = 0 })
            .ToList();

        var recentOrders = await _db.Orders.AsNoTracking()
            .OrderByDescending(o => o.PlacedAt)
            .Take(8)
            .Select(o => new OrderListItemDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                PlacedAt = o.PlacedAt,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                PaymentMethod = o.PaymentMethod,
                TotalAmount = o.TotalAmount,
                CurrencyCode = o.CurrencyCode,
                ExchangeRate = o.ExchangeRate,
                ItemCount = o.Items.Sum(i => i.Quantity),
                CustomerName = _db.Users.Where(u => u.Id == o.UserId).Select(u => u.FullName).FirstOrDefault(),
                CustomerEmail = _db.Users.Where(u => u.Id == o.UserId).Select(u => u.Email).FirstOrDefault()
            })
            .ToListAsync(ct);

        var topProducts = await _db.OrderItems.AsNoTracking()
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                Units = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.Units)
            .Take(5)
            .ToListAsync(ct);

        var topIds = topProducts.Select(t => t.ProductId).ToArray();
        var slugLookup = await _db.Products.AsNoTracking()
            .Where(p => topIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Slug, ct);

        var top = topProducts.Select(t => new TopProductRow
        {
            ProductId = t.ProductId,
            Name = t.ProductName,
            Slug = slugLookup.TryGetValue(t.ProductId, out var s) ? s : "",
            UnitsSold = t.Units,
            Revenue = t.Revenue
        }).ToList();

        var lowStock = await _db.Products.AsNoTracking()
            .Where(p => p.StockQuantity <= LowStockThreshold)
            .OrderBy(p => p.StockQuantity)
            .Take(10)
            .Select(p => new LowStockRow
            {
                ProductId = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive
            })
            .ToListAsync(ct);

        return new DashboardDto
        {
            Metrics = metrics,
            Sales = sales,
            RecentOrders = recentOrders,
            TopProducts = top,
            LowStock = lowStock
        };
    }
}
