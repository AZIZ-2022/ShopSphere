/* ============================================================================
   ECommerceDB.sql
   ----------------------------------------------------------------------------
   Single-file database setup for the ECommerce application.

   Contains:  1) database creation
              2) full schema (generated from EF Core migration InitialCreate)
              3) seed data (roles, admin user, categories, brands, products,
                 product images, sliders, testimonials, site settings, coupon)

   How to run:
     - sqlcmd:  sqlcmd -S localhost -E -i "Database\ECommerceDB.sql"
     - SSMS  :  open this file, enable "SQLCMD Mode" (Query menu), press F5

   Safe to run more than once: every object and every seed row is guarded.
   Default login ->  admin@ecommerce.local  /  Admin@123
   Generated: 2026-09-08
   ============================================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'ECommerceDB') IS NULL
BEGIN
    CREATE DATABASE [ECommerceDB];
END;
GO

USE [ECommerceDB];
GO

-- Required for the filtered unique indexes on the Identity tables.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ============================================================================
   SECTION 1 & 2 - SCHEMA  (EF Core migration: 20260908045042_InitialCreate)
   ============================================================================ */



IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [Addresses] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Phone] nvarchar(30) NOT NULL,
        [Line1] nvarchar(250) NOT NULL,
        [Line2] nvarchar(250) NULL,
        [City] nvarchar(100) NOT NULL,
        [State] nvarchar(100) NULL,
        [PostalCode] nvarchar(20) NOT NULL,
        [Country] nvarchar(100) NOT NULL,
        [AddressType] nvarchar(20) NOT NULL,
        [IsDefault] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_Addresses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [Brands] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(150) NOT NULL,
        [Slug] nvarchar(160) NOT NULL,
        [LogoUrl] nvarchar(400) NULL,
        [Description] nvarchar(1000) NULL,
        [MetaTitle] nvarchar(200) NULL,
        [MetaDescription] nvarchar(300) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_Brands] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [Carts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NULL,
        [AnonymousId] nvarchar(64) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_Carts] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(150) NOT NULL,
        [Slug] nvarchar(160) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [ImageUrl] nvarchar(400) NULL,
        [MetaTitle] nvarchar(200) NULL,
        [MetaDescription] nvarchar(300) NULL,
        [ParentCategoryId] int NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Categories_Categories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [ContactMessages] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(150) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [Subject] nvarchar(200) NULL,
        [Message] nvarchar(2000) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [Coupons] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(40) NOT NULL,
        [Description] nvarchar(250) NULL,
        [DiscountType] nvarchar(20) NOT NULL,
        [DiscountValue] decimal(18,2) NOT NULL,
        [MinOrderAmount] decimal(18,2) NOT NULL,
        [MaxDiscountAmount] decimal(18,2) NULL,
        [UsageLimit] int NULL,
        [UsedCount] int NOT NULL,
        [StartsAt] datetime2 NOT NULL,
        [EndsAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_Coupons] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [NewsletterSubscribers] (
        [Id] int NOT NULL IDENTITY,
        [Email] nvarchar(256) NOT NULL,
        [IsActive] bit NOT NULL,
        [SubscribedAt] datetime2 NOT NULL,
        [UnsubscribedAt] datetime2 NULL,
        CONSTRAINT [PK_NewsletterSubscribers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNumber] nvarchar(30) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [PaymentStatus] nvarchar(20) NOT NULL,
        [PaymentMethod] nvarchar(30) NOT NULL,
        [SubTotal] decimal(18,2) NOT NULL,
        [DiscountAmount] decimal(18,2) NOT NULL,
        [ShippingCost] decimal(18,2) NOT NULL,
        [TaxAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Orders_TaxAmount] DEFAULT 0,
        [TotalAmount] decimal(18,2) NOT NULL,
        [CurrencyCode] nvarchar(3) NOT NULL,
        [ExchangeRate] decimal(18,6) NOT NULL,
        [CouponCode] nvarchar(40) NULL,
        [CustomerNote] nvarchar(1000) NULL,
        [ShippingName] nvarchar(150) NOT NULL,
        [ShippingPhone] nvarchar(30) NOT NULL,
        [ShippingLine1] nvarchar(250) NOT NULL,
        [ShippingLine2] nvarchar(250) NULL,
        [ShippingCity] nvarchar(100) NOT NULL,
        [ShippingState] nvarchar(100) NULL,
        [ShippingPostalCode] nvarchar(20) NOT NULL,
        [ShippingCountry] nvarchar(100) NOT NULL,
        [PlacedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] nvarchar(450) NOT NULL,
        [Description] nvarchar(250) NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [SiteSettings] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(100) NOT NULL,
        [Value] nvarchar(2000) NULL,
        [Description] nvarchar(300) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [Sliders] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(150) NOT NULL,
        [Subtitle] nvarchar(300) NULL,
        [ImageUrl] nvarchar(400) NOT NULL,
        [MobileImageUrl] nvarchar(400) NULL,
        [LinkUrl] nvarchar(400) NULL,
        [ButtonText] nvarchar(60) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [StartsAt] datetime2 NULL,
        [EndsAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_Sliders] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = 'HomeSections'
)
BEGIN
    CREATE TABLE [HomeSections] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(60) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsVisible] bit NOT NULL,
        [Title] nvarchar(200) NULL,
        [Subtitle] nvarchar(400) NULL,
        [ButtonText] nvarchar(60) NULL,
        [LinkUrl] nvarchar(400) NULL,
        [ConfigJson] nvarchar(4000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_HomeSections] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [sys].[indexes] WHERE [name] = 'IX_HomeSections_Key' AND [object_id] = OBJECT_ID('[HomeSections]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_HomeSections_Key] ON [HomeSections] ([Key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = 'CmsPages'
)
BEGIN
    CREATE TABLE [CmsPages] (
        [Id] int NOT NULL IDENTITY,
        [Slug] nvarchar(100) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [BodyHtml] nvarchar(max) NOT NULL,
        [MetaTitle] nvarchar(200) NULL,
        [MetaDescription] nvarchar(300) NULL,
        [IsPublished] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_CmsPages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [sys].[indexes] WHERE [name] = 'IX_CmsPages_Slug' AND [object_id] = OBJECT_ID('[CmsPages]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_CmsPages_Slug] ON [CmsPages] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = 'FeatureFlags'
)
BEGIN
    CREATE TABLE [FeatureFlags] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(60) NOT NULL,
        [IsEnabled] bit NOT NULL,
        [Description] nvarchar(300) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_FeatureFlags] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [sys].[indexes] WHERE [name] = 'IX_FeatureFlags_Key' AND [object_id] = OBJECT_ID('[FeatureFlags]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_FeatureFlags_Key] ON [FeatureFlags] ([Key]);
END;
GO

INSERT INTO [FeatureFlags] ([Key], [IsEnabled], [Description], [CreatedAt])
SELECT v.[Key], 1, v.[Description], SYSUTCDATETIME()
FROM (VALUES
    (N'Wishlist',   N'Lets customers save products to a wishlist and move them to cart later.'),
    (N'Reviews',    N'Lets customers read and submit ratings/reviews on product pages.'),
    (N'Newsletter', N'Shows newsletter signup forms and accepts new subscriptions.')
) AS v([Key], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [FeatureFlags] f WHERE f.[Key] = v.[Key]);
GO

IF NOT EXISTS (
    SELECT * FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = 'ConfigChangeLogs'
)
BEGIN
    CREATE TABLE [ConfigChangeLogs] (
        [Id] int NOT NULL IDENTITY,
        [EntityType] nvarchar(40) NOT NULL,
        [EntityKey] nvarchar(100) NOT NULL,
        [OldValueJson] nvarchar(max) NULL,
        [NewValueJson] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_ConfigChangeLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [sys].[indexes] WHERE [name] = 'IX_ConfigChangeLogs_EntityType_CreatedAt' AND [object_id] = OBJECT_ID('[ConfigChangeLogs]')
)
BEGIN
    CREATE INDEX [IX_ConfigChangeLogs_EntityType_CreatedAt] ON [ConfigChangeLogs] ([EntityType], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [Testimonials] (
        [Id] int NOT NULL IDENTITY,
        [CustomerName] nvarchar(150) NOT NULL,
        [Designation] nvarchar(150) NULL,
        [PhotoUrl] nvarchar(400) NULL,
        [Message] nvarchar(1000) NOT NULL,
        [Rating] tinyint NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_Testimonials] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] nvarchar(450) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [ProfileImageUrl] nvarchar(400) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastLoginAt] datetime2 NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(250) NOT NULL,
        [Slug] nvarchar(260) NOT NULL,
        [Sku] nvarchar(60) NOT NULL,
        [ShortDescription] nvarchar(500) NULL,
        [Description] nvarchar(max) NULL,
        [MetaTitle] nvarchar(200) NULL,
        [MetaDescription] nvarchar(300) NULL,
        [CategoryId] int NOT NULL,
        [BrandId] int NULL,
        [Price] decimal(18,2) NOT NULL,
        [DiscountPrice] decimal(18,2) NULL,
        [StockQuantity] int NOT NULL,
        [IsFeatured] bit NOT NULL,
        [IsNewArrival] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [AverageRating] decimal(3,2) NOT NULL,
        [ReviewCount] int NOT NULL,
        [ViewCount] int NOT NULL,
        [SoldCount] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Brands_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brands] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderStatusHistories] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Note] nvarchar(500) NULL,
        [ChangedAt] datetime2 NOT NULL,
        [ChangedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_OrderStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderStatusHistories_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [Method] nvarchar(30) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [TransactionId] nvarchar(100) NULL,
        [GatewayResponse] nvarchar(2000) NULL,
        [PaidAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [RoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [UserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [UserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [UserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [CartItems] (
        [Id] int NOT NULL IDENTITY,
        [CartId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CartItems_Carts_CartId] FOREIGN KEY ([CartId]) REFERENCES [Carts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CartItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [ProductId] int NOT NULL,
        [ProductName] nvarchar(250) NOT NULL,
        [Sku] nvarchar(60) NOT NULL,
        [ImageUrl] nvarchar(400) NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [Quantity] int NOT NULL,
        [LineTotal] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductImages] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [ImageUrl] nvarchar(400) NOT NULL,
        [AltText] nvarchar(250) NULL,
        [IsMain] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK_ProductImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductReviews] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [ReviewerName] nvarchar(150) NULL,
        [Rating] tinyint NOT NULL,
        [Title] nvarchar(150) NULL,
        [Comment] nvarchar(2000) NOT NULL,
        [IsApproved] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_ProductReviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductReviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE TABLE [WishlistItems] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ProductId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_WishlistItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WishlistItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Addresses_UserId] ON [Addresses] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Brands_Slug] ON [Brands] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CartItems_CartId_ProductId] ON [CartItems] ([CartId], [ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CartItems_ProductId] ON [CartItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Carts_AnonymousId] ON [Carts] ([AnonymousId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Carts_UserId] ON [Carts] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Categories_ParentCategoryId] ON [Categories] ([ParentCategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Slug] ON [Categories] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ContactMessages_IsRead] ON [ContactMessages] ([IsRead]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Coupons_Code] ON [Coupons] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NewsletterSubscribers_Email] ON [NewsletterSubscribers] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_OrderNumber] ON [Orders] ([OrderNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_PlacedAt] ON [Orders] ([PlacedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_Status] ON [Orders] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderStatusHistories_OrderId] ON [OrderStatusHistories] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_OrderId] ON [Payments] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductImages_ProductId] ON [ProductImages] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductReviews_ProductId_IsApproved] ON [ProductReviews] ([ProductId], [IsApproved]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductReviews_UserId] ON [ProductReviews] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_BrandId] ON [Products] ([BrandId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_IsActive_IsFeatured] ON [Products] ([IsActive], [IsFeatured]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_IsActive_IsNewArrival] ON [Products] ([IsActive], [IsNewArrival]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Sku] ON [Products] ([Sku]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Slug] ON [Products] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RoleClaims_RoleId] ON [RoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SiteSettings_Key] ON [SiteSettings] ([Key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserClaims_UserId] ON [UserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserLogins_UserId] ON [UserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WishlistItems_ProductId] ON [WishlistItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WishlistItems_UserId_ProductId] ON [WishlistItems] ([UserId], [ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908045042_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260908045042_InitialCreate', N'8.0.10');
END;
GO

COMMIT;
GO




/* ============================================================================
   SEED DATA
   Idempotent: every block is guarded so the whole script can be re-run safely.
   Passwords: admin@ecommerce.local / Admin@123
   ============================================================================ */

SET NOCOUNT ON;
GO

/* ---------- Roles ------------------------------------------------------------ */
IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [NormalizedName] = N'ADMIN')
    INSERT INTO [Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp], [Description])
    VALUES (N'a1a1a1a1-aaaa-4aaa-8aaa-a1a1a1a1a1a1', N'Admin', N'ADMIN',
            CONVERT(nvarchar(36), NEWID()), N'Full administrative access');

IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [NormalizedName] = N'CUSTOMER')
    INSERT INTO [Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp], [Description])
    VALUES (N'c1c1c1c1-cccc-4ccc-8ccc-c1c1c1c1c1c1', N'Customer', N'CUSTOMER',
            CONVERT(nvarchar(36), NEWID()), N'Standard shopper');
