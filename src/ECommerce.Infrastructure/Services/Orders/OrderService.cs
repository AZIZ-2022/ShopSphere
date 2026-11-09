using ECommerce.Application.Cart;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Orders;

public sealed class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cart;
    private readonly IAddressService _addresses;
    private readonly ICouponService _coupons;
    private readonly ISiteSettingsService _settings;
    private readonly IDateTimeProvider _clock;

    public OrderService(
        ApplicationDbContext db,
        ICartService cart,
        IAddressService addresses,
        ICouponService coupons,
        ISiteSettingsService settings,
        IDateTimeProvider clock)
    {
        _db = db;
        _cart = cart;
        _addresses = addresses;
        _coupons = coupons;
        _settings = settings;
        _clock = clock;
    }

    public async Task<CheckoutContextDto> BuildCheckoutAsync(string userId, string? couponCode, CancellationToken ct = default)
    {
        var cart = await _cart.GetCartAsync(CartOwner.ForUser(userId), ct);
        var addresses = await _addresses.ListAsync(userId, ct);
        var settings = await _settings.GetAsync(ct);

        var lines = cart.Lines.Select(l => new CheckoutLineDto
        {
            ProductId = l.ProductId,
            Name = l.Name,
            Slug = l.Slug,
            ImageUrl = l.ImageUrl,
            UnitPrice = l.UnitPrice,
            Quantity = l.Quantity
        }).ToList();

        var subTotal = lines.Sum(l => l.LineTotal);

        decimal discount = 0;
        string? applied = null;
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var result = await _coupons.ValidateAsync(couponCode, subTotal, ct);
            if (result.Valid)
            {
                discount = result.DiscountAmount;
                applied = result.Code;
            }
        }

        var shipping = ShippingFor(subTotal - discount, settings.FreeShippingThreshold, settings.FlatShippingRate, lines.Count);
        var tax = TaxFor(subTotal - discount, settings.TaxRatePercent);

        return new CheckoutContextDto
        {
            Lines = lines,
            Addresses = addresses,
            ShippingCost = shipping,
            FreeShippingThreshold = settings.FreeShippingThreshold,
            DiscountAmount = discount,
            TaxAmount = tax,
            AppliedCoupon = applied,
            EnableCod = settings.PaymentEnableCod,
            EnableOnlineGateway = settings.PaymentEnableOnlineGateway
        };
    }

    public async Task<Result<PlaceOrderResultDto>> PlaceOrderAsync(string userId, PlaceOrderRequest request, CancellationToken ct = default)
    {
        var cart = await _db.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is null || cart.Items.Count == 0)
            return Result<PlaceOrderResultDto>.Failure("Your cart is empty.");

        // Resolve the shipping address.
        Address? address = null;
        if (request.ShippingAddressId is { } addrId)
        {
            address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == addrId && a.UserId == userId, ct);
            if (address is null) return Result<PlaceOrderResultDto>.Failure("Please choose a valid delivery address.");
        }
        else if (request.NewAddress is { } newAddr)
        {
            var created = await _addresses.CreateAsync(userId, newAddr, ct);
            if (!created.Succeeded) return Result<PlaceOrderResultDto>.Failure(created.Error!);
            address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == created.Data, ct);
        }

        if (address is null)
            return Result<PlaceOrderResultDto>.Failure("A delivery address is required.");

        // Load products and validate stock.
        var productIds = cart.Items.Select(i => i.ProductId).ToArray();
        var products = await _db.Products
            .Include(p => p.Images)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        foreach (var item in cart.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var p) || !p.IsActive)
                return Result<PlaceOrderResultDto>.Failure($"“{item.ProductId}” is no longer available. Please review your cart.");
            if (item.Quantity > p.StockQuantity)
                return Result<PlaceOrderResultDto>.Failure($"“{p.Name}” only has {p.StockQuantity} left. Please update your cart.");
        }

        var settings = await _settings.GetAsync(ct);
        if (request.PaymentMethod == PaymentMethodType.CashOnDelivery && !settings.PaymentEnableCod)
            return Result<PlaceOrderResultDto>.Failure("Cash on Delivery is not available right now. Please choose another payment method.");
        if (request.PaymentMethod == PaymentMethodType.OnlineGateway && !settings.PaymentEnableOnlineGateway)
            return Result<PlaceOrderResultDto>.Failure("Online payment is not available right now. Please choose another payment method.");

        var subTotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);

        decimal discount = 0;
        string? couponCode = null;
        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var couponResult = await _coupons.ValidateAsync(request.CouponCode, subTotal, ct);
            if (couponResult.Valid)
            {
                discount = couponResult.DiscountAmount;
                couponCode = couponResult.Code;
                coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode, ct);
            }
        }

        var shipping = ShippingFor(subTotal - discount, settings.FreeShippingThreshold, settings.FlatShippingRate, cart.Items.Count);
        var tax = TaxFor(subTotal - discount, settings.TaxRatePercent);
        var total = Math.Max(0, subTotal - discount) + shipping + tax;

        var now = _clock.UtcNow;
        var isOnline = request.PaymentMethod == PaymentMethodType.OnlineGateway;

        // Online orders are created unpaid; the customer completes payment on the next page.
        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            PaymentMethod = request.PaymentMethod,
            SubTotal = subTotal,
            DiscountAmount = discount,
            ShippingCost = shipping,
            TaxAmount = tax,
            TotalAmount = total,
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "USD" : request.CurrencyCode.ToUpperInvariant(),
            ExchangeRate = request.ExchangeRate <= 0 ? 1m : request.ExchangeRate,
            CouponCode = couponCode,
            CustomerNote = string.IsNullOrWhiteSpace(request.CustomerNote) ? null : request.CustomerNote.Trim(),
            ShippingName = address.FullName,
            ShippingPhone = address.Phone,
            ShippingLine1 = address.Line1,
            ShippingLine2 = address.Line2,
            ShippingCity = address.City,
            ShippingState = address.State,
            ShippingPostalCode = address.PostalCode,
            ShippingCountry = address.Country,
            PlacedAt = now
        };

        foreach (var item in cart.Items)
        {
            var p = products[item.ProductId];
            order.Items.Add(new OrderItem
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Sku = p.Sku,
                ImageUrl = p.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault(),
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                LineTotal = item.UnitPrice * item.Quantity
            });
        }

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = order.Status,
            Note = isOnline ? "Order placed — awaiting online payment." : "Order placed (Cash on Delivery).",
            ChangedAt = now,
            ChangedBy = userId
        });

        order.Payments.Add(new Payment
        {
            Method = request.PaymentMethod,
            Status = PaymentStatus.Pending,
            Amount = total,
            CreatedAt = now
        });

        if (coupon is not null)
            coupon.UsedCount += 1;

        order.OrderNumber = await GenerateOrderNumberAsync(now, ct);
        _db.Orders.Add(order);
        _db.CartItems.RemoveRange(cart.Items);
        _db.Carts.Remove(cart);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Atomically reserve stock for every line before committing the order. Each
        // UPDATE re-checks StockQuantity >= requested quantity in the same statement as
        // the decrement, so two concurrent checkouts can never both succeed for the last
        // unit — unlike a load-check-then-save-the-tracked-entity pattern, this is race-safe
        // regardless of the database's transaction isolation level.
        foreach (var item in cart.Items)
        {
            var affected = await _db.Products
                .Where(p => p.Id == item.ProductId && p.StockQuantity >= item.Quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.StockQuantity, p => p.StockQuantity - item.Quantity)
                    .SetProperty(p => p.SoldCount, p => p.SoldCount + item.Quantity), ct);

            if (affected == 0)
            {
                await tx.RollbackAsync(ct);
                var name = products.TryGetValue(item.ProductId, out var outOfStock) ? outOfStock.Name : "An item";
                return Result<PlaceOrderResultDto>.Failure($"“{name}” just sold out. Please update your cart.");
            }
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Most likely an order-number race — try once more with a salted number.
            order.OrderNumber = await GenerateOrderNumberAsync(now, ct, salt: Random.Shared.Next(1000, 9999));
            await _db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);

        return Result<PlaceOrderResultDto>.Success(new PlaceOrderResultDto
        {
            OrderNumber = order.OrderNumber,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            RequiresPayment = isOnline
        });
    }

    public async Task<IReadOnlyList<OrderListItemDto>> ListForCustomerAsync(string userId, CancellationToken ct = default) =>
        await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.PlacedAt)
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
                ItemCount = o.Items.Sum(i => i.Quantity)
            })
            .ToListAsync(ct);

    public async Task<OrderDto?> GetForCustomerAsync(string userId, string orderNumber, CancellationToken ct = default)
    {
        var order = await LoadAsync(o => o.UserId == userId && o.OrderNumber == orderNumber, ct);
        return order;
    }

    public async Task<OrderDto?> TrackAsync(string orderNumber, string email, CancellationToken ct = default)
    {
        orderNumber = (orderNumber ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim();
        if (orderNumber.Length == 0 || email.Length == 0) return null;

        var userId = await _db.Users.AsNoTracking()
            .Where(u => u.NormalizedEmail == email.ToUpperInvariant())
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);

        if (userId is null) return null;
        return await LoadAsync(o => o.OrderNumber == orderNumber && o.UserId == userId, ct);
    }

    public async Task<Result> CancelAsync(string userId, string orderNumber, string? reason, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.OrderNumber == orderNumber, ct);

        if (order is null) return Result.Failure("Order not found.");
        if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            return Result.Failure("This order can no longer be cancelled — it is already being processed.");

        order.Status = OrderStatus.Cancelled;

        var ids = order.Items.Select(i => i.ProductId).ToArray();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync(ct);
        foreach (var item in order.Items)
        {
            var p = products.FirstOrDefault(x => x.Id == item.ProductId);
            if (p is null) continue;
            p.StockQuantity += item.Quantity;
            p.SoldCount = Math.Max(0, p.SoldCount - item.Quantity);
        }

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Cancelled,
            Note = string.IsNullOrWhiteSpace(reason) ? "Cancelled by customer." : $"Cancelled by customer: {reason.Trim()}",
            ChangedAt = _clock.UtcNow,
            ChangedBy = userId
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RequestReturnAsync(string userId, string orderNumber, string? reason, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.OrderNumber == orderNumber, ct);

        if (order is null) return Result.Failure("Order not found.");
        if (order.Status != OrderStatus.Delivered)
            return Result.Failure("Only delivered orders can have a return requested.");

        order.Status = OrderStatus.ReturnRequested;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.ReturnRequested,
            Note = string.IsNullOrWhiteSpace(reason) ? "Return requested by customer." : $"Return requested by customer: {reason.Trim()}",
            ChangedAt = _clock.UtcNow,
            ChangedBy = userId
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // -------------------------------------------------------------- helpers

    internal async Task<OrderDto?> LoadAsync(
        System.Linq.Expressions.Expression<Func<Order, bool>> predicate, CancellationToken ct)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(predicate, ct);

        if (order is null) return null;

        var slugLookup = await _db.Products.AsNoTracking()
            .Where(p => order.Items.Select(i => i.ProductId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Slug, ct);

        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == order.UserId)
            .Select(u => new { u.Email, u.FullName })
            .FirstOrDefaultAsync(ct);

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            CustomerEmail = user?.Email,
            CustomerName = user?.FullName,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            PaymentMethod = order.PaymentMethod,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            ShippingCost = order.ShippingCost,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.TotalAmount,
            CurrencyCode = order.CurrencyCode,
            ExchangeRate = order.ExchangeRate,
            CouponCode = order.CouponCode,
            CustomerNote = order.CustomerNote,
            ShippingName = order.ShippingName,
            ShippingPhone = order.ShippingPhone,
            ShippingLine1 = order.ShippingLine1,
            ShippingLine2 = order.ShippingLine2,
            ShippingCity = order.ShippingCity,
            ShippingState = order.ShippingState,
            ShippingPostalCode = order.ShippingPostalCode,
            ShippingCountry = order.ShippingCountry,
            PlacedAt = order.PlacedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Sku = i.Sku,
                ImageUrl = i.ImageUrl,
                Slug = slugLookup.TryGetValue(i.ProductId, out var s) ? s : null,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal
            }).ToList(),
            History = order.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .Select(h => new OrderStatusEntryDto { Status = h.Status, Note = h.Note, ChangedAt = h.ChangedAt })
                .ToList(),
            Payments = order.Payments
                .OrderBy(p => p.Id)
                .Select(p => new OrderPaymentDto
                {
                    Method = p.Method,
                    Status = p.Status,
                    Amount = p.Amount,
                    TransactionId = p.TransactionId,
                    GatewayResponse = p.GatewayResponse,
                    PaidAt = p.PaidAt,
                    CreatedAt = p.CreatedAt
                })
                .ToList()
        };
    }

    private static decimal ShippingFor(decimal payable, decimal threshold, decimal flat, int lineCount)
    {
        if (lineCount == 0) return 0m;
        return threshold > 0 && payable >= threshold ? 0m : flat;
    }

    private static decimal TaxFor(decimal taxableAmount, decimal ratePercent) =>
        ratePercent > 0 ? Math.Round(Math.Max(0, taxableAmount) * ratePercent / 100m, 2) : 0m;

    private async Task<string> GenerateOrderNumberAsync(DateTime nowUtc, CancellationToken ct, int? salt = null)
    {
        var dayStart = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc);
        var todays = await _db.Orders.CountAsync(o => o.PlacedAt >= dayStart, ct);
        var seq = todays + 1 + (salt ?? 0);
        return $"ORD-{nowUtc:yyyyMMdd}-{seq:D4}";
    }
}
