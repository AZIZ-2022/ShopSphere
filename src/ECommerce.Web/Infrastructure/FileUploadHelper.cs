using ECommerce.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ECommerce.Web.Infrastructure;

/// <summary>Shared single-image upload handling for admin forms (category/brand/slider/testimonial/site images).</summary>
public static class FileUploadHelper
{
    private const int DefaultMaxBytes = 3_000_000;

    /// <summary>Saves <paramref name="file"/> via <paramref name="files"/> and returns its URL, or null if no file
    /// was chosen. Adds a model error and returns null if the file is too large or fails to save.</summary>
    public static async Task<string?> SaveImageAsync(
        this IFileStorage files, IFormFile? file, string subFolder, ModelStateDictionary modelState,
        int maxBytes = DefaultMaxBytes, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0) return null;

        if (file.Length > maxBytes)
        {
            modelState.AddModelError(string.Empty, $"'{file.FileName}' is larger than {maxBytes / 1_000_000} MB and was not saved.");
            return null;
        }

        try
        {
            await using var stream = file.OpenReadStream();
            return await files.SaveAsync(stream, file.FileName, subFolder, ct);
        }
        catch (Exception ex)
        {
            modelState.AddModelError(string.Empty, $"'{file.FileName}' could not be saved: {ex.Message}");
            return null;
        }
    }
}
