using Microsoft.EntityFrameworkCore;
using PremsCart.Api.Models;

namespace PremsCart.Api.Data;

public sealed class PremsCartDbContext(DbContextOptions<PremsCartDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<University> Universities => Set<University>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<PickupLocation> PickupLocations => Set<PickupLocation>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<WishlistItem> Wishlist => Set<WishlistItem>();
    public DbSet<WantedPost> WantedPosts => Set<WantedPost>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StoreProduct> StoreProducts => Set<StoreProduct>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();

    public DbSet<OfferProposal> OfferProposals => Set<OfferProposal>();
    public DbSet<PasswordReset> PasswordResets => Set<PasswordReset>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        var events = ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified).ToList();
        var preferences = new Dictionary<int, User?>();
        async Task Notify(int id, string title, string message, string link, string category) {
            if (!preferences.TryGetValue(id, out var user)) { user = await Users.FindAsync(new object[] { id }, cancellationToken); preferences[id] = user; }
            if (user == null) return;
            var enabled = category switch { "messages" => user.NotifyMessages, "offers" => user.NotifyOffers, "orders" => user.NotifyOrders, "rentals" => user.NotifyRentals, "saved" => user.NotifySavedListings, _ => true };
            if (enabled) Notifications.Add(new Notification { UserId = id, Title = title, Message = message, Link = link, Type = category });
        }
        foreach (var e in events)
        {
            switch (e.Entity)
            {
                case Offer o:
                    await Notify(o.BuyerId, "Offer update", $"Offer for item #{o.ProductId}: {o.Status} · ৳{o.OfferAmount}", "/dashboard/offers", "offers");
                    await Notify(o.SellerId, "Offer update", $"Offer for item #{o.ProductId}: {o.Status} · ৳{o.OfferAmount}", "/dashboard/offers", "offers"); break;
                case Order o:
                    await Notify(o.BuyerId, "Order update", $"Item #{o.ProductId}: {o.Status}. Pickup: {o.PickupStatus}",  e.State == EntityState.Added ? "/dashboard/purchases" : $"/orders/{o.Id}", o.RentalDays.HasValue ? "rentals" : "orders");
                    await Notify(o.SellerId, "Order update", $"Item #{o.ProductId}: {o.Status}. Pickup: {o.PickupStatus}",  e.State == EntityState.Added ? "/dashboard/sales" : $"/orders/{o.Id}", o.RentalDays.HasValue ? "rentals" : "orders"); break;
                case Message m when e.State == EntityState.Added:
                    var c = await Conversations.FindAsync(new object[] { m.ConversationId }, cancellationToken);
                    if (c != null) await Notify(m.SenderId == c.BuyerId ? c.SellerId : c.BuyerId, "New message", "You received a marketplace message.", $"/messages/{c.Id}", "messages"); break;
                case Review r when e.State == EntityState.Added:
                    await Notify(r.ReviewedUserId, "New review", $"You received a {r.Rating}-star review.", $"/students/{r.ReviewedUserId}", "reviews"); break;
                case Product p when e.State == EntityState.Modified && e.Property("Status").IsModified:
                    foreach (var id in await Wishlist.Where(w => w.ProductId == p.Id).Select(w => w.UserId).ToListAsync(cancellationToken))
                        await Notify(id, "Saved item update", $"{p.Title}: {p.Status}", "/dashboard/wishlist", "saved"); break;
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) => ConfigureModel(modelBuilder);

    internal static void ConfigureModel(ModelBuilder modelBuilder)
    {
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
        modelBuilder.Entity<StoreProduct>().ToTable(t => { t.HasCheckConstraint("CK_StoreProducts_Quantity", "\"Quantity\" >= 0"); t.HasCheckConstraint("CK_StoreProducts_SortOrder", "\"SortOrder\" >= 0"); });
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
    }
}
