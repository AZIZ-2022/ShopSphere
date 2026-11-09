using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface INewsletterService
{
    Task<Result> SubscribeAsync(string email, CancellationToken ct = default);
}
