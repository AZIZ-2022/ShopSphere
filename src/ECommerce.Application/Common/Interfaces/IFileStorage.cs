namespace ECommerce.Application.Common.Interfaces;

/// <summary>
/// Stores uploaded binary assets (product images, sliders, avatars...).
/// The Phase 1 implementation writes to <c>wwwroot/uploads</c>.
/// </summary>
public interface IFileStorage
{
    /// <summary>Persists a file and returns its web-relative URL (e.g. <c>/uploads/products/abc.jpg</c>).</summary>
    Task<string> SaveAsync(Stream content, string originalFileName, string subFolder, CancellationToken ct = default);

    Task DeleteAsync(string webRelativePath, CancellationToken ct = default);
}