GO

/* ---------- Admin user ----------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [NormalizedEmail] = N'ADMIN@ECOMMERCE.LOCAL')
    INSERT INTO [Users]
        ([Id], [FullName], [ProfileImageUrl], [IsActive], [CreatedAt], [LastLoginAt],
         [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed],
         [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber],
         [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount])
    VALUES
        (N'11111111-1111-1111-1111-111111111111', N'Store Administrator', NULL, 1, SYSUTCDATETIME(), NULL,
         N'admin@ecommerce.local', N'ADMIN@ECOMMERCE.LOCAL', N'admin@ecommerce.local', N'ADMIN@ECOMMERCE.LOCAL', 1,
         N'AQAAAAIAAYagAAAAELBVPqNq81FQS9+OeV1o3JVrJHUmedbjA2IIlMVc3w6R9pVbl83ccCRcyz+G7slDNw==',
         N'7Q6ADMINSECURITYSTAMP0000000000000', CONVERT(nvarchar(36), NEWID()), NULL,
         0, 0, NULL, 1, 0);
GO

IF NOT EXISTS (SELECT 1 FROM [UserRoles]
               WHERE [UserId] = N'11111111-1111-1111-1111-111111111111'
                 AND [RoleId] = N'a1a1a1a1-aaaa-4aaa-8aaa-a1a1a1a1a1a1')
    INSERT INTO [UserRoles] ([UserId], [RoleId])
    VALUES (N'11111111-1111-1111-1111-111111111111', N'a1a1a1a1-aaaa-4aaa-8aaa-a1a1a1a1a1a1');
GO

/* ---------- Site settings ------------------------------------------------- */
INSERT INTO [SiteSettings] ([Key], [Value], [Description], [CreatedAt])
SELECT v.[Key], v.[Value], v.[Description], SYSUTCDATETIME()
FROM (VALUES
    (N'Site.Name',              N'ShopSphere',                               N'Store display name'),
    (N'Site.Tagline',           N'Everything you need, delivered.',          N'Short tagline'),
    (N'Site.LogoUrl',           N'',                                         N'Header logo image URL (leave blank to show the text mark)'),
    (N'Site.FaviconUrl',        N'',                                         N'Browser tab icon URL (leave blank to use the default)'),
    (N'Contact.Email',          N'support@shopsphere.local',                 N'Support email'),
    (N'Contact.Phone',          N'+880 1700-000000',                         N'Support phone'),
    (N'Contact.Address',        N'House 12, Road 5, Dhaka 1207, Bangladesh', N'Business address'),
    (N'Social.Facebook',        N'https://facebook.com',                     N'Facebook URL'),
    (N'Social.Instagram',       N'https://instagram.com',                    N'Instagram URL'),
    (N'Social.Twitter',         N'https://twitter.com',                      N'Twitter/X URL'),
    (N'Currency.Base',          N'USD',                                      N'Base currency all prices are stored in'),
    (N'Currency.Supported',     N'USD,BDT',                                  N'Comma separated currency codes'),
    (N'Currency.Rate.BDT',      N'120.00',                                   N'1 USD in BDT'),
    (N'Shipping.FreeThreshold', N'100',                                      N'Free shipping over this base-currency amount'),
    (N'Shipping.FlatRate',      N'5',                                        N'Flat shipping cost below the free threshold'),
    (N'Theme.PrimaryColor',     N'#2563eb',                                  N'Primary brand colour (hex)'),
    (N'Theme.AccentColor',      N'#ef4444',                                  N'Accent / sale colour (hex)'),
    (N'Theme.FontFamily',       N'Inter',                                    N'Site font: Inter, Roboto, Poppins, Nunito, or OpenSans'),
    (N'Theme.ButtonStyle',      N'rounded',                                  N'Button corner style: rounded, pill, or square'),
    (N'Tax.Rate',               N'0',                                        N'Sales tax / VAT percentage applied to the discounted subtotal'),
    (N'Payment.EnableCod',      N'true',                                     N'Allow Cash on Delivery at checkout'),
    (N'Payment.EnableOnlineGateway', N'true',                                N'Allow online payment at checkout'),
    (N'Store.BusinessHours',    N'',                                         N'Shown on the Contact page, e.g. Mon-Sat, 9:00 AM - 8:00 PM')
) AS v([Key], [Value], [Description])
WHERE NOT EXISTS (SELECT 1 FROM [SiteSettings] s WHERE s.[Key] = v.[Key]);
GO

