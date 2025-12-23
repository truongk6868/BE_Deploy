using System;
using System.Collections.Generic;
using CondotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CondotelManagement.Data;

public partial class CondotelDbVer1Context : DbContext
{
    public CondotelDbVer1Context()
    {
    }

    public CondotelDbVer1Context(DbContextOptions<CondotelDbVer1Context> options)
        : base(options)
    {
    }
    public virtual DbSet<BlogRequest> BlogRequests { get; set; } = null!;
    public virtual DbSet<AdminReport> AdminReports { get; set; }

    public virtual DbSet<Amenity> Amenities { get; set; }

    public virtual DbSet<BlogCategory> BlogCategories { get; set; }

    public virtual DbSet<BlogPost> BlogPosts { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingDetail> BookingDetails { get; set; }

    public virtual DbSet<Condotel> Condotels { get; set; }

    public virtual DbSet<CondotelAmenity> CondotelAmenities { get; set; }

    public virtual DbSet<CondotelDetail> CondotelDetails { get; set; }

    public virtual DbSet<CondotelImage> CondotelImages { get; set; }

    public virtual DbSet<CondotelPrice> CondotelPrices { get; set; }

    public virtual DbSet<CondotelUtility> CondotelUtilities { get; set; }

    public virtual DbSet<Models.Host> Hosts { get; set; }

    public virtual DbSet<HostPackage> HostPackages { get; set; }

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<Package> Packages { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<Resort> Resorts { get; set; }

    public virtual DbSet<ResortUtility> ResortUtilities { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<RefundRequest> RefundRequests { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<ServicePackage> ServicePackages { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Utility> Utilities { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }
    public DbSet<ChatConversation> ChatConversations { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
	public virtual DbSet<HostVoucherSetting> HostVoucherSettings { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminReport>(entity =>
        {
            entity.HasKey(e => e.ReportId);

            entity.ToTable("AdminReport");

            entity.Property(e => e.ReportId).HasColumnName("ReportID");
            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.FileUrl)
                .HasMaxLength(255)
                .HasColumnName("FileURL");
            entity.Property(e => e.GeneratedDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ReportType).HasMaxLength(100);

            entity.HasOne(d => d.Admin).WithMany(p => p.AdminReports)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdminReport_User");
        });

        modelBuilder.Entity<Amenity>(entity =>
        {
            entity.Property(e => e.AmenityId).HasColumnName("AmenityID");
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.HostID)
                .HasColumnName("HostID")
                .IsRequired();
            entity.HasOne(e => e.Host)
                .WithMany(h => h.Amenities)
                .HasForeignKey(e => e.HostID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BlogCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.HasIndex(e => e.Slug, "IX_BlogCategories_Slug").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Slug)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.HasKey(e => e.PostId);

            entity.HasIndex(e => e.Slug, "IX_BlogPosts_Slug").IsUnique();

            entity.Property(e => e.PostId).HasColumnName("PostID");
            entity.Property(e => e.AuthorUserId).HasColumnName("AuthorUserID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FeaturedImageUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PublishedAt).HasColumnType("datetime");
            entity.Property(e => e.Slug)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Draft");
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.AuthorUser).WithMany(p => p.BlogPosts)
                .HasForeignKey(d => d.AuthorUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BlogPosts_User");

            entity.HasOne(d => d.Category).WithMany(p => p.BlogPosts)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BlogPosts_BlogCategories");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("Booking");

            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.CondotelId).HasColumnName("CondotelID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.VoucherId).HasColumnName("VoucherID");
            entity.Property(e => e.IsPaidToHost)
                .HasDefaultValue(false);
            entity.Property(e => e.PaidToHostAt).HasColumnType("datetime");
            entity.Property(e => e.PayoutRejectedAt).HasColumnType("datetime");
            entity.Property(e => e.PayoutRejectionReason).HasMaxLength(500);

            entity.HasOne(d => d.Condotel).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CondotelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Booking_Condotel");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Booking_User");

            entity.HasOne(d => d.Promotion).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK_Booking_Promotion");

            entity.HasOne(d => d.Voucher).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Booking_Voucher");
            entity.Property(e => e.CheckInToken)
.HasMaxLength(20)
.IsUnicode(false);

            entity.Property(e => e.CheckInTokenGeneratedAt)
                .HasColumnType("datetime");

            entity.Property(e => e.CheckInTokenUsedAt)
                .HasColumnType("datetime");
            entity.Property(e => e.GuestFullName)
        .HasMaxLength(150);

            entity.Property(e => e.GuestPhone)
                .HasMaxLength(20);
            entity.Property(e => e.GuestIdNumber)
        .HasMaxLength(50);
        });



            modelBuilder.Entity<BookingDetail>(entity =>
            {
                entity.ToTable("BookingDetail");

                entity.Property(e => e.BookingDetailId).HasColumnName("BookingDetailID");
                entity.Property(e => e.BookingId).HasColumnName("BookingID");
                entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
                entity.Property(e => e.Quantity).HasDefaultValue(1);
                entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

                entity.HasOne(d => d.Booking).WithMany(p => p.BookingDetails)
                    .HasForeignKey(d => d.BookingId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BookingDetail_Booking");

                entity.HasOne(d => d.Service).WithMany(p => p.BookingDetails)
                    .HasForeignKey(d => d.ServiceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BookingDetail_Service");
            });

            modelBuilder.Entity<Condotel>(entity =>
            {
                entity.ToTable("Condotel");

                entity.Property(e => e.CondotelId).HasColumnName("CondotelID");
                entity.Property(e => e.Bathrooms).HasDefaultValue(1);
                entity.Property(e => e.Beds).HasDefaultValue(1);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.HostId).HasColumnName("HostID");
                entity.Property(e => e.Name).HasMaxLength(150);
                entity.Property(e => e.PricePerNight).HasColumnType("decimal(12, 2)");
                entity.Property(e => e.ResortId).HasColumnName("ResortID");
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.HasOne(d => d.Host).WithMany(p => p.Condotels)
                    .HasForeignKey(d => d.HostId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Condotel_Host");

                entity.HasOne(d => d.Resort).WithMany(p => p.Condotels)
                    .HasForeignKey(d => d.ResortId)
                    .HasConstraintName("FK_Condotel_Resort");
            });

            modelBuilder.Entity<CondotelAmenity>(entity =>
            {
                entity.HasKey(e => new { e.CondotelId, e.AmenityId });

                entity.Property(e => e.CondotelId).HasColumnName("CondotelID");
                entity.Property(e => e.AmenityId).HasColumnName("AmenityID");
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.HasOne(d => d.Amenity).WithMany(p => p.CondotelAmenities)
                    .HasForeignKey(d => d.AmenityId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CondotelAmenities_Amenity");

                entity.HasOne(d => d.Condotel).WithMany(p => p.CondotelAmenities)
                    .HasForeignKey(d => d.CondotelId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CondotelAmenities_Condotel");
            });

            modelBuilder.Entity<CondotelDetail>(entity =>
            {
                entity.HasKey(e => e.DetailId);

                entity.Property(e => e.DetailId).HasColumnName("DetailID");
                entity.Property(e => e.BuildingName).HasMaxLength(150);
                entity.Property(e => e.CondotelId).HasColumnName("CondotelID");
                entity.Property(e => e.HygieneStandards).HasMaxLength(500);
                entity.Property(e => e.RoomNumber).HasMaxLength(50);
                entity.Property(e => e.SafetyFeatures).HasMaxLength(500);
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.HasOne(d => d.Condotel).WithMany(p => p.CondotelDetails)
                    .HasForeignKey(d => d.CondotelId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CondotelDetails_Condotel");
            });

            modelBuilder.Entity<CondotelImage>(entity =>
            {
                entity.HasKey(e => e.ImageId);

                entity.ToTable("CondotelImage");

                entity.Property(e => e.ImageId).HasColumnName("ImageID");
                entity.Property(e => e.Caption).HasMaxLength(255);
                entity.Property(e => e.CondotelId).HasColumnName("CondotelID");
                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(255)
                    .HasColumnName("ImageURL");

                entity.HasOne(d => d.Condotel).WithMany(p => p.CondotelImages)
                    .HasForeignKey(d => d.CondotelId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CondotelImage_Condotel");
            });

            modelBuilder.Entity<CondotelPrice>(entity =>
            {
                entity.HasKey(e => e.PriceId);

                entity.ToTable("CondotelPrice");

                entity.Property(e => e.PriceId).HasColumnName("PriceID");
                entity.Property(e => e.BasePrice).HasColumnType("decimal(12, 2)");
                entity.Property(e => e.CondotelId).HasColumnName("CondotelID");
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.PriceType)
                    .HasMaxLength(50)
                    .HasDefaultValue("Normal");
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.HasOne(d => d.Condotel).WithMany(p => p.CondotelPrices)
                    .HasForeignKey(d => d.CondotelId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CondotelPrice_Condotel");
            });

            modelBuilder.Entity<CondotelUtility>(entity =>
            {
                entity.HasKey(e => new { e.CondotelId, e.UtilityId });

                entity.Property(e => e.CondotelId).HasColumnName("CondotelID");
                entity.Property(e => e.UtilityId).HasColumnName("UtilityID");
                entity.Property(e => e.DateAdded).HasDefaultValueSql("(getdate())");
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.HasOne(d => d.Condotel).WithMany(p => p.CondotelUtilities)
                    .HasForeignKey(d => d.CondotelId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CondotelUtilities_Condotel");

                entity.HasOne(d => d.Utility).WithMany(p => p.CondotelUtilities)
                    .HasForeignKey(d => d.UtilityId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CondotelUtilities_Utilities");
            });

            modelBuilder.Entity<Models.Host>(entity =>
            {
                entity.ToTable("Host");

                entity.Property(e => e.HostId).HasColumnName("HostID");
                entity.Property(e => e.Address).HasMaxLength(255);
                entity.Property(e => e.CompanyName).HasMaxLength(200);
                entity.Property(e => e.PhoneContact).HasMaxLength(20);
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");
                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.User).WithMany(p => p.Hosts)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Host_User");
            });

            modelBuilder.Entity<HostPackage>(entity =>
            {
                entity.HasKey(e => new { e.HostId, e.PackageId });

                entity.ToTable("HostPackage");

                entity.Property(e => e.HostId).HasColumnName("HostID");
                entity.Property(e => e.PackageId).HasColumnName("PackageID");
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.HasOne(d => d.Host).WithMany(p => p.HostPackages)
                    .HasForeignKey(d => d.HostId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HostPackage_Host");

                entity.HasOne(d => d.Package).WithMany(p => p.HostPackages)
                    .HasForeignKey(d => d.PackageId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_HostPackage_Package");
            });
            modelBuilder.Entity<BlogRequest>(entity =>
            {
                entity.ToTable("BlogRequests");

                entity.HasKey(e => e.BlogRequestId);

                entity.Property(e => e.BlogRequestId)
                    .HasColumnName("BlogRequestID");

                entity.Property(e => e.HostId)
                    .HasColumnName("HostID");

                entity.Property(e => e.Title)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.Content)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("Pending");

                entity.Property(e => e.RequestDate)
                    .HasColumnType("datetime2(0)")
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.ProcessedDate)
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.ProcessedByUserId)
                    .HasColumnName("ProcessedByUserID");
                entity.Property(e => e.CategoryId)
            .HasColumnName("CategoryID");

                // ←← THÊM RELATIONSHIP VỚI BLOG CATEGORIES
                entity.HasOne(d => d.BlogCategory)
                    .WithMany(c => c.BlogRequests)  // nếu BlogCategory có ICollection<BlogRequest>
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Relationships
                entity.HasOne(d => d.Host)
                    .WithMany(p => p.BlogRequests)
                    .HasForeignKey(d => d.HostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.ProcessedByUser)
                    .WithMany()
                    .HasForeignKey(d => d.ProcessedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Location");

                entity.Property(e => e.LocationId).HasColumnName("LocationID");
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.Name).HasMaxLength(150);
                entity.Property(e => e.ImageUrl).HasMaxLength(500);
            });

            modelBuilder.Entity<Package>(entity =>
            {
                entity.ToTable("Package");

                entity.Property(e => e.PackageId).HasColumnName("PackageID");
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.Duration).HasMaxLength(50);
                entity.Property(e => e.Name).HasMaxLength(150);
                entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");
            });

            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.ToTable("Promotion");

                entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
                entity.Property(e => e.CondotelId).HasColumnName("CondotelID");
                entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(5, 2)");
                entity.Property(e => e.Name).HasMaxLength(150);
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");
                entity.Property(e => e.TargetAudience).HasMaxLength(100);

                entity.HasOne(d => d.Condotel).WithMany(p => p.Promotions)
                    .HasForeignKey(d => d.CondotelId)
                    .HasConstraintName("FK_Promotion_Condotel");
            });

            modelBuilder.Entity<Resort>(entity =>
            {
                entity.ToTable("Resort");

                entity.Property(e => e.ResortId).HasColumnName("ResortID");
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.LocationId).HasColumnName("LocationID");
                entity.Property(e => e.Name).HasMaxLength(150);
                entity.Property(e => e.Address).HasMaxLength(500);

                entity.HasOne(d => d.Location).WithMany(p => p.Resorts)
                    .HasForeignKey(d => d.LocationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Resort_Location");
            });

            modelBuilder.Entity<ResortUtility>(entity =>
            {
                entity.HasKey(e => new { e.ResortId, e.UtilityId });

                entity.Property(e => e.ResortId).HasColumnName("ResortID");
                entity.Property(e => e.UtilityId).HasColumnName("UtilityID");
                entity.Property(e => e.Cost).HasColumnType("decimal(12, 2)");
                entity.Property(e => e.DescriptionDetail).HasMaxLength(255);
                entity.Property(e => e.OperatingHours).HasMaxLength(50);
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.HasOne(d => d.Resort).WithMany(p => p.ResortUtilities)
                    .HasForeignKey(d => d.ResortId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ResortUtilities_Resort");

                entity.HasOne(d => d.Utility).WithMany(p => p.ResortUtilities)
                    .HasForeignKey(d => d.UtilityId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ResortUtilities_Utility");
            });

            modelBuilder.Entity<Review>(entity =>
            {
                entity.ToTable("Review");

                entity.Property(e => e.ReviewId).HasColumnName("ReviewID");
                entity.Property(e => e.BookingId).HasColumnName("BookingID");
                entity.Property(e => e.Comment).HasMaxLength(500);
                entity.Property(e => e.CondotelId).HasColumnName("CondotelID");
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())");
                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.Booking).WithMany(p => p.Reviews)
                    .HasForeignKey(d => d.BookingId)
                    .HasConstraintName("FK_Review_Booking");

                entity.HasOne(d => d.Condotel).WithMany(p => p.Reviews)
                    .HasForeignKey(d => d.CondotelId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Review_Condotel");

                entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Review_User");

                entity.Property(r => r.Reply)
                    .HasMaxLength(1000)
                    .IsRequired(false);

                entity.Property(r => r.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("Visible")
                    .IsRequired();
            });

            modelBuilder.Entity<RefundRequest>(entity =>
            {
                entity.ToTable("RefundRequests");

                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.BookingId).HasColumnName("BookingId");
                entity.Property(e => e.CustomerId).HasColumnName("CustomerId");
                entity.Property(e => e.CustomerName)
                    .HasMaxLength(255)
                    .IsRequired();
                entity.Property(e => e.CustomerEmail).HasMaxLength(255);

                entity.Property(e => e.RefundAmount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("Pending")
                    .IsRequired();

                entity.Property(e => e.BankCode).HasMaxLength(50);
                entity.Property(e => e.AccountNumber).HasMaxLength(50);
                entity.Property(e => e.AccountHolder).HasMaxLength(255);

                entity.Property(e => e.Reason).HasMaxLength(500);
                entity.Property(e => e.CancelDate).HasColumnType("datetime");
                entity.Property(e => e.ProcessedBy).HasColumnName("ProcessedBy");
                entity.Property(e => e.ProcessedAt).HasColumnType("datetime");
                entity.Property(e => e.TransactionId).HasMaxLength(100);
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("GETDATE()")
                    .IsRequired();
                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Booking)
                    .WithMany()
                    .HasForeignKey(d => d.BookingId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_RefundRequests_Bookings");

                entity.HasOne(d => d.Customer)
                    .WithMany()
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_RefundRequests_Users_Customer");

                entity.HasOne(d => d.ProcessedByUser)
                    .WithMany()
                    .HasForeignKey(d => d.ProcessedBy)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_RefundRequests_Users_Admin");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Role");

                entity.HasIndex(e => e.RoleName, "UQ_Role_RoleName").IsUnique();

                entity.Property(e => e.RoleId).HasColumnName("RoleID");
                entity.Property(e => e.RoleName).HasMaxLength(50);
            });

            modelBuilder.Entity<ServicePackage>(entity =>
            {
                entity.HasKey(e => e.ServiceId);

                entity.ToTable("ServicePackage");

                entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");
                entity.Property(e => e.HostID)
                    .HasColumnName("HostID")
                    .IsRequired();
                entity.HasOne(e => e.Host)
                    .WithMany(h => h.ServicePackages)
                    .HasForeignKey(e => e.HostID)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ChatConversation>()
            .ToTable("ChatConversation");
            modelBuilder.Entity<ChatMessage>()
            .ToTable("ChatMessage");
            modelBuilder.Entity<ChatConversation>(b =>
            {
                b.HasKey(c => c.ConversationId);
                b.Property(c => c.Name).HasMaxLength(255);
                b.Property(c => c.ConversationType).HasMaxLength(20).IsRequired();
                b.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                b.HasOne(c => c.UserA)
                 .WithMany()
                 .HasForeignKey(c => c.UserAId)
                 .OnDelete(DeleteBehavior.Restrict);


                b.HasOne(c => c.UserB)
                 .WithMany()
                 .HasForeignKey(c => c.UserBId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ChatMessage>(b =>
            {
                b.HasKey(m => m.MessageId);
                b.Property(m => m.SentAt).HasDefaultValueSql("GETUTCDATE()");
                b.Property(m => m.Content).HasColumnType("nvarchar(max)");
                b.HasOne(m => m.Conversation)
                 .WithMany(c => c.Messages)
                 .HasForeignKey(m => m.ConversationId)
                 .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(m => m.Sender)
       .WithMany()
       .HasForeignKey(m => m.SenderId)
       .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");

                entity.HasIndex(e => e.Email, "UQ_User_Email").IsUnique();

                entity.Property(e => e.UserId).HasColumnName("UserID");
                entity.Property(e => e.Address).HasMaxLength(255);
                entity.Property(e => e.CreatedAt)
                    .HasPrecision(0)
                    .HasDefaultValueSql("(sysdatetime())");
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.FullName).HasMaxLength(150);
                entity.Property(e => e.Gender).HasMaxLength(10);
                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(500)
                    .HasColumnName("ImageURL");
                entity.Property(e => e.PasswordHash).HasMaxLength(100);
                entity.Property(e => e.PasswordResetToken).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.RoleId).HasColumnName("RoleID");
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");

                entity.HasOne(d => d.Role).WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_Role");
            });

            modelBuilder.Entity<Utility>(entity =>
            {
                entity.Property(e => e.UtilityId).HasColumnName("UtilityID");
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.Name).HasMaxLength(100);
            });

            modelBuilder.Entity<Voucher>(entity =>
            {
                entity.HasKey(e => e.VoucherId).HasName("PK__Voucher__3AEE79C1FC1B0A7C");

                entity.ToTable("Voucher");

                entity.HasIndex(e => e.Code, "UQ__Voucher__A25C5AA71A906DB6").IsUnique();

                entity.Property(e => e.VoucherId).HasColumnName("VoucherID");
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.CondotelId).HasColumnName("CondotelID");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(5, 2)");
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");
                entity.Property(e => e.UsedCount).HasDefaultValue(0);

                entity.HasOne(d => d.Condotel).WithMany(p => p.Vouchers)
                    .HasForeignKey(d => d.CondotelId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Voucher_Condotel");

                entity.Property(e => e.UserId).HasColumnName("UserID");
                entity.HasOne(d => d.User)
                    .WithMany(p => p.Vouchers)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Voucher_User");
            });

            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.ToTable("Wallet");

                entity.Property(e => e.WalletId).HasColumnName("WalletID");
                entity.Property(e => e.AccountHolderName).HasMaxLength(150);
                entity.Property(e => e.AccountNumber).HasMaxLength(50);
                entity.Property(e => e.BankName).HasMaxLength(100);
                entity.Property(e => e.HostId).HasColumnName("HostID");
                entity.Property(e => e.UserId).HasColumnName("UserID");
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active");
                entity.Property(e => e.IsDefault)
                    .HasDefaultValue(true);

                entity.HasOne(d => d.User).WithMany(p => p.Wallets)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_Wallet_User");

                entity.HasOne(w => w.Host)
                    .WithMany(h => h.Wallets) // One-to-Many: một Host có thể có nhiều Wallet
                    .HasForeignKey(w => w.HostId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<HostVoucherSetting>(entity =>
            {
                entity.ToTable("HostVoucherSetting");

                entity.HasKey(e => e.SettingID);

                entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(5, 2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10, 2)");

                entity.HasOne(e => e.Host)
                    .WithOne(h => h.VoucherSetting)
                    .HasForeignKey<HostVoucherSetting>(e => e.HostID)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_HostVoucherSetting_Host");
            });

            OnModelCreatingPartial(modelBuilder); }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
