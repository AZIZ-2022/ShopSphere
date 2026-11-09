using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public class Coupon : AuditableSoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }

    public decimal MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }

    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
}
