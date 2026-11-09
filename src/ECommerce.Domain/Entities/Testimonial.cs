using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class Testimonial : AuditableEntity
{
    public string CustomerName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? PhotoUrl { get; set; }
    public string Message { get; set; } = string.Empty;
    public byte Rating { get; set; } = 5;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
