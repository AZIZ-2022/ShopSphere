using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(160).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.ImageUrl).HasMaxLength(400);
        b.Property(x => x.MetaTitle).HasMaxLength(200);
        b.Property(x => x.MetaDescription).HasMaxLength(300);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.Property(x => x.DeletedBy).HasMaxLength(256);

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.ParentCategoryId);

        b.HasOne(x => x.ParentCategory)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> b)
    {
        b.ToTable("Brands");
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(160).IsRequired();
        b.Property(x => x.LogoUrl).HasMaxLength(400);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.MetaTitle).HasMaxLength(200);
        b.Property(x => x.MetaDescription).HasMaxLength(300);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.Property(x => x.DeletedBy).HasMaxLength(256);

        b.HasIndex(x => x.Slug).IsUnique();
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products");
        b.Property(x => x.Name).HasMaxLength(250).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(260).IsRequired();
        b.Property(x => x.Sku).HasMaxLength(60).IsRequired();
        b.Property(x => x.ShortDescription).HasMaxLength(500);
        b.Property(x => x.Description).HasColumnType("nvarchar(max)");
        b.Property(x => x.MetaTitle).HasMaxLength(200);
        b.Property(x => x.MetaDescription).HasMaxLength(300);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.Property(x => x.DeletedBy).HasMaxLength(256);

        b.Property(x => x.Price).HasPrecision(18, 2);
        b.Property(x => x.DiscountPrice).HasPrecision(18, 2);
        b.Property(x => x.AverageRating).HasPrecision(3, 2);

        b.Ignore(x => x.EffectivePrice);

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.Sku).IsUnique();
        b.HasIndex(x => x.CategoryId);
        b.HasIndex(x => x.BrandId);
        b.HasIndex(x => new { x.IsActive, x.IsFeatured });
        b.HasIndex(x => new { x.IsActive, x.IsNewArrival });

        b.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Brand)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> b)
    {
        b.ToTable("ProductImages");
        b.Property(x => x.ImageUrl).HasMaxLength(400).IsRequired();
        b.Property(x => x.AltText).HasMaxLength(250);
        b.HasIndex(x => x.ProductId);

        b.HasOne(x => x.Product)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> b)
    {
        b.ToTable("ProductReviews");
        b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.ReviewerName).HasMaxLength(150);
        b.Property(x => x.Title).HasMaxLength(150);
        b.Property(x => x.Comment).HasMaxLength(2000).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);

        b.HasIndex(x => new { x.ProductId, x.IsApproved });
        b.HasIndex(x => x.UserId);

        b.HasOne(x => x.Product)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
