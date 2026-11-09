using ECommerce.Application.Common.Interfaces;

namespace ECommerce.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
