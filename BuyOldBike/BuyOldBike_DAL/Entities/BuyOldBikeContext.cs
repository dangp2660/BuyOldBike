using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BuyOldBike_DAL.Entities;

public partial class BuyOldBikeContext : DbContext
{
    public BuyOldBikeContext()
    {
        EnsureSchema();
    }

    public BuyOldBikeContext(DbContextOptions<BuyOldBikeContext> options)
        : base(options)
    {
        EnsureSchema();
    }

    private static bool _schemaEnsured;
    private static readonly object _schemaLock = new();

    private void EnsureSchema()
    {
        if (_schemaEnsured) return;
        lock (_schemaLock)
        {
            if (_schemaEnsured) return;

            Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'dbo.listings', N'U') IS NOT NULL AND COL_LENGTH('dbo.listings', 'views') IS NULL
BEGIN
    ALTER TABLE dbo.listings
    ADD views INT NOT NULL CONSTRAINT DF_listings_views DEFAULT (0) WITH VALUES;
END

IF OBJECT_ID(N'dbo.return_request_images', N'U') IS NOT NULL AND COL_LENGTH('dbo.return_request_images', 'uploader_role') IS NULL
BEGIN
    ALTER TABLE dbo.return_request_images
    ADD uploader_role VARCHAR(20) NOT NULL CONSTRAINT DF_return_request_images_uploader_role DEFAULT ('Buyer') WITH VALUES;
END

IF OBJECT_ID(N'dbo.inspection_images', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.inspection_images
    (
        image_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_inspection_images_image_id DEFAULT (NEWID()),
        inspection_id UNIQUEIDENTIFIER NOT NULL,
        image_url VARCHAR(500) NOT NULL,
        created_at DATETIME NULL,
        CONSTRAINT PK_inspection_images PRIMARY KEY (image_id),
        CONSTRAINT FK_inspection_images_inspections
            FOREIGN KEY (inspection_id) REFERENCES dbo.inspections(inspection_id)
    );

    CREATE INDEX IX_inspection_images_inspection_id ON dbo.inspection_images(inspection_id);
END

IF OBJECT_ID(N'dbo.withdrawal_requests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.withdrawal_requests
    (
        withdrawal_request_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_withdrawal_requests_withdrawal_request_id DEFAULT (NEWID()),
        user_id UNIQUEIDENTIFIER NOT NULL,
        amount DECIMAL(18, 2) NOT NULL,
        bank_name NVARCHAR(255) NOT NULL,
        account_number VARCHAR(50) NOT NULL,
        account_name NVARCHAR(255) NOT NULL,
        status VARCHAR(20) NOT NULL CONSTRAINT DF_withdrawal_requests_status DEFAULT ('Pending'),
        created_at DATETIME NOT NULL CONSTRAINT DF_withdrawal_requests_created_at DEFAULT (GETDATE()),
        confirmed_at DATETIME NULL,
        CONSTRAINT PK_withdrawal_requests PRIMARY KEY (withdrawal_request_id),
        CONSTRAINT FK_withdrawal_requests_users FOREIGN KEY (user_id) REFERENCES dbo.users(user_id)
    );

    CREATE INDEX IX_withdrawal_requests_user_id ON dbo.withdrawal_requests(user_id);
    CREATE INDEX IX_withdrawal_requests_status ON dbo.withdrawal_requests(status);
END
");

            _schemaEnsured = true;
        }
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Inspection> Inspections { get; set; }

    public virtual DbSet<InspectionComponent> InspectionComponents { get; set; }

    public virtual DbSet<InspectionImage> InspectionImages { get; set; }

    public virtual DbSet<InspectionLocation> InspectionLocations { get; set; }

    public virtual DbSet<InspectionScore> InspectionScores { get; set; }

    public virtual DbSet<KycImage> KycImages { get; set; }

    public virtual DbSet<KycProfile> KycProfiles { get; set; }

    public virtual DbSet<Listing> Listings { get; set; }

    public virtual DbSet<ListingImage> ListingImages { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<UserWallet> UserWallets { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }

    public virtual DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }

    public virtual DbSet<ReturnRequest> ReturnRequests { get; set; }

    public virtual DbSet<ReturnRequestImage> ReturnRequestImages { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<SellerProfile> SellerProfiles { get; set; }

    public virtual DbSet<Type> Types { get; set; }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<FrameSize> FrameSizes { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__addresse__CAA247C8EC1E0399");

            entity.ToTable("addresses");

            entity.HasIndex(e => e.UserId, "UQ__addresse__B9BE370E41395CC7").IsUnique();

            entity.Property(e => e.AddressId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("address_id");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.Detail)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("detail");
            entity.Property(e => e.District)
                .HasMaxLength(100)
                .HasColumnName("district");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone_number");
            entity.Property(e => e.Province)
                .HasMaxLength(100)
                .HasColumnName("province");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Ward)
                .HasMaxLength(100)
                .HasColumnName("ward");

            entity.HasOne(d => d.User).WithOne(p => p.Address)
                .HasForeignKey<Address>(d => d.UserId)
                .HasConstraintName("FK__addresses__user___5812160E");
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandId).HasName("PK__brands__5E5A8E27B6C6C1A3");

            entity.ToTable("brands");

            entity.HasIndex(e => e.BrandName, "UQ__brands__0C0C3B58C071D52F").IsUnique();

            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.BrandName)
                .HasMaxLength(100)
                .HasColumnName("brand_name");
        });

        modelBuilder.Entity<Inspection>(entity =>
        {
            entity.HasKey(e => e.InspectionId).HasName("PK__inspecti__C3C4E743462EB6BA");

            entity.ToTable("inspections");

            entity.Property(e => e.InspectionId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("inspection_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.InspectionLocationId).HasColumnName("inspection_location_id");
            entity.Property(e => e.InspectionTypeId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("inspection_type_id");
            entity.Property(e => e.InspectorId).HasColumnName("inspector_id");
            entity.Property(e => e.ListingId).HasColumnName("listing_id");
            entity.Property(e => e.OverallScore).HasColumnName("overall_score");
            entity.Property(e => e.RejectReason)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("reject_reason");
            entity.Property(e => e.Result)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("result");
            entity.Property(e => e.ScheduledAt)
                .HasColumnType("datetime")
                .HasColumnName("scheduled_at");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.InspectionLocation).WithMany(p => p.Inspections)
                .HasForeignKey(d => d.InspectionLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__inspectio__inspe__44FF419A");

            entity.HasOne(d => d.Inspector).WithMany(p => p.Inspections)
                .HasForeignKey(d => d.InspectorId)
                .HasConstraintName("FK__inspectio__inspe__440B1D61");

            entity.HasOne(d => d.Listing).WithMany(p => p.Inspections)
                .HasForeignKey(d => d.ListingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__inspectio__listi__4316F928");
        });

        modelBuilder.Entity<InspectionImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__inspecti__DC9AC955B4F128C8");

            entity.ToTable("inspection_images");

            entity.Property(e => e.ImageId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("image_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("image_url");
            entity.Property(e => e.InspectionId).HasColumnName("inspection_id");

            entity.HasOne(d => d.Inspection).WithMany(p => p.InspectionImages)
                .HasForeignKey(d => d.InspectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_inspection_images_inspections");
        });

        modelBuilder.Entity<InspectionComponent>(entity =>
        {
            entity.HasKey(e => e.ComponentId).HasName("PK__inspecti__AEB1DA5903A6342E");

            entity.ToTable("inspection_components");

            entity.Property(e => e.ComponentId)
                .ValueGeneratedNever()
                .HasColumnName("component_id");
            entity.Property(e => e.ComponentName)
                .HasMaxLength(255)
                .HasColumnName("component_name");
        });

        modelBuilder.Entity<InspectionLocation>(entity =>
        {
            entity.HasKey(e => e.InspectionLocationId).HasName("PK__inspecti__496CB5FF03F872EB");

            entity.ToTable("inspection_locations");

            entity.Property(e => e.InspectionLocationId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("inspection_location_id");
            entity.Property(e => e.AddressLine)
                .HasMaxLength(255)
                .HasColumnName("address_line");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.ContactName)
                .HasMaxLength(255)
                .HasColumnName("contact_name");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("contact_phone");
            entity.Property(e => e.District)
                .HasMaxLength(100)
                .HasColumnName("district");
            entity.Property(e => e.Latitude)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("longitude");
            entity.Property(e => e.Note)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("note");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");
            entity.Property(e => e.Ward)
                .HasMaxLength(100)
                .HasColumnName("ward");
        });

        modelBuilder.Entity<InspectionScore>(entity =>
        {
            entity.HasKey(e => new { e.InspectionId, e.ComponentId }).HasName("PK__inspecti__492FFAE6D8F64561");

            entity.ToTable("inspection_scores");

            entity.Property(e => e.InspectionId).HasColumnName("inspection_id");
            entity.Property(e => e.ComponentId).HasColumnName("component_id");
            entity.Property(e => e.Note)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("note");
            entity.Property(e => e.Score).HasColumnName("score");

            entity.HasOne(d => d.Component).WithMany(p => p.InspectionScores)
                .HasForeignKey(d => d.ComponentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__inspectio__compo__4AB81AF0");

            entity.HasOne(d => d.Inspection).WithMany(p => p.InspectionScores)
                .HasForeignKey(d => d.InspectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__inspectio__inspe__49C3F6B7");
        });

        modelBuilder.Entity<KycImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__kyc_imag__DC9AC955A5F63AC9");

            entity.ToTable("kyc_images");

            entity.Property(e => e.ImageId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("image_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ImageType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("image_type");

            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("image_url");

            entity.Property(e => e.ImageData)
                .HasColumnType("varbinary(max)")
                .HasColumnName("image_data");

            entity.Property(e => e.ContentType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("content_type");

            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");

            entity.Property(e => e.KycId).HasColumnName("kyc_id");

            entity.HasOne(d => d.Kyc).WithMany(p => p.KycImages)
                .HasForeignKey(d => d.KycId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__kyc_image__kyc_i__3C69FB99");
        });

        modelBuilder.Entity<KycProfile>(entity =>
        {
            entity.HasKey(e => e.KycId).HasName("PK__kyc_prof__071B54395808B212");

            entity.ToTable("kyc_profiles");

            entity.Property(e => e.KycId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("kyc_id");
            entity.Property(e => e.DateOfBirth)
                .HasMaxLength(50)
                .HasColumnName("dateOfBirth");
            entity.Property(e => e.ExpiryDate)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("expiryDate");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(50)
                .HasColumnName("gender");
            entity.Property(e => e.IdNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("id_number");
            entity.Property(e => e.Nationality)
                .HasMaxLength(100)
                .HasColumnName("nationality");
            entity.Property(e => e.PlaceOfOrigin)
                .HasMaxLength(255)
                .HasColumnName("placeOfOrigin");
            entity.Property(e => e.PlaceOfResidence)
                .HasMaxLength(255)
                .HasColumnName("placeOfResidence");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VerifiedAt)
                .HasColumnType("datetime")
                .HasColumnName("verified_at");

            entity.HasOne(d => d.User).WithMany(p => p.KycProfiles)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__kyc_profi__user___2F10007B");
        });

        modelBuilder.Entity<Listing>(entity =>
        {
            entity.HasKey(e => e.ListingId).HasName("PK__listings__89D81774C292269D");

            entity.ToTable("listings");

            entity.Property(e => e.ListingId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("listing_id");
            entity.Property(e => e.BikeTypeId).HasColumnName("bike_type_id");
            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("description");
            entity.Property(e => e.FrameNumber)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("frame_number");
            entity.Property(e => e.ListingUrl)
                .HasMaxLength(500)
                .HasColumnName("listing_url");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UsageDuration).HasColumnName("usage_duration");
            entity.Property(e => e.Views).HasColumnName("views");

            entity.HasOne(d => d.BikeType).WithMany(p => p.Listings)
                .HasForeignKey(d => d.BikeTypeId)
                .HasConstraintName("FK__listings__bike_t__34C8D9D1");

            entity.HasOne(d => d.Brand).WithMany(p => p.Listings)
                .HasForeignKey(d => d.BrandId)
                .HasConstraintName("FK__listings__brand___33D4B598");

            entity.HasOne(d => d.Seller).WithMany(p => p.Listings)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("FK__listings__seller__32E0915F");
        });

        modelBuilder.Entity<ListingImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__listing___DC9AC9559AAA75AB");

            entity.ToTable("listing_images");

            entity.Property(e => e.ImageId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("image_id");
            entity.Property(e => e.ImageType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("image_type");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("image_url");
            entity.Property(e => e.ListingId).HasColumnName("listing_id");

            entity.HasOne(d => d.Listing).WithMany(p => p.ListingImages)
                .HasForeignKey(d => d.ListingId)
                .HasConstraintName("FK__listing_i__listi__38996AB5");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__orders__465962295039E90B");

            entity.ToTable("orders");

            entity.Property(e => e.OrderId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("order_id");
            entity.Property(e => e.BuyerId).HasColumnName("buyer_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DeliveryDetail)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("delivery_detail");
            entity.Property(e => e.DeliveryDistrict)
                .HasMaxLength(100)
                .HasColumnName("delivery_district");
            entity.Property(e => e.DeliveryFullName)
                .HasMaxLength(255)
                .HasColumnName("delivery_full_name");
            entity.Property(e => e.DeliveryPhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("delivery_phone_number");
            entity.Property(e => e.DeliveryProvince)
                .HasMaxLength(100)
                .HasColumnName("delivery_province");
            entity.Property(e => e.DeliveryWard)
                .HasMaxLength(100)
                .HasColumnName("delivery_ward");
            entity.Property(e => e.ListingId).HasColumnName("listing_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_amount");

            entity.HasOne(d => d.Buyer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.BuyerId)
                .HasConstraintName("FK__orders__buyer_id__52593CB8");

            entity.HasOne(d => d.Listing).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ListingId)
                .HasConstraintName("FK__orders__listing___534D60F1");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__payments__ED1FC9EA7142CD22");

            entity.ToTable("payments");

            entity.Property(e => e.PaymentId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PaymentType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("payment_type");
            entity.Property(e => e.ProviderTxnNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("provider_txn_no");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TxnRef)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("txn_ref");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_payments_orders_order_id");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__payments__user_i__4E88ABD4");
        });

        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            entity.HasKey(e => e.ReturnRequestId).HasName("PK__return_r__C456CAE173276121");

            entity.ToTable("return_requests");

            entity.Property(e => e.ReturnRequestId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("return_request_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Detail)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("detail");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Reason)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("reason");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Order).WithMany(p => p.ReturnRequests)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__return_re__order__5BE2A6F2");
        });

        modelBuilder.Entity<ReturnRequestImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__return_r__DC9AC955176E1DDD");

            entity.ToTable("return_request_images");

            entity.Property(e => e.ImageId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("image_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("image_url");
            entity.Property(e => e.UploaderRole)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("('Buyer')")
                .HasColumnName("uploader_role");
            entity.Property(e => e.ReturnRequestId).HasColumnName("return_request_id");

            entity.HasOne(d => d.ReturnRequest).WithMany(p => p.ReturnRequestImages)
                .HasForeignKey(d => d.ReturnRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__return_re__retur__5FB337D6");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__reviews__60883D90F0CDD3EB");

            entity.ToTable("reviews");

            entity.Property(e => e.ReviewId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("review_id");
            entity.Property(e => e.BuyerId).HasColumnName("buyer_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("description");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");

            entity.HasOne(d => d.Buyer).WithMany(p => p.ReviewBuyers)
                .HasForeignKey(d => d.BuyerId)
                .HasConstraintName("FK__reviews__buyer_i__6477ECF3");

            entity.HasOne(d => d.Order).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__reviews__order_i__6383C8BA");

            entity.HasOne(d => d.Seller).WithMany(p => p.ReviewSellers)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("FK__reviews__seller___656C112C");
        });

        modelBuilder.Entity<SellerProfile>(entity =>
        {
            entity.HasKey(e => e.SellerId);

            entity.ToTable("seller_profiles");

            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.SellerRating)
                .HasColumnType("float")
                .HasDefaultValue(0d)
                .HasColumnName("seller_rating");
            entity.Property(e => e.TotalReviews)
                .HasDefaultValue(0)
                .HasColumnName("total_reviews");
            entity.Property(e => e.LastReviewDate)
                .HasColumnType("datetime")
                .HasColumnName("last_review_date");

            entity.HasOne(d => d.Seller).WithOne(p => p.SellerProfile)
                .HasForeignKey<SellerProfile>(d => d.SellerId);
        });

        modelBuilder.Entity<Type>(entity =>
        {
            entity.HasKey(e => e.BikeTypeId).HasName("PK__types__EA3CEBA282E3718C");

            entity.ToTable("types");

            entity.HasIndex(e => e.Name, "UQ__types__72E12F1BABFEF908").IsUnique();

            entity.Property(e => e.BikeTypeId).HasColumnName("bike_type_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<UserWallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("PK__user_wal__C5B49B70D45C7A91");

            entity.ToTable("user_wallets");

            entity.HasIndex(e => e.UserId, "UQ__user_wal__B9BE370E6A9166A1").IsUnique();

            entity.Property(e => e.WalletId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("wallet_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Balance)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("balance");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User).WithOne(p => p.Wallet)
                .HasForeignKey<UserWallet>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__user_wal__user___7B5B524B");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.WalletTransactionId).HasName("PK__wallet_t__C5A1E4D88C0AF0F1");

            entity.ToTable("wallet_transactions");

            entity.HasIndex(e => e.WalletId, "IX__wallet_t__wallet_id");

            entity.Property(e => e.WalletTransactionId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("wallet_transaction_id");
            entity.Property(e => e.WalletId).HasColumnName("wallet_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.Direction)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("direction");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Note)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("note");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__wallet_t__wallet__00400204");
        });

        modelBuilder.Entity<WithdrawalRequest>(entity =>
        {
            entity.HasKey(e => e.WithdrawalRequestId).HasName("PK_withdrawal_requests");

            entity.ToTable("withdrawal_requests");

            entity.HasIndex(e => e.UserId, "IX_withdrawal_requests_user_id");

            entity.Property(e => e.WithdrawalRequestId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("withdrawal_request_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.BankName)
                .HasMaxLength(255)
                .HasColumnName("bank_name");
            entity.Property(e => e.AccountNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("account_number");
            entity.Property(e => e.AccountName)
                .HasMaxLength(255)
                .HasColumnName("account_name");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending")
                .HasColumnName("status");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ConfirmedAt)
                .HasColumnType("datetime")
                .HasColumnName("confirmed_at");

            entity.HasOne(d => d.User).WithMany(p => p.WithdrawalRequests)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_withdrawal_requests_users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__users__B9BE370F8510F526");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "UQ__users__AB6E616431DF5BB6").IsUnique();

            entity.Property(e => e.UserId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("user_id");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone_number");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Active")
                .HasColumnName("status");
        });

        modelBuilder.Entity<FrameSize>(entity =>
        {
            entity.HasKey(e => e.FrameSizeId);

            entity.ToTable("frame_sizes");

            entity.Property(e => e.FrameSizeId)
                .ValueGeneratedOnAdd()
                .HasColumnName("frame_size_id");
            entity.Property(e => e.SizeValue)
                .HasMaxLength(50)
                .HasColumnName("size_value");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId);

            entity.ToTable("messages");

            entity.Property(e => e.MessageId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("message_id");
            entity.Property(e => e.ListingId).HasColumnName("listing_id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.ReceiverId).HasColumnName("receiver_id");
            entity.Property(e => e.Content)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("content");
            entity.Property(e => e.SentAt)
                .HasColumnType("datetime")
                .HasColumnName("sent_at");
            entity.Property(e => e.IsRead).HasColumnName("is_read");

            entity.HasOne(d => d.Listing).WithMany()
                .HasForeignKey(d => d.ListingId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Sender).WithMany()
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Receiver).WithMany()
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