/* ---------- Homepage sections ---------------------------------------------- */
INSERT INTO [HomeSections] ([Key], [DisplayOrder], [IsVisible], [Title], [Subtitle], [ButtonText], [LinkUrl], [CreatedAt])
SELECT v.[Key], v.[DisplayOrder], 1, v.[Title], v.[Subtitle], v.[ButtonText], v.[LinkUrl], SYSUTCDATETIME()
FROM (VALUES
    (N'HeroSlider',          10,  CAST(NULL AS nvarchar(200)), CAST(NULL AS nvarchar(400)), CAST(NULL AS nvarchar(60)), CAST(NULL AS nvarchar(400))),
    (N'FeatureStrip',        20,  CAST(NULL AS nvarchar(200)), CAST(NULL AS nvarchar(400)), CAST(NULL AS nvarchar(60)), CAST(NULL AS nvarchar(400))),
    (N'FeaturedCategories',  30,  N'Shop by category',                    N'Browse our most popular departments',         CAST(NULL AS nvarchar(60)), CAST(NULL AS nvarchar(400))),
    (N'Deals',               40,  N'Deals of the day',                    CAST(NULL AS nvarchar(400)),                    N'View all deals', N'/deals'),
    (N'FeaturedProducts',    50,  N'Featured products',                   N'Hand-picked favourites',                       N'See more',     N'/search?sort=5'),
    (N'NewArrivals',         60,  N'New arrivals',                        N'Just landed in store',                         N'See more',     N'/search?sort=3'),
    (N'PromoBanner',         70,  N'Save big across every category',      N'Discounts refreshed daily — don''t miss out.', N'Shop the sale', N'/deals'),
    (N'BestSellers',         80,  N'Best sellers',                        N'What everyone''s buying',                      N'See more',     N'/search?sort=5'),
    (N'Testimonials',        90,  N'What our customers say',              CAST(NULL AS nvarchar(400)),                    CAST(NULL AS nvarchar(60)), CAST(NULL AS nvarchar(400))),
    (N'Brands',              100, N'Popular brands',                      CAST(NULL AS nvarchar(400)),                    CAST(NULL AS nvarchar(60)), CAST(NULL AS nvarchar(400))),
    (N'Newsletter',          110, N'Join our newsletter',                 N'Be first to hear about new arrivals, exclusive offers and sales.', CAST(NULL AS nvarchar(60)), CAST(NULL AS nvarchar(400)))
) AS v([Key], [DisplayOrder], [Title], [Subtitle], [ButtonText], [LinkUrl])
WHERE NOT EXISTS (SELECT 1 FROM [HomeSections] s WHERE s.[Key] = v.[Key]);
GO

