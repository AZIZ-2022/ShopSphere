using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> b)
    {
        b.ToTable("Addresses");
        b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(30).IsRequired();
        b.Property(x => x.Line1).HasMaxLength(250).IsRequired();
        b.Property(x => x.Line2).HasMaxLength(250);
        b.Property(x => x.City).HasMaxLength(100).IsRequired();
        b.Property(x => x.State).HasMaxLength(100);
        b.Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
        b.Property(x => x.Country).HasMaxLength(100).IsRequired();
        b.Property(x => x.AddressType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.Property(x => x.DeletedBy).HasMaxLength(256);

        b.HasIndex(x => x.UserId);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");
        b.Property(x => x.OrderNumber).HasMaxLength(30).IsRequired();
        b.Property(x => x.UserId).HasMaxLength(450).IsRequired();

        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30);

        b.Property(x => x.SubTotal).HasPrecision(18, 2);
        b.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        b.Property(x => x.ShippingCost).HasPrecision(18, 2);
        b.Property(x => x.TaxAmount).HasPrecision(18, 2);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.ExchangeRate).HasPrecision(18, 6);
        b.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();

        b.Property(x => x.CouponCode).HasMaxLength(40);
        b.Property(x => x.CustomerNote).HasMaxLength(1000);

        b.Property(x => x.ShippingName).HasMaxLength(150).IsRequired();
        b.Property(x => x.ShippingPhone).HasMaxLength(30).IsRequired();
        b.Property(x => x.ShippingLine1).HasMaxLength(250).IsRequired();
        b.Property(x => x.ShippingLine2).HasMaxLength(250);
        b.Property(x => x.ShippingCity).HasMaxLength(100).IsRequired();
        b.Property(x => x.ShippingState).HasMaxLength(100);
        b.Property(x => x.ShippingPostalCode).HasMaxLength(20).IsRequired();
        b.Property(x => x.ShippingCountry).HasMaxLength(100).IsRequired();

        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);

        b.HasIndex(x => x.OrderNumber).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.PlacedAt);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("OrderItems");
        b.Property(x => x.ProductName).HasMaxLength(250).IsRequired();
        b.Property(x => x.Sku).HasMaxLength(60).IsRequired();
        b.Property(x => x.ImageUrl).HasMaxLength(400);
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.LineTotal).HasPrecision(18, 2);

        b.HasIndex(x => x.OrderId);

        b.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> b)
    {
        b.ToTable("OrderStatusHistories");
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.ChangedBy).HasMaxLength(256);

        b.HasIndex(x => x.OrderId);

        b.HasOne(x => x.Order)
            .WithMany(x => x.StatusHistory)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");
        b.Property(x => x.Method).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.TransactionId).HasMaxLength(100);
        b.Property(x => x.GatewayResponse).HasMaxLength(2000);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);

        b.HasIndex(x => x.OrderId);

        b.HasOne(x => x.Order)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> b)
    {
        b.ToTable("Coupons");
        b.Property(x => x.Code).HasMaxLength(40).IsRequired();
        b.Property(x => x.Description).HasMaxLength(250);
        b.Property(x => x.DiscountType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.DiscountValue).HasPrecision(18, 2);
        b.Property(x => x.MinOrderAmount).HasPrecision(18, 2);
        b.Property(x => x.MaxDiscountAmount).HasPrecision(18, 2);
        b.Property(x => x.CreatedBy).HasMaxLength(256);
        b.Property(x => x.UpdatedBy).HasMaxLength(256);
        b.Property(x => x.DeletedBy).HasMaxLength(256);

        b.HasIndex(x => x.Code).IsUnique();
    }
}
