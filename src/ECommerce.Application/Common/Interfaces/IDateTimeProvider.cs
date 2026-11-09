namespace ECommerce.Application.Common.Interfaces;

/// <summary>Abstraction over the system clock so time-dependent logic stays testable.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