/* ---------- CMS pages ------------------------------------------------------ */
INSERT INTO [CmsPages] ([Slug], [Title], [BodyHtml], [IsPublished], [CreatedAt])
SELECT v.[Slug], v.[Title], v.[BodyHtml], 1, SYSUTCDATETIME()
FROM (VALUES
    (N'privacy', N'Privacy Policy',
        N'<p class="text-muted">This is a demo store. The policy below describes how the application handles data.</p>
        <h2>What we store</h2>
        <ul>
            <li><strong>Account:</strong> your name, email, phone and a securely hashed password.</li>
            <li><strong>Orders:</strong> order contents, amounts and a snapshot of the delivery address at the time of purchase.</li>
            <li><strong>Addresses:</strong> the entries you save to your address book.</li>
            <li><strong>Cart &amp; wishlist:</strong> items you add, linked to your account or a guest cookie.</li>
        </ul>
        <h2>How it''s used</h2>
        <ul>
            <li>To process and deliver your orders and let you track them.</li>
            <li>To show prices in your selected currency (a cookie stores that choice).</li>
            <li>Newsletter subscriptions are only used to send store updates and can be cancelled any time.</li>
        </ul>
        <h2>Payments</h2>
        <p>Online payments in this demo go through a simulated gateway. No real card data is stored or charged.</p>
        <h2>Contact</h2>
        <p>For any data request, <a href="/contact">contact us</a>.</p>'),
    (N'shipping', N'Shipping & Returns',
        N'<h2>Shipping</h2>
        <ul>
            <li>Orders are processed within 1–2 business days.</li>
            <li>Standard delivery takes 3–5 business days depending on your location.</li>
            <li>Free shipping applies automatically once your subtotal reaches the threshold shown in the top bar; otherwise a small flat rate applies and is shown at checkout.</li>
            <li>You can follow every status change (Confirmed → Processing → Shipped → Delivered) on your order page.</li>
        </ul>
        <h2>Returns</h2>
        <ul>
            <li>Request a return within <strong>7 days</strong> of delivery from your order detail page, or by contacting support with your order number.</li>
            <li>Items should be unused and in their original packaging.</li>
            <li>Once approved, the product is restocked and any online payment is refunded to the original method.</li>
            <li>Cash-on-Delivery orders are refunded via the details you provide when the return is approved.</li>
        </ul>
        <p>Questions? <a href="/contact">Get in touch</a>.</p>'),
    (N'faq', N'Frequently Asked Questions',
        N'<div class="faq">
            <details open>
                <summary>How do I place an order?</summary>
                <p>Add items to your cart, go to checkout, choose a delivery address and a payment
                method (Cash on Delivery or online), then place your order. You''ll get an order number
                and can track it any time.</p>
            </details>
            <details>
                <summary>What payment methods are available?</summary>
                <p>Cash on Delivery, and online card payment via our secure gateway. Online orders are
                confirmed once payment succeeds.</p>
            </details>
            <details>
                <summary>Can I track my order without an account?</summary>
                <p>Yes — use <a href="/order/track">Track Order</a> with your order
                number and the email you used at checkout.</p>
            </details>
            <details>
                <summary>How do returns work?</summary>
                <p>Contact us within 7 days of delivery. Once your return is approved the item is restocked
                and, for online payments, your refund is processed.</p>
            </details>
            <details>
                <summary>Do you offer free shipping?</summary>
                <p>Yes, on orders above the threshold shown in the top bar. Smaller orders have a small flat
                shipping fee, shown at checkout.</p>
            </details>
        </div>'),
    (N'about-us', N'About Us',
        N'<p class="text-muted">This is a demo e-commerce store built to showcase the platform.</p>
        <p>We''re focused on making online shopping simple: a clear catalog, transparent pricing,
        fast checkout, and order tracking every step of the way.</p>
        <h2>Our promise</h2>
        <ul>
            <li>Quality products at fair prices.</li>
            <li>Clear shipping and return policies — no surprises.</li>
            <li>Responsive support when you need help.</li>
        </ul>
        <p>Have a question? <a href="/contact">Contact us</a> any time.</p>'),
    (N'terms', N'Terms & Conditions',
        N'<p class="text-muted">These terms govern your use of this store. This is demo/placeholder text — replace it with your business''s actual terms.</p>
        <h2>Orders & payment</h2>
        <p>By placing an order you confirm the details provided are accurate. Prices are shown in your
        selected currency at the exchange rate in effect at checkout.</p>
        <h2>Shipping & returns</h2>
        <p>See our <a href="/shipping">Shipping &amp; Returns</a> page for delivery times and the return window.</p>
        <h2>Account use</h2>
        <p>You''re responsible for keeping your account credentials confidential and for all activity under your account.</p>
        <h2>Changes</h2>
        <p>We may update these terms from time to time; continued use of the store after a change means you accept the update.</p>')
) AS v([Slug], [Title], [BodyHtml])
WHERE NOT EXISTS (SELECT 1 FROM [CmsPages] p WHERE p.[Slug] = v.[Slug]);
GO

/* ---------- Categories --------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM [Categories])
BEGIN
    SET IDENTITY_INSERT [Categories] ON;
    INSERT INTO [Categories] ([Id], [Name], [Slug], [Description], [ParentCategoryId], [DisplayOrder], [IsActive], [CreatedAt], [IsDeleted])
    VALUES
        (1, N'Electronics',   N'electronics',  N'Phones, laptops, audio and accessories', NULL, 1, 1, SYSUTCDATETIME(), 0),
        (2, N'Fashion',       N'fashion',      N'Clothing, footwear and accessories',     NULL, 2, 1, SYSUTCDATETIME(), 0),
        (3, N'Home & Living', N'home-living',  N'Furniture, kitchen and decor',           NULL, 3, 1, SYSUTCDATETIME(), 0),
        (4, N'Beauty',        N'beauty',       N'Skincare, makeup and fragrance',         NULL, 4, 1, SYSUTCDATETIME(), 0),
        (5, N'Sports',        N'sports',       N'Fitness, outdoor and gear',              NULL, 5, 1, SYSUTCDATETIME(), 0),
        (6, N'Books',         N'books',        N'Fiction, non-fiction and children',      NULL, 6, 1, SYSUTCDATETIME(), 0),
        (7,  N'Smartphones',  N'smartphones',  NULL, 1, 1, 1, SYSUTCDATETIME(), 0),
        (8,  N'Laptops',      N'laptops',      NULL, 1, 2, 1, SYSUTCDATETIME(), 0),
        (9,  N'Audio',        N'audio',        NULL, 1, 3, 1, SYSUTCDATETIME(), 0),
        (10, N'Men',          N'men',          NULL, 2, 1, 1, SYSUTCDATETIME(), 0),
        (11, N'Women',        N'women',        NULL, 2, 2, 1, SYSUTCDATETIME(), 0),
        (12, N'Kitchen',      N'kitchen',      NULL, 3, 1, 1, SYSUTCDATETIME(), 0),
        (13, N'Furniture',    N'furniture',    NULL, 3, 2, 1, SYSUTCDATETIME(), 0);
    SET IDENTITY_INSERT [Categories] OFF;
END
GO

/* ---------- Brands ----------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM [Brands])
BEGIN
    SET IDENTITY_INSERT [Brands] ON;
    INSERT INTO [Brands] ([Id], [Name], [Slug], [DisplayOrder], [IsActive], [CreatedAt], [IsDeleted])
    VALUES
        (1, N'Aurora',     N'aurora',     1, 1, SYSUTCDATETIME(), 0),
        (2, N'NimbusTech',  N'nimbustech', 2, 1, SYSUTCDATETIME(), 0),
        (3, N'Vantage',     N'vantage',    3, 1, SYSUTCDATETIME(), 0),
        (4, N'Meadow',      N'meadow',     4, 1, SYSUTCDATETIME(), 0),
        (5, N'PulseFit',    N'pulsefit',   5, 1, SYSUTCDATETIME(), 0);
    SET IDENTITY_INSERT [Brands] OFF;
END
GO

/* ---------- Products -------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM [Products])
BEGIN
    SET IDENTITY_INSERT [Products] ON;
    INSERT INTO [Products]
        ([Id], [Name], [Slug], [Sku], [ShortDescription], [Description], [CategoryId], [BrandId],
         [Price], [DiscountPrice], [StockQuantity], [IsFeatured], [IsNewArrival], [IsActive],
         [AverageRating], [ReviewCount], [ViewCount], [SoldCount], [CreatedAt], [IsDeleted])
    SELECT
        v.[Id], v.[Name], v.[Slug], v.[Sku], v.[ShortDescription],
        N'<p>' + v.[Name] + N' is part of our curated range. Durable materials, thoughtful design and a warranty you can rely on.</p>',
        v.[CategoryId], v.[BrandId], v.[Price], v.[DiscountPrice], v.[StockQuantity],
        v.[IsFeatured], v.[IsNewArrival], 1, v.[AverageRating], v.[ReviewCount], v.[ViewCount], v.[SoldCount],
        SYSUTCDATETIME(), 0
    FROM (VALUES
        (1,  N'Aurora Wireless Headphones',        N'aurora-wireless-headphones',        N'SKU-1001', N'Premium over-ear wireless headphones',   9,  1,  129.99, 106.59, 45,  1, 1, 4.60, 128, 2400, 320),
        (2,  N'NimbusTech 5G Smartphone X',        N'nimbustech-5g-smartphone-x',        N'SKU-1002', N'Flagship 5G phone with AMOLED display',  7,  2,  699.00, NULL,   30,  0, 0, 4.40, 210, 5200, 610),
        (3,  N'Vantage UltraBook 14',              N'vantage-ultrabook-14',              N'SKU-1003', N'Thin and light 14-inch laptop',         8,  3,  980.00, NULL,   18,  0, 0, 4.55, 96,  3100, 140),
        (4,  N'Aurora Bluetooth Speaker Mini',     N'aurora-bluetooth-speaker-mini',     N'SKU-1004', N'Pocket speaker with deep bass',         9,  1,  39.99,  32.79,  120, 0, 0, 4.20, 64,  1800, 430),
        (5,  N'NimbusTech Smartwatch Pulse',       N'nimbustech-smartwatch-pulse',       N'SKU-1005', N'Fitness and heart-rate tracking',       1,  2,  149.00, NULL,   54,  1, 0, 4.30, 88,  2600, 260),
        (6,  N'Vantage 4K Action Camera',          N'vantage-4k-action-camera',          N'SKU-1006', N'Waterproof 4K adventure camera',        1,  3,  199.00, NULL,   26,  0, 1, 4.10, 40,  1500, 90),
        (7,  N'Meadow Cotton Crew T-Shirt',        N'meadow-cotton-crew-t-shirt',        N'SKU-1007', N'Soft 100% cotton everyday tee',         10, 4,  14.99,  12.29,  300, 0, 0, 4.50, 320, 4200, 1500),
        (8,  N'Vantage Slim-Fit Denim Jeans',      N'vantage-slim-fit-denim-jeans',      N'SKU-1008', N'Stretch denim with a modern cut',       10, 3,  44.99,  NULL,   140, 0, 0, 4.25, 150, 2100, 520),
        (9,  N'Aurora Running Sneakers',           N'aurora-running-sneakers',           N'SKU-1009', N'Lightweight cushioned running shoes',   10, 1,  69.99,  NULL,   85,  1, 0, 4.60, 205, 3300, 700),
        (10, N'Meadow Linen Summer Dress',         N'meadow-linen-summer-dress',         N'SKU-1010', N'Breathable linen midi dress',           11, 4,  54.00,  44.28,  60,  0, 0, 4.40, 70,  1600, 210),
        (11, N'Vantage Leather Belt Classic',      N'vantage-leather-belt-classic',      N'SKU-1011', N'Full-grain leather belt',               10, 3,  24.99,  NULL,   200, 0, 1, 4.30, 55,  900,  340),
        (12, N'Aurora Canvas Backpack',            N'aurora-canvas-backpack',            N'SKU-1012', N'20L water-resistant daypack',           2,  1,  49.99,  NULL,   95,  0, 0, 4.45, 130, 2000, 480),
        (13, N'Meadow Ceramic Dinner Set 16pc',    N'meadow-ceramic-dinner-set-16pc',    N'SKU-1013', N'Chip-resistant stoneware, service for 4',12, 4,  89.00,  72.98,  40,  1, 0, 4.55, 60,  1400, 150),
        (14, N'Vantage Non-Stick Fry Pan 28cm',    N'vantage-non-stick-fry-pan-28cm',    N'SKU-1014', N'PFOA-free non-stick coating',           12, 3,  27.50,  NULL,   150, 0, 0, 4.20, 90,  1700, 610),
        (15, N'Meadow Oak Coffee Table',           N'meadow-oak-coffee-table',           N'SKU-1015', N'Solid oak with storage shelf',          13, 4,  219.00, NULL,   12,  0, 0, 4.60, 24,  800,  40),
        (16, N'Aurora LED Desk Lamp',              N'aurora-led-desk-lamp',              N'SKU-1016', N'Dimmable with USB charging port',       13, 1,  34.99,  28.69,  110, 1, 1, 4.35, 140, 2200, 520),
        (17, N'Meadow Memory Foam Pillow',         N'meadow-memory-foam-pillow',         N'SKU-1017', N'Contour support, washable cover',       13, 4,  29.99,  NULL,   180, 1, 0, 4.40, 175, 2500, 760),
        (18, N'Vantage Stainless Cookware Set',    N'vantage-stainless-cookware-set',    N'SKU-1018', N'10-piece tri-ply stainless set',        12, 3,  159.00, NULL,   22,  0, 0, 4.50, 48,  1100, 90),
        (19, N'Aurora Vitamin C Face Serum',       N'aurora-vitamin-c-face-serum',       N'SKU-1019', N'Brightening antioxidant serum',         4,  1,  19.99,  16.39,  260, 0, 0, 4.30, 230, 3800, 1200),
        (20, N'Meadow Hydrating Day Cream',        N'meadow-hydrating-day-cream',        N'SKU-1020', N'Lightweight SPF 30 moisturiser',        4,  4,  22.00,  NULL,   210, 1, 0, 4.35, 160, 2700, 640),
        (21, N'Aurora Matte Lipstick Trio',       N'aurora-matte-lipstick-trio',        N'SKU-1021', N'Three long-wear nude shades',           4,  1,  27.00,  NULL,   130, 1, 1, 4.25, 95,  1900, 410),
        (22, N'PulseFit Adjustable Dumbbell 20kg', N'pulsefit-adjustable-dumbbell-20kg', N'SKU-1022', N'Space-saving 2.5-20kg per hand',        5,  5,  149.00, 122.18, 35,  1, 0, 4.65, 78,  2100, 190),
        (23, N'PulseFit Yoga Mat Pro',             N'pulsefit-yoga-mat-pro',             N'SKU-1023', N'6mm non-slip cushioned mat',            5,  5,  34.99,  NULL,   160, 0, 0, 4.45, 210, 3000, 880),
        (24, N'PulseFit Insulated Water Bottle 1L',N'pulsefit-insulated-water-bottle-1l',N'SKU-1024', N'24h cold / 12h hot, leak-proof',        5,  5,  18.99,  NULL,   320, 0, 0, 4.50, 280, 4100, 1600),
        (25, N'The Silent Horizon (Novel)',        N'the-silent-horizon-novel',          N'SKU-1025', N'Bestselling literary mystery',          6,  NULL, 12.99, 10.65,  500, 1, 0, 4.40, 340, 5000, 2100),
        (26, N'Clean Code Habits (Paperback)',     N'clean-code-habits-paperback',       N'SKU-1026', N'Practical guide for developers',        6,  NULL, 21.99, NULL,   240, 0, 1, 4.55, 190, 3600, 900)
    ) AS v([Id], [Name], [Slug], [Sku], [ShortDescription], [CategoryId], [BrandId],
           [Price], [DiscountPrice], [StockQuantity], [IsFeatured], [IsNewArrival],
           [AverageRating], [ReviewCount], [ViewCount], [SoldCount]);
    SET IDENTITY_INSERT [Products] OFF;
END
GO

/* ---------- Product images (one main image per product) --------------- */
IF NOT EXISTS (SELECT 1 FROM [ProductImages])
BEGIN
    INSERT INTO [ProductImages] ([ProductId], [ImageUrl], [AltText], [IsMain], [DisplayOrder])
    SELECT p.[Id],
           N'/images/products/sample-' + CAST(((p.[Id] - 1) % 12) + 1 AS nvarchar(2)) + N'.jpg',
           p.[Name], 1, 0
    FROM [Products] p;
END
GO

/* ---------- Sliders -------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM [Sliders])
BEGIN
    SET IDENTITY_INSERT [Sliders] ON;
    INSERT INTO [Sliders] ([Id], [Title], [Subtitle], [ImageUrl], [LinkUrl], [ButtonText], [DisplayOrder], [IsActive], [CreatedAt])
    VALUES
        (1, N'Big Season Sale',        N'Up to 40% off across Electronics',    N'/images/sliders/slide-1.jpg', N'/c/electronics',  N'Shop Deals',   1, 1, SYSUTCDATETIME()),
        (2, N'New Arrivals Are Here',  N'Fresh styles for the new season',     N'/images/sliders/slide-2.jpg', N'/c/fashion',      N'Explore',      2, 1, SYSUTCDATETIME()),
        (3, N'Upgrade Your Home',      N'Comfort and style for every room',    N'/images/sliders/slide-3.jpg', N'/c/home-living',  N'Browse Home',  3, 1, SYSUTCDATETIME());
    SET IDENTITY_INSERT [Sliders] OFF;
END
GO

/* ---------- Testimonials ------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM [Testimonials])
BEGIN
    SET IDENTITY_INSERT [Testimonials] ON;
    INSERT INTO [Testimonials] ([Id], [CustomerName], [Designation], [Message], [Rating], [DisplayOrder], [IsActive], [CreatedAt])
    VALUES
        (1, N'Farhana R.', N'Verified Buyer', N'Fast delivery and the product quality exceeded my expectations.', 5, 1, 1, SYSUTCDATETIME()),
        (2, N'Tanvir A.',  N'Verified Buyer', N'Great prices and the checkout was smooth. Will order again.',     5, 2, 1, SYSUTCDATETIME()),
        (3, N'Nabila K.',  N'Verified Buyer', N'Customer support helped me swap a size with zero hassle.',        4, 3, 1, SYSUTCDATETIME()),
        (4, N'Sabbir H.',  N'Verified Buyer', N'The packaging was premium and everything arrived intact.',        5, 4, 1, SYSUTCDATETIME());
    SET IDENTITY_INSERT [Testimonials] OFF;
END
GO

/* ---------- Coupon ------------------------------------------------ */
IF NOT EXISTS (SELECT 1 FROM [Coupons] WHERE [Code] = N'WELCOME10')
BEGIN
    SET IDENTITY_INSERT [Coupons] ON;
    INSERT INTO [Coupons] ([Id], [Code], [Description], [DiscountType], [DiscountValue],
        [MinOrderAmount], [MaxDiscountAmount], [UsageLimit], [UsedCount], [StartsAt], [EndsAt], [IsActive], [CreatedAt], [IsDeleted])
    VALUES
        (1, N'WELCOME10', N'10% off your first order (min $50)', N'Percentage', 10.00,
         50.00, 30.00, 1000, 0, '2026-01-01T00:00:00', '2026-12-31T23:59:59', 1, SYSUTCDATETIME(), 0);
    SET IDENTITY_INSERT [Coupons] OFF;
END
GO

PRINT 'ECommerceDB seed complete.';
GO



