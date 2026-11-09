using ECommerce.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Stores uploads under <c>wwwroot/uploads/{subFolder}</c> with a GUID file name.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly IWebHostEnvironment _env;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public LocalFileStorage(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveAsync(Stream content, string originalFileName, string subFolder, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (Array.IndexOf(AllowedExtensions, ext) < 0)
            throw new InvalidOperationException($"File type '{ext}' is not allowed.");

        var safeSubFolder = string.Join("_", subFolder.Split(Path.GetInvalidFileNameChars()));
        var folder = Path.Combine(_env.WebRootPath, "uploads", safeSubFolder);
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
        {
            await content.CopyToAsync(fs, ct);
        }

        return $"/uploads/{safeSubFolder}/{fileName}";
    }

    public Task DeleteAsync(string webRelativePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(webRelativePath))
            return Task.CompletedTask;

        var relative = webRelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_env.WebRootPath, relative);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
