using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class SliderConfiguration : IEntityTypeConfiguration<Slider>
{
    public void Configure(EntityTypeBuilder<Slider> b)
    {
        b.ToTable("Sliders");
        b.Property(x => x.Title).HasMaxLength(150).IsRequired();
        b.Property(x => x.Subtitle).HasMaxLength(300);
        b.Property(x => x.ImageUrl).HasMaxLength(400).IsRequired();
        b.Property(x => x.MobileImageUrl).HasMaxLength(400);
        b.Property(x => x.LinkUrl).HasMaxLength(400);
        b.Property(x => x.ButtonText).HasMaxLength(60);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
    }
}

public class CmsPageConfiguration : IEntityTypeConfiguration<CmsPage>
{
    public void Configure(EntityTypeBuilder<CmsPage> b)
    {
        b.ToTable("CmsPages");
        b.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.BodyHtml).HasColumnType("nvarchar(max)");
        b.Property(x => x.MetaTitle).HasMaxLength(200);
        b.Property(x => x.MetaDescription).HasMaxLength(300);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

public class ConfigChangeLogConfiguration : IEntityTypeConfiguration<ConfigChangeLog>
{
    public void Configure(EntityTypeBuilder<ConfigChangeLog> b)
    {
        b.ToTable("ConfigChangeLogs");
        b.Property(x => x.EntityType).HasMaxLength(40).IsRequired();
        b.Property(x => x.EntityKey).HasMaxLength(100).IsRequired();
        b.Property(x => x.OldValueJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.NewValueJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => new { x.EntityType, x.CreatedAt });
    }
}

public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> b)
    {
        b.ToTable("FeatureFlags");
        b.Property(x => x.Key).HasMaxLength(60).IsRequired();
        b.Property(x => x.Description).HasMaxLength(300);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => x.Key).IsUnique();
    }
}

public class HomeSectionConfiguration : IEntityTypeConfiguration<HomeSection>
{
    public void Configure(EntityTypeBuilder<HomeSection> b)
    {
        b.ToTable("HomeSections");
        b.Property(x => x.Key).HasMaxLength(60).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200);
        b.Property(x => x.Subtitle).HasMaxLength(400);
        b.Property(x => x.ButtonText).HasMaxLength(60);
        b.Property(x => x.LinkUrl).HasMaxLength(400);
        b.Property(x => x.ConfigJson).HasMaxLength(4000);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => x.Key).IsUnique();
    }
}

public class TestimonialConfiguration : IEntityTypeConfiguration<Testimonial>
{
    public void Configure(EntityTypeBuilder<Testimonial> b)
    {
        b.ToTable("Testimonials");
        b.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Designation).HasMaxLength(150);
        b.Property(x => x.PhotoUrl).HasMaxLength(400);
        b.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
    }
}

public class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> b)
    {
        b.ToTable("NewsletterSubscribers");
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
    }
}

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> b)
    {
        b.ToTable("ContactMessages");
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(200);
        b.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        b.HasIndex(x => x.IsRead);
    }
}

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> b)
    {
        b.ToTable("SiteSettings");
        b.Property(x => x.Key).HasMaxLength(100).IsRequired();
        b.Property(x => x.Value).HasMaxLength(2000);
        b.Property(x => x.Description).HasMaxLength(300);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.HasIndex(x => x.Key).IsUnique();
    }
}
