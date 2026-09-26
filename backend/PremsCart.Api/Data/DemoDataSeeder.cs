using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PremsCart.Api.Models;

namespace PremsCart.Api.Data;

/// <summary>
/// Optional showcase data for short-lived university demos (for example a free Render deployment).
/// Enable with DemoData__Enabled=true. The seeder is idempotent: it uses a marker account and
/// does nothing after the showcase dataset has been created once.
///
/// Demo product images intentionally use externally hosted CC0/public-domain Wikimedia Commons
/// photographs so they survive free-host filesystem resets. See DemoData/IMAGE_SOURCES.md.
/// </summary>
public static class DemoDataSeeder
{
    private const string MarkerEmail = "showcase.owner_44901@bscse.puc.ac.bd";

    private static readonly string BooksImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/ee/Books_stack_in_Gramedia_Book_Store.jpg/960px-Books_stack_in_Gramedia_Book_Store.jpg";
    private static readonly string LaptopImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/61/Laptop_on_a_desk.jpg/960px-Laptop_on_a_desk.jpg";
    private static readonly string MouseImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/d/dc/Computer_mice.jpg/960px-Computer_mice.jpg";
    private static readonly string KeyboardImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/09/Keyboard_close-up.jpg/960px-Keyboard_close-up.jpg";
    private static readonly string HeadphonesImage = "https://upload.wikimedia.org/wikipedia/commons/8/81/Headphones_%2856330%29.jpg";
    private static readonly string BackpackImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a5/Wenger_backpack.jpg/960px-Wenger_backpack.jpg";
    private static readonly string NotebookImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/ef/Desk-laptop-notebook-pen_%2823958719819%29.jpg/960px-Desk-laptop-notebook-pen_%2823958719819%29.jpg";
    private static readonly string HoodieImage = "https://upload.wikimedia.org/wikipedia/commons/f/f4/White_Hoodie.jpg";
    private static readonly string PhoneImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/89/Smartphone_display_screen.jpg/960px-Smartphone_display_screen.jpg";
    private static readonly string ChargerImage = "https://upload.wikimedia.org/wikipedia/commons/c/cd/Charger_image.jpg";
    private static readonly string ShoesImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4f/Shoes_photo.jpg/960px-Shoes_photo.jpg";
    private static readonly string BottleImage = "https://upload.wikimedia.org/wikipedia/commons/a/a2/Blue_Water_Bottle.jpg";
    private static readonly string SandwichImage = "https://upload.wikimedia.org/wikipedia/commons/6/66/Breakfast_Sandwich.jpg";
    private static readonly string SnackImage = "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4d/Chocolate_snack.jpg/960px-Chocolate_snack.jpg";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("DemoData:Enabled")) return;

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PremsCartDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        if (await db.Users.AnyAsync(x => x.Email == MarkerEmail)) return;

        var password = configuration["DemoData:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length < 10)
            throw new InvalidOperationException("Set DemoData:Password to a private password with at least 10 characters before enabling showcase data.");

        var now = DateTime.UtcNow;

        User MakeUser(string first, string last, string email, int roleId = 1, int daysAgo = 15, bool verified = true, string status = "Active")
        {
            var suffix = email.Split('_')[1].Split('@')[0];
            var u = new User
            {
                FirstName = first,
                LastName = last,
                Email = email,
                UniversityEmail = email,
                Batch = int.TryParse(suffix[..2], out var batch) ? batch : 44,
                StudentIdLastThreeDigits = suffix[^3..],
                Program = "BS",
                Department = "CSE",
                RoleId = roleId,
                IsVerified = verified,
                Status = status,
                CreatedAt = now.AddDays(-daysAgo),
                UpdatedAt = now.AddDays(-Math.Min(daysAgo, 2))
            };
            u.PasswordHash = hasher.HashPassword(u, password);
            return u;
        }

        var techOwner = MakeUser("Rafi", "Hossain", MarkerEmail, 2, 30);
        var bookOwner = MakeUser("Nusrat", "Jahan", "showcase.books_44902@bscse.puc.ac.bd", 2, 28);
        var styleOwner = MakeUser("Tanjim", "Ahmed", "showcase.style_44903@bscse.puc.ac.bd", 2, 24);
        var studyOwner = MakeUser("Mehjabin", "Sultana", "showcase.study_44904@bscse.puc.ac.bd", 2, 22);

        var ayesha = MakeUser("Ayesha", "Rahman", "showcase.ayesha_44911@bscse.puc.ac.bd", 1, 20);
        var nafis = MakeUser("Nafis", "Ahmed", "showcase.nafis_44912@bscse.puc.ac.bd", 1, 18);
        var samira = MakeUser("Samira", "Khan", "showcase.samira_44913@bscse.puc.ac.bd", 1, 17);
        var farhan = MakeUser("Farhan", "Karim", "showcase.farhan_44914@bscse.puc.ac.bd", 1, 16);
        var mim = MakeUser("Mim", "Akter", "showcase.mim_44915@bscse.puc.ac.bd", 1, 14);
        var arif = MakeUser("Arif", "Mahmud", "showcase.arif_44916@bscse.puc.ac.bd", 1, 13);
        var tisha = MakeUser("Tisha", "Noor", "showcase.tisha_44917@bscse.puc.ac.bd", 1, 12);
        // Non-loginable moderation examples make the Admin > Users screen useful without creating
        // a shared privileged demo account.
        var pendingVerification = MakeUser("Pending", "Student", "showcase.pending_44919@bscse.puc.ac.bd", 1, 8, verified: false);
        var suspendedStudent = MakeUser("Suspended", "Student", "showcase.suspended_44920@bscse.puc.ac.bd", 1, 10, status: "Suspended");
        suspendedStudent.SuspensionReason = "Demo moderation example";

        var users = new[] { techOwner, bookOwner, styleOwner, studyOwner, ayesha, nafis, samira, farhan, mim, arif, tisha, pendingVerification, suspendedStudent };
        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        var categories = await db.Categories.ToDictionaryAsync(x => x.CategoryName, x => x.Id);
        int Cat(string name) => categories[name];

        var stores = new[]
        {
            new Store { OwnerId = techOwner.Id, StoreName = "Campus Tech Hub", Description = "Affordable electronics, chargers and computer accessories for campus life.", Logo = LaptopImage, CreatedAt = now.AddDays(-27) },
            new Store { OwnerId = bookOwner.Id, StoreName = "Book Nook PU", Description = "Textbooks, reference books and exam-prep materials from students.", Logo = BooksImage, CreatedAt = now.AddDays(-26) },
            new Store { OwnerId = styleOwner.Id, StoreName = "Campus Closet", Description = "Pre-loved clothing, shoes and everyday student accessories.", Logo = HoodieImage, CreatedAt = now.AddDays(-21) },
            new Store { OwnerId = studyOwner.Id, StoreName = "Study Spot", Description = "Notebooks, bags and practical study essentials for classes and labs.", Logo = NotebookImage, CreatedAt = now.AddDays(-20) }
        };
        db.Stores.AddRange(stores);
        await db.SaveChangesAsync();

        Product P(User seller, string title, string description, string category, decimal? price, string type, string condition, string location, string image, bool negotiable = false, bool allowRent = false, decimal? rentalPrice = null, int daysAgo = 0)
        {
            var p = new Product
            {
                SellerId = seller.Id,
                CategoryId = Cat(category),
                Title = title,
                Description = description,
                Price = type == "Giveaway" ? 0 : price,
                TransactionType = type,
                Condition = condition,
                Status = "Available",
                Location = location,
                IsNegotiable = negotiable && type == "Sell",
                AllowRent = allowRent,
                RentalPrice = allowRent ? rentalPrice : null,
                CreatedAt = now.AddDays(-daysAgo)
            };
            p.Images.Add(new ProductImage { ImageUrl = image, IsPrimary = true });
            return p;
        }

        var products = new List<Product>
        {
            // Campus Tech Hub
            P(techOwner, "Student Laptop - 14 inch", "Reliable laptop for assignments, coding and presentations. Charger included.", "Electronics", 28500, "Sell", "Good", "Library", LaptopImage, true, true, 900, 1),
            P(techOwner, "Wireless Mouse", "Comfortable wireless mouse with USB receiver. Tested and working.", "Electronics", 650, "Sell", "Good", "Main gate", MouseImage, true, false, null, 2),
            P(techOwner, "Mechanical Keyboard", "Full-size keyboard in good working condition. Great for coding and reports.", "Electronics", 1800, "Sell", "Like new", "Library", KeyboardImage, true, true, 120, 3),
            P(techOwner, "Over-ear Headphones", "Comfortable headphones for online classes, study sessions and music.", "Electronics", 1200, "Sell", "Good", "Canteen", HeadphonesImage, true, true, 100, 4),
            P(techOwner, "Android Phone - Backup Device", "Useful backup phone for calls, messaging and basic apps.", "Electronics", 6500, "Sell", "Fair", "Main gate", PhoneImage, true, false, null, 5),
            P(techOwner, "USB-C Charger & Cable", "Working USB-C charger with cable for everyday campus use.", "Electronics", 500, "Sell", "Good", "Library", ChargerImage, false, false, null, 6),
            P(techOwner, "Laptop for Presentation Day", "Short-term laptop rental for presentations, demos or lab work.", "Electronics", 650, "Rent", "Good", "Library", LaptopImage, false, false, null, 7),
            P(techOwner, "Keyboard - Daily Rental", "Borrow a keyboard for lab work or a short project.", "Electronics", 80, "Rent", "Good", "Main gate", KeyboardImage, false, false, null, 8),

            // Book Nook PU
            P(bookOwner, "Data Structures & Algorithms", "Clean study copy with a few highlighted sections. Useful for CSE courses.", "Books", 550, "Sell", "Good", "Library", BooksImage, true, false, null, 1),
            P(bookOwner, "Computer Networks Textbook", "Networking reference book suitable for labs, assignments and exam revision.", "Books", 650, "Sell", "Good", "Library", BooksImage, true, false, null, 2),
            P(bookOwner, "Discrete Mathematics Book", "Readable copy with solved examples and practice problems.", "Books", 480, "Sell", "Fair", "Canteen", BooksImage, true, false, null, 3),
            P(bookOwner, "Database Systems Reference", "Useful reference for SQL, normalization and database design.", "Books", 700, "Sell", "Like new", "Main gate", BooksImage, true, false, null, 4),
            P(bookOwner, "Software Engineering Notes", "Organized notes and printed slides covering major SE topics.", "Academic Materials", 250, "Sell", "Good", "Library", NotebookImage, false, false, null, 5),
            P(bookOwner, "Old Semester Books Bundle", "Giving away a mixed bundle of older course books to a student who can use them.", "Books", 0, "Giveaway", "Fair", "Library", BooksImage, false, false, null, 6),
            P(bookOwner, "Exam Notes Pack", "Printed revision notes for several CSE subjects. Giving them away to a student who needs them.", "Academic Materials", 0, "Giveaway", "Good", "Canteen", NotebookImage, false, false, null, 7),
            P(bookOwner, "Textbook Rental - Weekend", "Borrow a reference textbook for a few days during exam preparation.", "Books", 60, "Rent", "Good", "Library", BooksImage, false, false, null, 8),

            // Campus Closet
            P(styleOwner, "White Hoodie", "Comfortable hoodie in good condition. Washed and ready to wear.", "Clothing", 900, "Sell", "Good", "Main gate", HoodieImage, true, false, null, 1),
            P(styleOwner, "Campus Casual Hoodie", "Soft everyday hoodie suitable for campus and cooler evenings.", "Clothing", 1100, "Sell", "Like new", "Canteen", HoodieImage, true, false, null, 2),
            P(styleOwner, "Sneakers - Everyday Pair", "Comfortable used pair suitable for everyday campus walking.", "Clothing", 850, "Sell", "Good", "Main gate", ShoesImage, true, false, null, 3),
            P(styleOwner, "Shoes for Event Day", "Short rental for presentations, events or formal campus activities.", "Clothing", 150, "Rent", "Good", "Main gate", ShoesImage, false, false, null, 4),
            P(styleOwner, "Red Student Backpack", "Spacious backpack with multiple compartments for books and a laptop.", "Accessories", 1300, "Sell", "Good", "Library", BackpackImage, true, false, null, 5),
            P(styleOwner, "Reusable Water Bottle", "Clean reusable bottle for daily campus use.", "Accessories", 350, "Sell", "Like new", "Canteen", BottleImage, false, false, null, 6),
            P(styleOwner, "Backpack Giveaway", "Older backpack with plenty of life left. Giving it away to a student who needs one.", "Accessories", 0, "Giveaway", "Fair", "Library", BackpackImage, false, false, null, 7),
            P(styleOwner, "Hoodie Giveaway", "Used hoodie in fair condition. Giving it away instead of storing it.", "Clothing", 0, "Giveaway", "Fair", "Main gate", HoodieImage, false, false, null, 8),

            // Study Spot
            P(studyOwner, "Notebook & Sticky Notes Set", "Notebook and colorful sticky notes for lectures, labs and planning.", "Academic Materials", 280, "Sell", "New", "Canteen", NotebookImage, false, false, null, 1),
            P(studyOwner, "Study Desk Essentials Bundle", "Notebook, pen and sticky-note style study bundle for daily class use.", "Academic Materials", 420, "Sell", "New", "Library", NotebookImage, false, false, null, 2),
            P(studyOwner, "Backpack for Books", "Roomy backpack suitable for textbooks, notebooks and accessories.", "Accessories", 1150, "Sell", "Good", "Main gate", BackpackImage, true, false, null, 3),
            P(studyOwner, "Water Bottle - Blue", "Reusable bottle in excellent condition.", "Accessories", 300, "Sell", "Like new", "Canteen", BottleImage, false, false, null, 4),
            P(studyOwner, "Headphones for Online Class", "Working headphones suitable for online meetings and recorded lectures.", "Electronics", 950, "Sell", "Good", "Library", HeadphonesImage, true, false, null, 5),
            P(studyOwner, "Laptop + Notes Study Setup", "Laptop available for short-term rental with a simple study setup.", "Electronics", 750, "Rent", "Good", "Library", LaptopImage, false, false, null, 6),
            P(studyOwner, "Notebook Bundle Giveaway", "A few unused and partly used notebooks. Giving them away to someone who needs them.", "Academic Materials", 0, "Giveaway", "Good", "Canteen", NotebookImage, false, false, null, 7),
            P(studyOwner, "Spare Phone Charger Giveaway", "Older working charger and cable. Please check connector compatibility.", "Electronics", 0, "Giveaway", "Fair", "Main gate", ChargerImage, false, false, null, 8),

            // Individual student listings, so the marketplace does not look store-only
            P(ayesha, "Algorithms Book - Personal Copy", "Personal copy from last semester with neat highlighting.", "Books", 420, "Sell", "Good", "Library", BooksImage, true, false, null, 1),
            P(nafis, "Wireless Mouse - Student Sale", "Used for one semester and still working well.", "Electronics", 500, "Sell", "Good", "Main gate", MouseImage, true, false, null, 2),
            P(samira, "Notebook Pack", "Three notebooks and sticky notes for class use.", "Academic Materials", 180, "Sell", "Like new", "Canteen", NotebookImage, false, false, null, 3),
            P(farhan, "Headphones - Weekend Rental", "Rent these headphones for a few days for study or travel.", "Electronics", 90, "Rent", "Good", "Library", HeadphonesImage, false, false, null, 4),
            P(mim, "Reusable Bottle Giveaway", "Clean bottle I no longer use. Giving it away to another student.", "Accessories", 0, "Giveaway", "Good", "Canteen", BottleImage, false, false, null, 5),
            P(arif, "Laptop Charger", "Working charger, selling because I upgraded devices.", "Electronics", 450, "Sell", "Good", "Main gate", ChargerImage, true, false, null, 6),
            P(tisha, "Casual Hoodie", "Comfortable hoodie in good condition.", "Clothing", 750, "Sell", "Good", "Library", HoodieImage, true, false, null, 7),
            P(farhan, "Packed Sandwich Lunch", "Freshly packed sandwich lunch for a same-day campus handoff.", "Food", 120, "Sell", "New", "Canteen", SandwichImage, false, false, null, 1),
            P(samira, "Chocolate Snack Pack", "Sealed chocolate snack pack for a quick campus treat.", "Food", 80, "Sell", "New", "Canteen", SnackImage, false, false, null, 1)
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        var storeMap = stores.ToDictionary(x => x.OwnerId);
        foreach (var owner in new[] { techOwner, bookOwner, styleOwner, studyOwner })
        {
            var owned = products.Where(p => p.SellerId == owner.Id).ToList();
            for (var i = 0; i < owned.Count; i++)
            {
                db.StoreProducts.Add(new StoreProduct
                {
                    StoreId = storeMap[owner.Id].Id,
                    ProductId = owned[i].Id,
                    Quantity = owned[i].TransactionType == "Rent" ? 1 : (i % 4 == 0 ? 5 : i % 3 == 0 ? 3 : 1),
                    IsVisible = i != owned.Count - 1,
                    SortOrder = i
                });
            }
        }
        await db.SaveChangesAsync();

        // Wanted/request posts
        db.WantedPosts.AddRange(
            new WantedPost { UserId = ayesha.Id, Title = "Looking for a scientific calculator", Description = "Need a working calculator for exams. Prefer good condition.", Budget = 1000, CategoryId = Cat("Electronics"), Status = "Open", CreatedAt = now.AddDays(-2) },
            new WantedPost { UserId = nafis.Id, Title = "Need Computer Networks textbook", Description = "Looking for a readable copy for this semester.", Budget = 700, CategoryId = Cat("Books"), Status = "Open", CreatedAt = now.AddDays(-4) },
            new WantedPost { UserId = samira.Id, Title = "Need a USB-C charger", Description = "Working charger and cable preferred. Can meet at Main gate.", Budget = 600, CategoryId = Cat("Electronics"), Status = "Open", CreatedAt = now.AddDays(-1) },
            new WantedPost { UserId = farhan.Id, Title = "Looking for DSA notes", Description = "Need concise notes or printed slides for exam revision.", Budget = 250, CategoryId = Cat("Academic Materials"), Status = "Open", CreatedAt = now.AddDays(-3) },
            new WantedPost { UserId = mim.Id, Title = "Need a backpack", Description = "Any usable medium-size backpack is fine.", Budget = 900, CategoryId = Cat("Accessories"), Status = "Open", CreatedAt = now.AddDays(-6) },
            new WantedPost { UserId = arif.Id, Title = "Want a used hoodie", Description = "Looking for an affordable campus hoodie in good condition.", Budget = 800, CategoryId = Cat("Clothing"), Status = "Open", CreatedAt = now.AddDays(-5) },
            new WantedPost { UserId = tisha.Id, Title = "Need Database Systems book", Description = "Any edition with SQL and normalization chapters is useful.", Budget = 650, CategoryId = Cat("Books"), Status = "Open", CreatedAt = now.AddDays(-2) },
            new WantedPost { UserId = ayesha.Id, Title = "Looking for headphones to rent", Description = "Need headphones for a presentation and online meeting for two days.", Budget = 250, CategoryId = Cat("Electronics"), Status = "Open", CreatedAt = now.AddDays(-1) }
        );
        await db.SaveChangesAsync();

        Product Find(string title) => products.Single(x => x.Title == title);

        // Wishlists
        var wishPairs = new[]
        {
            (ayesha, "Mechanical Keyboard"), (ayesha, "Computer Networks Textbook"), (ayesha, "Red Student Backpack"),
            (nafis, "Student Laptop - 14 inch"), (nafis, "Database Systems Reference"),
            (samira, "White Hoodie"), (samira, "Notebook & Sticky Notes Set"),
            (farhan, "Over-ear Headphones"), (farhan, "Reusable Water Bottle"),
            (mim, "Algorithms Book - Personal Copy"), (arif, "Mechanical Keyboard"), (tisha, "Study Desk Essentials Bundle")
        };
        foreach (var (user, title) in wishPairs)
            db.Wishlist.Add(new WishlistItem { UserId = user.Id, ProductId = Find(title).Id, CreatedAt = now.AddHours(-(user.Id % 40)) });
        await db.SaveChangesAsync();

        // Offers in several states
        var mouse = Find("Wireless Mouse");
        var keyboard = Find("Mechanical Keyboard");
        var book = Find("Data Structures & Algorithms");
        var hoodie = Find("White Hoodie");

        var offer1 = new Offer { ProductId = keyboard.Id, BuyerId = ayesha.Id, SellerId = techOwner.Id, OfferAmount = 1550, Status = "Countered", LastProposerId = techOwner.Id, CreatedAt = now.AddHours(-8) };
        var offer2 = new Offer { ProductId = book.Id, BuyerId = nafis.Id, SellerId = bookOwner.Id, OfferAmount = 500, Status = "Pending", LastProposerId = nafis.Id, CreatedAt = now.AddHours(-15) };
        var offer3 = new Offer { ProductId = hoodie.Id, BuyerId = samira.Id, SellerId = styleOwner.Id, OfferAmount = 700, Status = "Rejected", LastProposerId = samira.Id, CreatedAt = now.AddDays(-2) };
        db.Offers.AddRange(offer1, offer2, offer3);
        await db.SaveChangesAsync();
        db.OfferProposals.AddRange(
            new OfferProposal { OfferId = offer1.Id, AuthorId = ayesha.Id, Amount = 1450, CreatedAt = now.AddHours(-9) },
            new OfferProposal { OfferId = offer1.Id, AuthorId = techOwner.Id, Amount = 1550, CreatedAt = now.AddHours(-8) },
            new OfferProposal { OfferId = offer2.Id, AuthorId = nafis.Id, Amount = 500, CreatedAt = now.AddHours(-15) },
            new OfferProposal { OfferId = offer3.Id, AuthorId = samira.Id, Amount = 700, CreatedAt = now.AddDays(-2) }
        );
        await db.SaveChangesAsync();

        // Orders with different lifecycle states
        var libraryId = await db.PickupLocations.Where(x => x.LocationName == "Library").Select(x => x.Id).SingleAsync();
        var gateId = await db.PickupLocations.Where(x => x.LocationName == "Main gate").Select(x => x.Id).SingleAsync();
        var canteenId = await db.PickupLocations.Where(x => x.LocationName == "Canteen").Select(x => x.Id).SingleAsync();

        var completed1 = new Order { BuyerId = ayesha.Id, SellerId = techOwner.Id, ProductId = mouse.Id, FinalPrice = 650, PickupLocationId = gateId, PickupTime = now.AddDays(-5), PickupStatus = "Completed", Status = "Completed", CreatedAt = now.AddDays(-7), CompletedAt = now.AddDays(-5) };
        var completed2 = new Order { BuyerId = nafis.Id, SellerId = bookOwner.Id, ProductId = Find("Discrete Mathematics Book").Id, FinalPrice = 480, PickupLocationId = libraryId, PickupTime = now.AddDays(-4), PickupStatus = "Completed", Status = "Completed", CreatedAt = now.AddDays(-6), CompletedAt = now.AddDays(-4) };
        var completed3 = new Order { BuyerId = samira.Id, SellerId = styleOwner.Id, ProductId = Find("Reusable Water Bottle").Id, FinalPrice = 350, PickupLocationId = canteenId, PickupTime = now.AddDays(-3), PickupStatus = "Completed", Status = "Completed", CreatedAt = now.AddDays(-5), CompletedAt = now.AddDays(-3) };
        var completedGiveaway = new Order { BuyerId = tisha.Id, SellerId = bookOwner.Id, ProductId = Find("Old Semester Books Bundle").Id, FinalPrice = 0, PickupLocationId = libraryId, PickupTime = now.AddDays(-8), PickupStatus = "Completed", Status = "Completed", CreatedAt = now.AddDays(-10), CompletedAt = now.AddDays(-8) };
        var completedRental = new Order { BuyerId = nafis.Id, SellerId = studyOwner.Id, ProductId = Find("Laptop + Notes Study Setup").Id, FinalPrice = 1500, RentalDays = 2, RentalStartDate = DateOnly.FromDateTime(now.AddDays(-8)), RentalStartedAt = now.AddDays(-8), RentalDueAt = now.AddDays(-6), ReturnedAt = now.AddDays(-6), CompletedAt = now.AddDays(-6), PickupLocationId = libraryId, PickupTime = now.AddDays(-8), PickupStatus = "Completed", Status = "Completed", CreatedAt = now.AddDays(-9) };
        var pending = new Order { BuyerId = farhan.Id, SellerId = studyOwner.Id, ProductId = Find("Headphones for Online Class").Id, FinalPrice = 950, PickupStatus = "None", Status = "Pending", CreatedAt = now.AddHours(-4) };
        var accepted = new Order { BuyerId = mim.Id, SellerId = styleOwner.Id, ProductId = Find("Red Student Backpack").Id, FinalPrice = 1300, PickupStatus = "None", Status = "Accepted", CreatedAt = now.AddHours(-10) };
        var pickup = new Order { BuyerId = arif.Id, SellerId = bookOwner.Id, ProductId = Find("Database Systems Reference").Id, FinalPrice = 700, PickupLocationId = libraryId, PickupTime = now.AddDays(1).Date.AddHours(9), PickupStatus = "Confirmed", PickupProposerId = bookOwner.Id, Status = "Pickup scheduled", CreatedAt = now.AddDays(-1) };
        var rented = new Order { BuyerId = tisha.Id, SellerId = techOwner.Id, ProductId = Find("Laptop for Presentation Day").Id, FinalPrice = 1950, RentalDays = 3, RentalStartDate = DateOnly.FromDateTime(now.AddDays(-1)), RentalStartedAt = now.AddHours(-20), RentalDueAt = now.AddDays(2), PickupLocationId = libraryId, PickupTime = now.AddHours(-20), PickupStatus = "Completed", Status = "Rented", CreatedAt = now.AddDays(-2) };
        var returning = new Order { BuyerId = ayesha.Id, SellerId = styleOwner.Id, ProductId = Find("Shoes for Event Day").Id, FinalPrice = 300, RentalDays = 2, RentalStartDate = DateOnly.FromDateTime(now.AddDays(-3)), RentalStartedAt = now.AddDays(-3), RentalDueAt = now.AddDays(-1), PickupLocationId = gateId, PickupTime = now.AddDays(-3), PickupStatus = "Completed", Status = "Return requested", CreatedAt = now.AddDays(-4) };
        var cancelled = new Order { BuyerId = arif.Id, SellerId = techOwner.Id, ProductId = Find("Android Phone - Backup Device").Id, FinalPrice = 6500, PickupStatus = "None", Status = "Cancelled", CreatedAt = now.AddDays(-4) };
        db.Orders.AddRange(completed1, completed2, completed3, completedGiveaway, completedRental, pending, accepted, pickup, rented, returning, cancelled);
        await db.SaveChangesAsync();

        // Keep item/stock state consistent with the lifecycle examples above.
        Find("Wireless Mouse").Status = "Sold";
        Find("Discrete Mathematics Book").Status = "Sold";
        Find("Reusable Water Bottle").Status = "Sold";
        Find("Old Semester Books Bundle").Status = "GivenAway";
        Find("Laptop for Presentation Day").Status = "Reserved";
        Find("Shoes for Event Day").Status = "Reserved";
        foreach (var title in new[] { "Wireless Mouse", "Discrete Mathematics Book", "Reusable Water Bottle", "Old Semester Books Bundle" })
        {
            var productId = Find(title).Id;
            var stock = await db.StoreProducts.SingleAsync(x => x.ProductId == productId);
            stock.Quantity = 0;
        }
        (await db.StoreProducts.SingleAsync(x => x.ProductId == Find("Red Student Backpack").Id)).Quantity = 4;
        (await db.StoreProducts.SingleAsync(x => x.ProductId == Find("Database Systems Reference").Id)).Quantity = 2;
        await db.SaveChangesAsync();

        db.Reviews.AddRange(
            new Review { OrderId = completed1.Id, ReviewerId = ayesha.Id, ReviewedUserId = techOwner.Id, Rating = 5, Comment = "Quick reply and smooth handoff at the gate.", CreatedAt = now.AddDays(-5).AddHours(2) },
            new Review { OrderId = completed1.Id, ReviewerId = techOwner.Id, ReviewedUserId = ayesha.Id, Rating = 5, Comment = "Friendly buyer and arrived on time.", CreatedAt = now.AddDays(-5).AddHours(3) },
            new Review { OrderId = completed2.Id, ReviewerId = nafis.Id, ReviewedUserId = bookOwner.Id, Rating = 4, Comment = "Book was exactly as described and easy pickup.", CreatedAt = now.AddDays(-4).AddHours(1) },
            new Review { OrderId = completed3.Id, ReviewerId = samira.Id, ReviewedUserId = styleOwner.Id, Rating = 5, Comment = "Very easy campus meetup and item was clean.", CreatedAt = now.AddDays(-3).AddHours(2) },
            new Review { OrderId = completedGiveaway.Id, ReviewerId = tisha.Id, ReviewedUserId = bookOwner.Id, Rating = 5, Comment = "Kind giveaway and the books are still useful for study.", CreatedAt = now.AddDays(-8).AddHours(1) },
            new Review { OrderId = completedRental.Id, ReviewerId = nafis.Id, ReviewedUserId = studyOwner.Id, Rating = 5, Comment = "Rental was smooth and the laptop worked well for my assignment.", CreatedAt = now.AddDays(-6).AddHours(2) }
        );
        await db.SaveChangesAsync();

        // Conversations + message history
        var c1 = new Conversation { ProductId = keyboard.Id, BuyerId = ayesha.Id, SellerId = techOwner.Id, CreatedAt = now.AddHours(-11) };
        var c2 = new Conversation { ProductId = book.Id, BuyerId = nafis.Id, SellerId = bookOwner.Id, CreatedAt = now.AddHours(-18) };
        var c3 = new Conversation { ProductId = hoodie.Id, BuyerId = samira.Id, SellerId = styleOwner.Id, CreatedAt = now.AddDays(-2) };
        var c4 = new Conversation { ProductId = Find("Headphones for Online Class").Id, BuyerId = farhan.Id, SellerId = studyOwner.Id, CreatedAt = now.AddHours(-6) };
        db.Conversations.AddRange(c1, c2, c3, c4);
        await db.SaveChangesAsync();
        db.Messages.AddRange(
            new Message { ConversationId = c1.Id, SenderId = ayesha.Id, MessageText = "Hi! Is the keyboard still available?", IsRead = true, SentAt = now.AddHours(-10.5) },
            new Message { ConversationId = c1.Id, SenderId = techOwner.Id, MessageText = "Yes, it is. You can check it at the Library before deciding.", IsRead = true, SentAt = now.AddHours(-10) },
            new Message { ConversationId = c1.Id, SenderId = ayesha.Id, MessageText = "Great. I also sent an offer.", IsRead = false, SentAt = now.AddHours(-8.5) },
            new Message { ConversationId = c2.Id, SenderId = nafis.Id, MessageText = "Does this book have any missing pages?", IsRead = true, SentAt = now.AddHours(-17) },
            new Message { ConversationId = c2.Id, SenderId = bookOwner.Id, MessageText = "No missing pages. A few chapters have highlighting only.", IsRead = false, SentAt = now.AddHours(-16) },
            new Message { ConversationId = c3.Id, SenderId = samira.Id, MessageText = "Can I see the hoodie near Main gate tomorrow?", IsRead = true, SentAt = now.AddDays(-2).AddHours(2) },
            new Message { ConversationId = c3.Id, SenderId = styleOwner.Id, MessageText = "Sure. Message me when you are on campus.", IsRead = true, SentAt = now.AddDays(-2).AddHours(3) },
            new Message { ConversationId = c4.Id, SenderId = farhan.Id, MessageText = "I need these for an online class. Can I collect today?", IsRead = false, SentAt = now.AddHours(-5) }
        );
        await db.SaveChangesAsync();

        // A few moderation examples so admin/moderator screens are not empty.
        db.Reports.AddRange(
            new Report { ReporterId = mim.Id, ReportedProductId = Find("Android Phone - Backup Device").Id, Reason = "Description may need more detail about battery condition.", Status = "Pending", CreatedAt = now.AddHours(-5) },
            new Report { ReporterId = arif.Id, ReportedUserId = nafis.Id, Reason = "Demo report for moderator workflow testing.", Status = "Resolved", ResolutionAction = "No action", ResolutionNote = "Reviewed for demo; no violation found.", CreatedAt = now.AddDays(-3) }
        );
        await db.SaveChangesAsync();

        db.Notifications.AddRange(
            new Notification { UserId = ayesha.Id, Title = "Welcome to the PremsCart demo", Message = "Your demo account includes saved items, messages, offers and order history.", Link = "/dashboard", Type = "account", CreatedAt = now.AddMinutes(-30) },
            new Notification { UserId = nafis.Id, Title = "Wanted post activity", Message = "Your Computer Networks request is visible to campus sellers.", Link = "/requests", Type = "orders", CreatedAt = now.AddHours(-3) },
            new Notification { UserId = techOwner.Id, Title = "Store ready", Message = "Campus Tech Hub has products, inventory and incoming activity for demonstration.", Link = "/dashboard/store", Type = "account", CreatedAt = now.AddHours(-2) }
        );
        await db.SaveChangesAsync();
    }
}
