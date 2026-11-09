using ECommerce.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Catalog;

internal static class SlugHelper
{
    /// <summary>
    /// Produces a unique slug for <paramref name="source"/>, appending -2, -3… on collision.
    /// <paramref name="exists"/> is called with a candidate slug and returns true if taken.
    /// </summary>
    public static async Task<string> UniqueAsync(
        string? explicitSlug,
        string source,
        Func<string, Task<bool>> exists)
    {
        var baseSlug = SlugGenerator.Create(string.IsNullOrWhiteSpace(explicitSlug) ? source : explicitSlug!);
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "item";

        var candidate = baseSlug;
        var suffix = 2;
        while (await exists(candidate))
            candidate = $"{baseSlug}-{suffix++}";

        return candidate;
    }
}
