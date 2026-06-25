using Microsoft.EntityFrameworkCore;
using QuickCart.Domain.Catalog.Entities;

namespace QuickCart.Infrastructure.Persistence;

/// <summary>
/// Seeds a sample catalog on first run. Idempotent: no-ops if any category already exists.
/// For SQL Server it applies pending migrations first; for SQLite/in-memory it uses EnsureCreated.
/// </summary>
public static class CatalogSeeder
{
    public static async Task SeedAsync(QuickCartDbContext db, CancellationToken ct = default)
    {
        if (db.Database.IsSqlServer())
            await db.Database.MigrateAsync(ct);
        else
            await db.Database.EnsureCreatedAsync(ct);

        if (await db.Categories.AnyAsync(ct)) return;

        var now = DateTime.UtcNow;

        var grocery     = new Category("Grocery",       "Everyday groceries and pantry staples", now);
        var beverages   = new Category("Beverages",     "Drinks, juices and water",              now);
        var personalCare = new Category("Personal Care","Health and personal care",               now);
        var snacks      = new Category("Snacks",        "Chips, biscuits and treats",            now);
        var electronics = new Category("Electronics",   "Gadgets and accessories",               now);

        db.Categories.AddRange(grocery, beverages, personalCare, snacks, electronics);

        db.Products.AddRange(
            // ── Grocery (7) ─────────────────────────────────────────────────────────
            new Product(grocery.CategoryId, "Basmati Rice 5kg",      "Aged long-grain basmati rice",          499m,  null, 120, now),
            new Product(grocery.CategoryId, "Whole Wheat Flour 2kg", "Stone-ground atta for daily rotis",     120m,  null,  80, now),
            new Product(grocery.CategoryId, "Toor Dal 1kg",          "Premium yellow split pigeon peas",      130m,  null,  90, now),
            new Product(grocery.CategoryId, "Masoor Dal 1kg",        "Red lentils, low-fat protein source",   110m,  null,  70, now),
            new Product(grocery.CategoryId, "Sugar 1kg",             "Fine-grain white sugar",                 50m,  null, 200, now),
            new Product(grocery.CategoryId, "Sunflower Oil 1L",      "Refined sunflower cooking oil",         150m,  null,  60, now),
            new Product(grocery.CategoryId, "Amul Butter 100g",      "Pasteurised salted table butter",        60m,  null,  50, now),

            // ── Beverages (5) ───────────────────────────────────────────────────────
            new Product(beverages.CategoryId, "Orange Juice 1L",         "100% squeezed orange juice",         99m,  null,  60, now),
            new Product(beverages.CategoryId, "Sparkling Water 6×500ml", "Lightly carbonated spring water",   150m,  null,  40, now),
            new Product(beverages.CategoryId, "Mango Frooti 200ml",      "Real mango fruit drink",             20m,  null, 120, now),
            new Product(beverages.CategoryId, "Coconut Water 330ml",     "Natural tender coconut water",       40m,  null,  80, now),
            new Product(beverages.CategoryId, "Green Tea 25 Bags",       "Antioxidant-rich green tea",        120m,  null,  45, now),

            // ── Personal Care (4) ───────────────────────────────────────────────────
            new Product(personalCare.CategoryId, "Toothpaste 100ml",  "Fluoride toothpaste, mint flavour",    75m,  null,   0, now),
            new Product(personalCare.CategoryId, "Shampoo 200ml",     "Anti-dandruff shampoo with neem",     180m,  null,  60, now),
            new Product(personalCare.CategoryId, "Hand Wash 200ml",   "Moisturising liquid hand wash",        65m,  null,  80, now),
            new Product(personalCare.CategoryId, "Face Wash 100ml",   "Oil-control facewash for daily use",  120m,  null,  50, now),

            // ── Snacks (5) ──────────────────────────────────────────────────────────
            new Product(snacks.CategoryId, "Potato Chips 150g",       "Salted potato chips, crispy",          30m,  null, 200, now),
            new Product(snacks.CategoryId, "Chocolate Biscuits 300g", "Chocolate-coated cream biscuits",      60m,  null, 150, now),
            new Product(snacks.CategoryId, "Kurkure Masala 90g",      "Crunchy corn puffs, masala flavour",   20m,  null, 250, now),
            new Product(snacks.CategoryId, "Namkeen Mix 200g",        "Assorted Indian savoury snack mix",    45m,  null, 100, now),
            new Product(snacks.CategoryId, "Digestive Biscuits 200g", "Whole-wheat digestive biscuits",       55m,  null,  90, now),

            // ── Electronics (4) ─────────────────────────────────────────────────────
            new Product(electronics.CategoryId, "USB-C Cable 1m",      "Fast-charge braided cable, 65W",     299m,  null,  90, now),
            new Product(electronics.CategoryId, "Wireless Earbuds",    "Bluetooth 5.3 earbuds with case",    999m,  null,  35, now),
            new Product(electronics.CategoryId, "Phone Stand",         "Adjustable desktop phone holder",    199m,  null,  30, now),
            new Product(electronics.CategoryId, "Power Bank 10000mAh", "Slim dual-output power bank",        799m,  null,  15, now));

        await db.SaveChangesAsync(ct);
    }
}
