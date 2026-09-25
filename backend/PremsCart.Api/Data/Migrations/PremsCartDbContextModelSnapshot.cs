using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PremsCart.Api.Models;
namespace PremsCart.Api.Data.Migrations;
[DbContext(typeof(PremsCartDbContext))]
public sealed class PremsCartDbContextModelSnapshot : ModelSnapshot {
    protected override void BuildModel(ModelBuilder modelBuilder) {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0").HasAnnotation("Relational:MaxIdentifierLength", 63).HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        modelBuilder.Entity<Role>(b => {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<string>("RoleName").HasColumnType("text").IsRequired(true);
            b.HasKey("Id");
            b.ToTable("Roles");
        });
        modelBuilder.Entity<University>(b => {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<string>("UniversityName").HasColumnType("text").IsRequired(true);
            b.Property<string>("EmailDomain").HasColumnType("text").IsRequired(true);
            b.Property<string>("Address").HasColumnType("text").IsRequired(false);
            b.HasKey("Id");
            b.ToTable("Universities");
        });
        modelBuilder.Entity<Department>(b => {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<string>("DepartmentName").HasColumnType("text").IsRequired(true);
            b.Property<string>("Code").HasColumnType("text").IsRequired(true);
            b.HasKey("Id");
            b.ToTable("Departments");
        });
        modelBuilder.Entity<Category>(b => {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<string>("CategoryName").HasColumnType("text").IsRequired(true);
            b.Property<string>("Description").HasColumnType("text").IsRequired(false);
            b.Property<string>("Image").HasColumnType("text").IsRequired(false);
            b.HasKey("Id");
            b.ToTable("Categories");
        });
        modelBuilder.Entity<PickupLocation>(b => {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<string>("LocationName").HasColumnType("text").IsRequired(true);
            b.Property<string>("Description").HasColumnType("text").IsRequired(false);
            b.HasKey("Id");
            b.ToTable("PickupLocations");
        });
        modelBuilder.Entity<User>(b => {
            b.Property<bool>("NotifyMessages").HasColumnType("boolean");
            b.Property<bool>("NotifyOffers").HasColumnType("boolean");
            b.Property<bool>("NotifyOrders").HasColumnType("boolean");
            b.Property<bool>("NotifyRentals").HasColumnType("boolean");
            b.Property<bool>("NotifySavedListings").HasColumnType("boolean");

            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<string>("FirstName").HasColumnType("text").IsRequired(true);
            b.Property<string>("LastName").HasColumnType("text").IsRequired(true);
            b.Property<string>("Email").HasColumnType("text").IsRequired(true);
            b.Property<string>("Status").HasColumnType("text").IsRequired(true);
            b.Property<string>("SuspensionReason").HasColumnType("text").IsRequired(false);
            b.Property<int>("TokenVersion").HasColumnType("integer");
            b.Property<string>("PasswordHash").HasColumnType("text").IsRequired(true);
            b.Property<string>("UniversityEmail").HasColumnType("text").IsRequired(true);
            b.Property<int?>("Batch").HasColumnType("integer");
            b.Property<string>("Department").HasColumnType("text").IsRequired(false);
            b.Property<string>("Program").HasColumnType("text").IsRequired(false);
            b.Property<string>("StudentIdLastThreeDigits").HasColumnType("text").IsRequired(false);
            b.Property<string>("ProfileImage").HasColumnType("text").IsRequired(false);
            b.Property<bool>("IsVerified").HasColumnType("boolean");
            b.Property<int>("RoleId").HasColumnType("integer");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.Property<DateTime>("UpdatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.ToTable("Users");
        });
        modelBuilder.Entity<Product>(b => {
            b.Property<bool>("IsHidden").HasColumnType("boolean");
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("SellerId").HasColumnType("integer");
            b.Property<int>("CategoryId").HasColumnType("integer");
            b.Property<string>("Title").HasColumnType("text").IsRequired(true);
            b.Property<string>("Description").HasColumnType("text").IsRequired(true);
            b.Property<bool>("AllowRent").HasColumnType("boolean");
            b.Property<decimal?>("RentalPrice").HasPrecision(12, 2).HasColumnType("numeric(12,2)");
            b.Property<decimal?>("Price").HasColumnType("numeric(12,2)");
            b.Property<string>("TransactionType").HasColumnType("text").IsRequired(true);
            b.Property<string>("Condition").HasColumnType("text").IsRequired(true);
            b.Property<string>("Status").HasColumnType("text").IsRequired(true);
            b.Property<string>("Location").HasColumnType("text").IsRequired(false);
            b.Property<bool>("IsNegotiable").HasColumnType("boolean");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.ToTable("Products");
        });
        modelBuilder.Entity<ProductImage>(b => {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("ProductId").HasColumnType("integer");
            b.Property<string>("ImageUrl").HasColumnType("text").IsRequired(true);
            b.Property<bool>("IsPrimary").HasColumnType("boolean");
            b.HasKey("Id");
            b.ToTable("ProductImages");
        });
        modelBuilder.Entity<WishlistItem>(b => {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("UserId").HasColumnType("integer");
            b.Property<int>("ProductId").HasColumnType("integer");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.ToTable("Wishlist");
        });
        modelBuilder.Entity<WantedPost>(b => {
            b.Property<bool>("IsHidden").HasColumnType("boolean");
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("UserId").HasColumnType("integer");
            b.Property<string>("Title").HasColumnType("text").IsRequired(true);
            b.Property<string>("Description").HasColumnType("text").IsRequired(true);
            b.Property<decimal?>("Budget").HasColumnType("numeric(12,2)");
            b.Property<int?>("CategoryId").HasColumnType("integer");
            b.Property<string>("Status").HasColumnType("text").IsRequired(true);
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.ToTable("WantedPosts");
        });
        modelBuilder.Entity<Conversation>(b => {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("ProductId").HasColumnType("integer");
            b.Property<int>("BuyerId").HasColumnType("integer");
            b.Property<int>("SellerId").HasColumnType("integer");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.ToTable("Conversations");
        });
        modelBuilder.Entity<Message>(b => {
            b.Property<bool>("IsHidden").HasColumnType("boolean");
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("ConversationId").HasColumnType("integer");
            b.Property<int>("SenderId").HasColumnType("integer");
            b.Property<string>("MessageText").HasColumnType("text").IsRequired(true);
            b.Property<bool>("IsRead").HasColumnType("boolean");
            b.Property<DateTime>("SentAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.ToTable("Messages");
        });
        modelBuilder.Entity<Offer>(b => {
            b.Property<int?>("LastProposerId").HasColumnType("integer");
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("ProductId").HasColumnType("integer");
            b.Property<int>("BuyerId").HasColumnType("integer");
            b.Property<int>("SellerId").HasColumnType("integer");
            b.Property<decimal>("OfferAmount").HasColumnType("numeric(12,2)");
            b.Property<string>("Status").HasColumnType("text").IsRequired(true);
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.ToTable("Offers");
        });
        modelBuilder.Entity<Order>(b => {
            b.Property<DateOnly?>("RentalStartDate").HasColumnType("date");
            b.Property<int?>("RentalDays").HasColumnType("integer");
            b.Property<DateTime?>("RentalStartedAt").HasColumnType("timestamp with time zone");
            b.Property<DateTime?>("RentalDueAt").HasColumnType("timestamp with time zone");
            b.Property<DateTime?>("ReturnedAt").HasColumnType("timestamp with time zone");

            b.Property<int?>("PickupProposerId").HasColumnType("integer");
            b.Property<string>("PickupStatus").HasColumnType("text").IsRequired(true);
            b.Property<DateTime?>("CompletedAt").HasColumnType("timestamp with time zone");
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("BuyerId").HasColumnType("integer");
            b.Property<int>("SellerId").HasColumnType("integer");
            b.Property<int>("ProductId").HasColumnType("integer");
            b.Property<decimal?>("FinalPrice").HasColumnType("numeric(12,2)");
            b.Property<int?>("PickupLocationId").HasColumnType("integer");
            b.Property<DateTime?>("PickupTime").HasColumnType("timestamp with time zone");
            b.Property<string>("Status").HasColumnType("text").IsRequired(true);
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.ToTable("Orders");
        });
        modelBuilder.Entity<Review>(b => {
            b.Property<bool>("IsHidden").HasColumnType("boolean");
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("ReviewerId").HasColumnType("integer");
            b.Property<int>("ReviewedUserId").HasColumnType("integer");
            b.Property<int>("OrderId").HasColumnType("integer");
            b.Property<int>("Rating").HasColumnType("integer");
            b.Property<string>("Comment").HasColumnType("text").IsRequired(false);
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.ToTable("Reviews");
        });
        modelBuilder.Entity<Store>(b => {
            b.Property<bool>("IsHidden").HasColumnType("boolean");
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("OwnerId").HasColumnType("integer");
            b.Property<string>("StoreName").HasColumnType("text").IsRequired(true);
            b.Property<string>("Description").HasColumnType("text").IsRequired(false);
            b.Property<string>("Logo").HasColumnType("text").IsRequired(false);
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.ToTable("Stores");
        });
        modelBuilder.Entity<StoreProduct>(b => {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("StoreId").HasColumnType("integer");
            b.Property<int>("ProductId").HasColumnType("integer");
            b.Property<int>("Quantity").HasColumnType("integer");
            b.HasKey("Id");
            b.ToTable("StoreProducts");
        });
        modelBuilder.Entity<Report>(b => {
            b.Property<string>("ResolutionAction").HasColumnType("text").IsRequired(false);
            b.Property<int?>("ReviewId").HasColumnType("integer");
            b.Property<int?>("MessageId").HasColumnType("integer");
            b.Property<string>("ResolutionNote").HasColumnType("text").IsRequired(false);
            b.Property<int?>("ModeratorId").HasColumnType("integer");
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("ReporterId").HasColumnType("integer");
            b.Property<int?>("ReportedProductId").HasColumnType("integer");
            b.Property<int?>("ReportedUserId").HasColumnType("integer");
            b.Property<string>("Reason").HasColumnType("text").IsRequired(true);
            b.Property<string>("Status").HasColumnType("text").IsRequired(true);
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.ToTable("Reports");
        });
        modelBuilder.Entity<Notification>(b => {
            b.Property<string>("Link").HasColumnType("text").IsRequired(true);
            b.Property<string>("Type").HasColumnType("text").IsRequired(true);
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("UserId").HasColumnType("integer");
            b.Property<string>("Title").HasColumnType("text").IsRequired(true);
            b.Property<string>("Message").HasColumnType("text").IsRequired(true);
            b.Property<bool>("IsRead").HasColumnType("boolean");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.ToTable("Notifications");
        });
        modelBuilder.Entity<EmailVerification>(b => {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("UserId").HasColumnType("integer");
            b.Property<string>("CodeHash").HasColumnType("text").IsRequired(true);
            b.Property<DateTime>("ExpiresAt").HasColumnType("timestamp with time zone");
            b.Property<int>("FailedAttempts").HasColumnType("integer");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.ToTable("EmailVerifications");
        });
        modelBuilder.Entity<OfferProposal>(b => {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("OfferId").HasColumnType("integer");
            b.Property<int>("AuthorId").HasColumnType("integer");
            b.Property<decimal>("Amount").HasColumnType("numeric(12,2)");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.ToTable("OfferProposals");
        });
        modelBuilder.Entity<PasswordReset>(b => {
            b.Property<int>("Id").HasColumnType("integer").ValueGeneratedOnAdd().HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            b.Property<int>("UserId").HasColumnType("integer");
            b.Property<string>("CodeHash").HasColumnType("text").IsRequired(true);
            b.Property<DateTime>("ExpiresAt").HasColumnType("timestamp with time zone");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.Property<int>("FailedAttempts").HasColumnType("integer");
            b.HasKey("Id");
            b.ToTable("PasswordResets");
        });

        modelBuilder.Entity<PasswordReset>().HasIndex(x => x.UserId).IsUnique();
        modelBuilder.Entity<OfferProposal>().HasOne(x => x.Offer).WithMany(x => x.Proposals).HasForeignKey(x => x.OfferId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OfferProposal>().Property(x => x.Amount).HasPrecision(12, 2);
        modelBuilder.Entity<Role>().HasIndex(x => x.RoleName).IsUnique();
        modelBuilder.Entity<University>().HasIndex(x => x.EmailDomain).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Category>().HasIndex(x => x.CategoryName).IsUnique();
        modelBuilder.Entity<PickupLocation>().HasIndex(x => x.LocationName).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.UniversityEmail).IsUnique();
        modelBuilder.Entity<User>().HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Product>().HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Product>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Product>().Property(x => x.RentalPrice).HasPrecision(12, 2);
        modelBuilder.Entity<Product>().Property(x => x.Price).HasPrecision(12, 2);
        modelBuilder.Entity<Product>().ToTable(t => t.HasCheckConstraint("CK_Products_Price", "\"Price\" IS NULL OR \"Price\" >= 0"));
        modelBuilder.Entity<ProductImage>().HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WishlistItem>().HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
        modelBuilder.Entity<WishlistItem>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WishlistItem>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WantedPost>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WantedPost>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<WantedPost>().Property(x => x.Budget).HasPrecision(12, 2);
        modelBuilder.Entity<Conversation>().HasIndex(x => new { x.ProductId, x.BuyerId, x.SellerId }).IsUnique();
        modelBuilder.Entity<Conversation>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Conversation>().HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Conversation>().HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Message>().HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Message>().HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Offer>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Offer>().HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Offer>().HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Offer>().Property(x => x.OfferAmount).HasPrecision(12, 2);
        modelBuilder.Entity<Order>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>().HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>().HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>().HasOne(x => x.PickupLocation).WithMany().HasForeignKey(x => x.PickupLocationId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Order>().Property(x => x.FinalPrice).HasPrecision(12, 2);
        modelBuilder.Entity<Review>().HasIndex(x => new { x.OrderId, x.ReviewerId }).IsUnique();
        modelBuilder.Entity<Review>().HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Review>().HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Review>().HasOne(x => x.ReviewedUser).WithMany().HasForeignKey(x => x.ReviewedUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Review>().ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "\"Rating\" BETWEEN 1 AND 5"));
        modelBuilder.Entity<Store>().HasIndex(x => x.OwnerId).IsUnique();
        modelBuilder.Entity<Store>().HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StoreProduct>().HasIndex(x => new { x.StoreId, x.ProductId }).IsUnique();
        modelBuilder.Entity<StoreProduct>().HasOne(x => x.Store).WithMany(x => x.StoreProducts).HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StoreProduct>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StoreProduct>().ToTable(t => t.HasCheckConstraint("CK_StoreProducts_Quantity", "\"Quantity\" >= 0"));
        modelBuilder.Entity<Report>().HasOne(x => x.Reporter).WithMany().HasForeignKey(x => x.ReporterId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Report>().HasOne(x => x.ReportedUser).WithMany().HasForeignKey(x => x.ReportedUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Report>().HasOne(x => x.ReportedProduct).WithMany().HasForeignKey(x => x.ReportedProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Notification>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmailVerification>().HasIndex(x => x.UserId).IsUnique();
        modelBuilder.Entity<EmailVerification>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, RoleName = "Student" }, new Role { Id = 2, RoleName = "Business Seller" },
            new Role { Id = 3, RoleName = "Moderator" }, new Role { Id = 4, RoleName = "Admin" });
        modelBuilder.Entity<University>().HasData(new University { Id = 1, UniversityName = "Premier University", EmailDomain = "bscse.puc.ac.bd", Address = "Chattogram" });
        modelBuilder.Entity<Department>().HasData(new Department { Id = 1, DepartmentName = "Computer Science and Engineering", Code = "CSE" });
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, CategoryName = "Books" }, new Category { Id = 2, CategoryName = "Electronics" },
            new Category { Id = 3, CategoryName = "Academic Materials" }, new Category { Id = 4, CategoryName = "Clothing" },
            new Category { Id = 5, CategoryName = "Food" }, new Category { Id = 6, CategoryName = "Accessories" });
        modelBuilder.Entity<PickupLocation>().HasData(
            new PickupLocation { Id = 2, LocationName = "Library" },
            new PickupLocation { Id = 3, LocationName = "Main gate" },
            new PickupLocation { Id = 4, LocationName = "Canteen" });

        modelBuilder.Entity<User>().HasIndex("RoleId");
        modelBuilder.Entity<Product>().HasIndex("SellerId");
        modelBuilder.Entity<Product>().HasIndex("CategoryId");
        modelBuilder.Entity<ProductImage>().HasIndex("ProductId");
        modelBuilder.Entity<WishlistItem>().HasIndex("ProductId");
        modelBuilder.Entity<WantedPost>().HasIndex("UserId");
        modelBuilder.Entity<WantedPost>().HasIndex("CategoryId");
        modelBuilder.Entity<Conversation>().HasIndex("BuyerId");
        modelBuilder.Entity<Conversation>().HasIndex("SellerId");
        modelBuilder.Entity<Message>().HasIndex("ConversationId");
        modelBuilder.Entity<Message>().HasIndex("SenderId");
        modelBuilder.Entity<Offer>().HasIndex("ProductId");
        modelBuilder.Entity<Offer>().HasIndex("BuyerId");
        modelBuilder.Entity<Offer>().HasIndex("SellerId");
        modelBuilder.Entity<Order>().HasIndex("BuyerId");
        modelBuilder.Entity<Order>().HasIndex("SellerId");
        modelBuilder.Entity<Order>().HasIndex("ProductId");
        modelBuilder.Entity<Order>().HasIndex("PickupLocationId");
        modelBuilder.Entity<Review>().HasIndex("ReviewerId");
        modelBuilder.Entity<Review>().HasIndex("ReviewedUserId");
        modelBuilder.Entity<StoreProduct>().HasIndex("ProductId");
        modelBuilder.Entity<Report>().HasIndex("ReporterId");
        modelBuilder.Entity<Report>().HasIndex("ReportedProductId");
        modelBuilder.Entity<Report>().HasIndex("ReportedUserId");
        modelBuilder.Entity<Notification>().HasIndex("UserId");
        modelBuilder.Entity<OfferProposal>().HasIndex("OfferId");
    }
}
