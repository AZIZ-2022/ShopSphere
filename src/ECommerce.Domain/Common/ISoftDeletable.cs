namespace ECommerce.Domain.Common;

/// <summary>
/// Marks an entity that is never physically removed. A global EF query filter
/// hides rows where <see cref="IsDeleted"/> is true.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
