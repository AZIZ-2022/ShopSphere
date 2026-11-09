using System.ComponentModel.DataAnnotations;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services;

public sealed class NewsletterService : INewsletterService
{
    private readonly ApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public NewsletterService(ApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result> SubscribeAsync(string email, CancellationToken ct = default)
    {
        email = (email ?? string.Empty).Trim();

        if (!new EmailAddressAttribute().IsValid(email) || email.Length > 256)
            return Result.Failure("Please enter a valid email address.");

        var existing = await _db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == email, ct);

        if (existing is not null)
        {
            if (existing.IsActive)
                return Result.Success();

            existing.IsActive = true;
            existing.UnsubscribedAt = null;
            existing.SubscribedAt = _clock.UtcNow;
        }
        else
        {
            _db.NewsletterSubscribers.Add(new NewsletterSubscriber
            {
                Email = email,
                IsActive = true,
                SubscribedAt = _clock.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
