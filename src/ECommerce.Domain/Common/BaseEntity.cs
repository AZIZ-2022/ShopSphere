namespace ECommerce.Domain.Common;

/// <summary>
/// Base type for all persistent entities that use an integer identity key.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}
