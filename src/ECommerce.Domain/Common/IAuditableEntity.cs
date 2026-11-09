namespace ECommerce.Domain.Common;

/// <summary>
/// Audit trail columns populated automatically by the persistence layer
/// (see AuditableEntitySaveChangesInterceptor in Infrastructure).
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
