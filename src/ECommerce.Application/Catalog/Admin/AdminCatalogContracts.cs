using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Catalog.Admin;

// ---------------------------------------------------------------- Categories

public sealed class AdminCategoryListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int? ParentCategoryId { get; init; }
    public string? ParentName { get; init; }
    public int Depth { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
}

public sealed class CategoryEditModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public interface ICategoryAdminService
{
    Task<IReadOnlyList<AdminCategoryListItem>> ListAsync(CancellationToken ct = default);
    Task<CategoryEditModel?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<(int Id, string Label)>> GetParentOptionsAsync(int? excludingId = null, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(CategoryEditModel model, string? userId, CancellationToken ct = default);
    Task<Result> UpdateAsync(CategoryEditModel model, string? userId, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, string? userId, CancellationToken ct = default);
}

// ------------------------------------------------------------------- Brands

public sealed class AdminBrandListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
}

public sealed class BrandEditModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public interface IBrandAdminService
{
    Task<IReadOnlyList<AdminBrandListItem>> ListAsync(CancellationToken ct = default);
    Task<BrandEditModel?> GetAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(BrandEditModel model, string? userId, CancellationToken ct = default);
    Task<Result> UpdateAsync(BrandEditModel model, string? userId, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, string? userId, CancellationToken ct = default);
}

// ----------------------------------------------------------------- Products

public sealed class AdminProductListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string? BrandName { get; init; }
    public decimal Price { get; init; }
    public decimal? DiscountPrice { get; init; }
    public int StockQuantity { get; init; }
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsNewArrival { get; init; }
    public string? MainImageUrl { get; init; }
}

public sealed class ProductEditModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public bool IsNewArrival { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public IReadOnlyList<ProductImageDto> Images { get; set; } = Array.Empty<ProductImageDto>();
}

public sealed class NewProductImage
{
    public string ImageUrl { get; init; } = string.Empty;
    public string? AltText { get; init; }
}

public interface IProductAdminService
{
    Task<PagedResult<AdminProductListItem>> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<ProductEditModel?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<(int Id, string Label)>> GetCategoryOptionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<(int Id, string Label)>> GetBrandOptionsAsync(CancellationToken ct = default);

    Task<Result<int>> CreateAsync(ProductEditModel model, IReadOnlyList<NewProductImage> images, string? userId, CancellationToken ct = default);
    Task<Result> UpdateAsync(ProductEditModel model, IReadOnlyList<NewProductImage> newImages, string? userId, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, string? userId, CancellationToken ct = default);

    Task<Result> AddImagesAsync(int productId, IReadOnlyList<NewProductImage> images, CancellationToken ct = default);
    Task<Result> DeleteImageAsync(int productId, int imageId, CancellationToken ct = default);
    Task<Result> SetMainImageAsync(int productId, int imageId, CancellationToken ct = default);
}
