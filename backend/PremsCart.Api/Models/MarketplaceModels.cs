namespace PremsCart.Api.Models;

public class Role { public int Id { get; set; } public string RoleName { get; set; } = ""; }
public class University { public int Id { get; set; } public string UniversityName { get; set; } = ""; public string EmailDomain { get; set; } = ""; public string? Address { get; set; } }
public class Department { public int Id { get; set; } public string DepartmentName { get; set; } = ""; public string Code { get; set; } = ""; }
public class Category { public int Id { get; set; } public string CategoryName { get; set; } = ""; public string? Description { get; set; } public string? Image { get; set; } }
public class PickupLocation { public int Id { get; set; } public string LocationName { get; set; } = ""; public string? Description { get; set; } }

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Status { get; set; } = "Active";
    public string? SuspensionReason { get; set; }
    public int TokenVersion { get; set; }
    public string PasswordHash { get; set; } = "";
    public string UniversityEmail { get; set; } = "";
    public int? Batch { get; set; }
    public string? Department { get; set; }
    public string? Program { get; set; }
    public string? StudentIdLastThreeDigits { get; set; }
    public string? ProfileImage { get; set; }
    public bool IsVerified { get; set; }
    public bool NotifyMessages { get; set; } = true;
    public bool NotifyOffers { get; set; } = true;
    public bool NotifyOrders { get; set; } = true;
    public bool NotifyRentals { get; set; } = true;
    public bool NotifySavedListings { get; set; } = true;

    public int RoleId { get; set; } = 1;
    public Role Role { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Product
{
    public bool IsHidden { get; set; }
    public int Id { get; set; }
    public int SellerId { get; set; }
    public User Seller { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal? Price { get; set; }
    public bool AllowRent { get; set; }
    public decimal? RentalPrice { get; set; }
    public string TransactionType { get; set; } = "Sell";
    public string Condition { get; set; } = "Used";
    public string Status { get; set; } = "Available";
    public string? Location { get; set; }
    public bool IsNegotiable { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ProductImage> Images { get; set; } = [];
}

public class ProductImage { public int Id { get; set; } public int ProductId { get; set; } public Product Product { get; set; } = null!; public string ImageUrl { get; set; } = ""; public bool IsPrimary { get; set; } }
public class WishlistItem { public int Id { get; set; } public int UserId { get; set; } public User User { get; set; } = null!; public int ProductId { get; set; } public Product Product { get; set; } = null!; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class WantedPost { public bool IsHidden { get; set; } public int Id { get; set; } public int UserId { get; set; } public User User { get; set; } = null!; public string Title { get; set; } = ""; public string Description { get; set; } = ""; public decimal? Budget { get; set; } public int? CategoryId { get; set; } public Category? Category { get; set; } public string Status { get; set; } = "Open"; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class Conversation { public int Id { get; set; } public int ProductId { get; set; } public Product Product { get; set; } = null!; public int BuyerId { get; set; } public User Buyer { get; set; } = null!; public int SellerId { get; set; } public User Seller { get; set; } = null!; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public List<Message> Messages { get; set; } = []; }
public class Message { public bool IsHidden { get; set; } public int Id { get; set; } public int ConversationId { get; set; } public Conversation Conversation { get; set; } = null!; public int SenderId { get; set; } public User Sender { get; set; } = null!; public string MessageText { get; set; } = ""; public bool IsRead { get; set; } public DateTime SentAt { get; set; } = DateTime.UtcNow; }
public class OfferProposal { public int Id { get; set; } public int OfferId { get; set; } public Offer Offer { get; set; } = null!; public int AuthorId { get; set; } public decimal Amount { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class PasswordReset { public int Id { get; set; } public int UserId { get; set; } public string CodeHash { get; set; } = ""; public DateTime ExpiresAt { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public int FailedAttempts { get; set; } }
public class Offer { public int? LastProposerId { get; set; } public List<OfferProposal> Proposals { get; set; } = []; public int Id { get; set; } public int ProductId { get; set; } public Product Product { get; set; } = null!; public int BuyerId { get; set; } public User Buyer { get; set; } = null!; public int SellerId { get; set; } public User Seller { get; set; } = null!; public decimal OfferAmount { get; set; } public string Status { get; set; } = "Pending"; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class Order { public DateOnly? RentalStartDate { get; set; } public int? RentalDays { get; set; } public DateTime? RentalStartedAt { get; set; } public DateTime? RentalDueAt { get; set; } public DateTime? ReturnedAt { get; set; } public int? PickupProposerId { get; set; } public string PickupStatus { get; set; } = "None"; public DateTime? CompletedAt { get; set; } public int Id { get; set; } public int BuyerId { get; set; } public User Buyer { get; set; } = null!; public int SellerId { get; set; } public User Seller { get; set; } = null!; public int ProductId { get; set; } public Product Product { get; set; } = null!; public decimal? FinalPrice { get; set; } public int? PickupLocationId { get; set; } public PickupLocation? PickupLocation { get; set; } public DateTime? PickupTime { get; set; } public string Status { get; set; } = "Pending"; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class Review { public bool IsHidden { get; set; } public int Id { get; set; } public int ReviewerId { get; set; } public User Reviewer { get; set; } = null!; public int ReviewedUserId { get; set; } public User ReviewedUser { get; set; } = null!; public int OrderId { get; set; } public Order Order { get; set; } = null!; public int Rating { get; set; } public string? Comment { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class Store { public bool IsHidden { get; set; } public int Id { get; set; } public int OwnerId { get; set; } public User Owner { get; set; } = null!; public string StoreName { get; set; } = ""; public string? Description { get; set; } public string? Logo { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public List<StoreProduct> StoreProducts { get; set; } = []; }
public class StoreProduct { public int Id { get; set; } public int StoreId { get; set; } public Store Store { get; set; } = null!; public int ProductId { get; set; } public Product Product { get; set; } = null!; public int Quantity { get; set; } = 1; }
public class Report { public string? ResolutionAction { get; set; } public int? ReviewId { get; set; } public int? MessageId { get; set; } public string? ResolutionNote { get; set; } public int? ModeratorId { get; set; } public int Id { get; set; } public int ReporterId { get; set; } public User Reporter { get; set; } = null!; public int? ReportedProductId { get; set; } public Product? ReportedProduct { get; set; } public int? ReportedUserId { get; set; } public User? ReportedUser { get; set; } public string Reason { get; set; } = ""; public string Status { get; set; } = "Pending"; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class Notification { public string Link { get; set; } = "/dashboard"; public string Type { get; set; } = "Activity"; public int Id { get; set; } public int UserId { get; set; } public User User { get; set; } = null!; public string Title { get; set; } = ""; public string Message { get; set; } = ""; public bool IsRead { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }

public class EmailVerification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string CodeHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
