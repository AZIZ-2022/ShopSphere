using ECommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Development email sender. Writes messages to the log and to
/// <c>App_Data/sent-emails</c> instead of contacting an SMTP server.
/// </summary>
public sealed class DevEmailSender : IEmailSender
{
    private readonly ILogger<DevEmailSender> _logger;

    public DevEmailSender(ILogger<DevEmailSender> logger) => _logger = logger;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation("DEV EMAIL -> {To} | {Subject}", to, subject);

        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "App_Data", "sent-emails");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.html");
            await File.WriteAllTextAsync(file, $"<!-- To: {to} | Subject: {subject} -->\n{htmlBody}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write dev email to disk.");
        }
    }
}
