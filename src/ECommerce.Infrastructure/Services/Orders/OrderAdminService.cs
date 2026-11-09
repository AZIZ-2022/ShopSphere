using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Orders;

public sealed class OrderAdminService : IOrderAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly OrderService _reader;
    private readonly IDateTimeProvider _clock;

    public OrderAdminService(ApplicationDbContext db, OrderService reader, IDateTimeProvider clock)
    {
        _db = db;
        _reader = reader;
        _clock = clock;
    }

    public async Task<PagedResult<OrderListItemDto>> ListAsync(OrderAdminFilter filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 100);

        var query = _db.Orders.AsNoTracking().AsQueryable();

        if (filter.Status is { } st)
            query = query.Where(o => o.Status == st);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(o =>
                EF.Functions.Like(o.OrderNumber, $"%{term}%") ||
                _db.Users.Any(u => u.Id == o.UserId &&
                    (EF.Functions.Like(u.Email!, $"%{term}%") || EF.Functions.Like(u.FullName, $"%{term}%"))));
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(o => o.PlacedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(o => new
            {
                o.Id, o.OrderNumber, o.PlacedAt, o.Status, o.PaymentStatus, o.PaymentMethod,
                o.TotalAmount, o.CurrencyCode, o.ExchangeRate,
                ItemCount = o.Items.Sum(i => i.Quantity),
                Customer = _db.Users.Where(u => u.Id == o.UserId).Select(u => new { u.FullName, u.Email }).FirstOrDefault()
            })
            .ToListAsync(ct);

        var items = rows.Select(o => new OrderListItemDto
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
            ItemCount = o.ItemCount,
            CustomerName = o.Customer?.FullName,
            CustomerEmail = o.Customer?.Email
        }).ToList();

        return new PagedResult<OrderListItemDto>(items, page, size, total);
    }

    public Task<OrderDto?> GetAsync(int id, CancellationToken ct = default) =>
        _reader.LoadAsync(o => o.Id == id, ct);

    public async Task<Result> ChangeStatusAsync(int id, OrderStatus status, string? note, string? changedBy, CancellationToken ct = default)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return Result.Failure("Order not found.");
        if (order.Status == status) return Result.Success();

        var previous = order.Status;
        order.Status = status;

        // Restock when an order is cancelled or returned.
        if (status is OrderStatus.Cancelled or OrderStatus.Returned &&
            previous is not OrderStatus.Cancelled and not OrderStatus.Returned)
        {
            var ids = order.Items.Select(i => i.ProductId).ToArray();
            var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync(ct);
            foreach (var item in order.Items)
            {
                var p = products.FirstOrDefault(x => x.Id == item.ProductId);
                if (p is null) continue;
                p.StockQuantity += item.Quantity;
                p.SoldCount = Math.Max(0, p.SoldCount - item.Quantity);
            }
        }

        if (status == OrderStatus.Delivered && order.PaymentMethod == PaymentMethodType.CashOnDelivery)
            order.PaymentStatus = PaymentStatus.Paid;

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = status,
            Note = string.IsNullOrWhiteSpace(note) ? $"Status changed to {status}." : note.Trim(),
            ChangedAt = _clock.UtcNow,
            ChangedBy = changedBy
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ChangePaymentStatusAsync(int id, PaymentStatus status, string? changedBy, CancellationToken ct = default)
    {
        var order = await _db.Orders.Include(o => o.Payments).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return Result.Failure("Order not found.");

        order.PaymentStatus = status;
        var payment = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault();
        if (payment is not null)
        {
            payment.Status = status;
            payment.PaidAt = status == PaymentStatus.Paid ? _clock.UtcNow : payment.PaidAt;
        }

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.Status,
            Note = $"Payment marked {status}.",
            ChangedAt = _clock.UtcNow,
            ChangedBy = changedBy
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<OrderMetricsDto> GetMetricsAsync(CancellationToken ct = default)
    {
        var totalOrders = await _db.Orders.CountAsync(ct);
        var pending = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending, ct);
        var revenue = await _db.Orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;
        var lowStock = await _db.Products.CountAsync(p => p.IsActive && p.StockQuantity <= 5, ct);

        return new OrderMetricsDto
        {
            TotalOrders = totalOrders,
            PendingOrders = pending,
            RevenuePaid = revenue,
            LowStockProducts = lowStock
        };
    }
}
