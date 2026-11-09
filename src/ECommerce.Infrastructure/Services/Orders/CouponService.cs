using System.Text.Json;
using ECommerce.Application.Common.Audit;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Orders;

public sealed class CouponService : ICouponService
{
    private readonly ApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public CouponService(ApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<CouponResult> ValidateAsync(string code, decimal subTotal, CancellationToken ct = default)
    {
        code = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
            return new CouponResult { Valid = false, Message = "Enter a coupon code." };

        var coupon = await _db.Coupons.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code.ToUpper() == code, ct);

        if (coupon is null || !coupon.IsActive)
            return new CouponResult { Valid = false, Code = code, Message = "That coupon code is not valid." };

        var now = _clock.UtcNow;
        if (now < coupon.StartsAt || now > coupon.EndsAt)
            return new CouponResult { Valid = false, Code = code, Message = "This coupon has expired." };

        if (coupon.UsageLimit is { } limit && coupon.UsedCount >= limit)
            return new CouponResult { Valid = false, Code = code, Message = "This coupon has reached its usage limit." };

        if (subTotal < coupon.MinOrderAmount)
            return new CouponResult
            {
                Valid = false, Code = code,
                Message = $"Spend at least {coupon.MinOrderAmount:0.##} to use this coupon."
            };

        var discount = coupon.DiscountType == DiscountType.Percentage
            ? subTotal * (coupon.DiscountValue / 100m)
            : coupon.DiscountValue;

        if (coupon.MaxDiscountAmount is { } max && discount > max)
            discount = max;

        discount = Math.Round(Math.Min(discount, subTotal), 2);

        return new CouponResult
        {
            Valid = true,
            Code = coupon.Code,
            DiscountAmount = discount,
            Message = $"Coupon applied — you save {discount:0.##}."
        };
    }
}

public sealed class CouponAdminService : ICouponAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfigAuditService _audit;
    private readonly ICurrentUserService _currentUser;

    public CouponAdminService(ApplicationDbContext db, IConfigAuditService audit, ICurrentUserService currentUser)
    {
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CouponListItem>> ListAsync(CancellationToken ct = default) =>
        await _db.Coupons.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CouponListItem
            {
                Id = c.Id,
                Code = c.Code,
                DiscountType = c.DiscountType,
                DiscountValue = c.DiscountValue,
                UsageLimit = c.UsageLimit,
                UsedCount = c.UsedCount,
                StartsAt = c.StartsAt,
                EndsAt = c.EndsAt,
                IsActive = c.IsActive
            })
            .ToListAsync(ct);

    public async Task<CouponEditModel?> GetAsync(int id, CancellationToken ct = default) =>
        await _db.Coupons.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CouponEditModel
            {
                Id = c.Id,
                Code = c.Code,
                Description = c.Description,
                DiscountType = c.DiscountType,
                DiscountValue = c.DiscountValue,
                MinOrderAmount = c.MinOrderAmount,
                MaxDiscountAmount = c.MaxDiscountAmount,
                UsageLimit = c.UsageLimit,
                UsedCount = c.UsedCount,
                StartsAt = c.StartsAt,
                EndsAt = c.EndsAt,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync(ct);

    public async Task<Result<int>> CreateAsync(CouponEditModel model, CancellationToken ct = default)
    {
        var error = await ValidateAsync(model, ct);
        if (error is not null) return Result<int>.Failure(error);

        var entity = new Coupon
        {
            Code = model.Code.Trim().ToUpperInvariant(),
            Description = model.Description?.Trim(),
            DiscountType = model.DiscountType,
            DiscountValue = model.DiscountValue,
            MinOrderAmount = model.MinOrderAmount,
            MaxDiscountAmount = model.MaxDiscountAmount,
            UsageLimit = model.UsageLimit,
            StartsAt = model.StartsAt,
            EndsAt = model.EndsAt,
            IsActive = model.IsActive
        };
        _db.Coupons.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> UpdateAsync(CouponEditModel model, CancellationToken ct = default)
    {
        var entity = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == model.Id, ct);
        if (entity is null) return Result.Failure("Coupon not found.");

        var error = await ValidateAsync(model, ct);
        if (error is not null) return Result.Failure(error);

        var before = JsonSerializer.Serialize(ToSnapshot(entity));

        entity.Code = model.Code.Trim().ToUpperInvariant();
        entity.Description = model.Description?.Trim();
        entity.DiscountType = model.DiscountType;
        entity.DiscountValue = model.DiscountValue;
        entity.MinOrderAmount = model.MinOrderAmount;
        entity.MaxDiscountAmount = model.MaxDiscountAmount;
        entity.UsageLimit = model.UsageLimit;
        entity.StartsAt = model.StartsAt;
        entity.EndsAt = model.EndsAt;
        entity.IsActive = model.IsActive;

        var after = JsonSerializer.Serialize(ToSnapshot(entity));

        await _db.SaveChangesAsync(ct);
        await _audit.RecordAsync(ConfigEntityTypes.Coupon, entity.Id.ToString(), before, after, _currentUser.UserName, ct);
        return Result.Success();
    }

    private static CouponEditModel ToSnapshot(Coupon c) => new()
    {
        Id = c.Id,
        Code = c.Code,
        Description = c.Description,
        DiscountType = c.DiscountType,
        DiscountValue = c.DiscountValue,
        MinOrderAmount = c.MinOrderAmount,
        MaxDiscountAmount = c.MaxDiscountAmount,
        UsageLimit = c.UsageLimit,
        UsedCount = c.UsedCount,
        StartsAt = c.StartsAt,
        EndsAt = c.EndsAt,
        IsActive = c.IsActive
    };

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return Result.Failure("Coupon not found.");

        _db.Coupons.Remove(entity); // soft delete via interceptor
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<string?> ValidateAsync(CouponEditModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Code)) return "Code is required.";
        if (model.DiscountValue <= 0) return "Discount value must be greater than zero.";
        if (model.DiscountType == DiscountType.Percentage && model.DiscountValue > 100) return "A percentage discount can't exceed 100.";
        if (model.EndsAt <= model.StartsAt) return "The end date must be after the start date.";

        var code = model.Code.Trim().ToUpperInvariant();
        var inUse = await _db.Coupons.AnyAsync(c => c.Code.ToUpper() == code && c.Id != model.Id, ct);
        return inUse ? $"Coupon code '{code}' is already in use." : null;
    }
}
