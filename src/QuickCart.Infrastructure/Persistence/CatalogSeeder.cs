using Microsoft.EntityFrameworkCore;
using QuickCart.Domain.Catalog.Entities;

namespace QuickCart.Infrastructure.Persistence;

/// <summary>
/// Replaces the entire catalog on every run.
/// For SQL Server the schema is managed by EF Core migrations applied separately (not on
/// startup) so the runtime identity never needs DDL permissions. For SQLite (local dev /
/// integration tests) EnsureCreatedAsync is still called so the database is bootstrapped
/// automatically without a manual migration step.
/// </summary>
public static class CatalogSeeder
{
    private const string ImgBase = "assets/images/products/";

    public static async Task SeedAsync(QuickCartDbContext db, CancellationToken ct = default)
    {
        if (!db.Database.IsSqlServer())
            await db.Database.EnsureCreatedAsync(ct);

        // Hard-delete all existing catalog data (ignore soft-delete query filters).
        // Products reference Categories via FK, so delete products first.
        await db.Products.IgnoreQueryFilters().ExecuteDeleteAsync(ct);
        await db.Categories.IgnoreQueryFilters().ExecuteDeleteAsync(ct);

        var now = DateTime.UtcNow;

        // ── Categories ──────────────────────────────────────────────────────────
        var fruitsVeg      = new Category("Fruits & Vegetables", "Fresh farm produce delivered daily", now);
        var dairy          = new Category("Dairy & Breakfast",   "Fresh dairy products and breakfast essentials", now);
        var grocery        = new Category("Grocery & Staples",   "Pantry essentials and everyday cooking needs", now);
        var snacks         = new Category("Snacks & Munchies",   "Chips, biscuits, chocolates and savoury treats", now);
        var beverages      = new Category("Beverages",           "Juices, water, coffee and cold drinks", now);
        var frozen         = new Category("Frozen Foods",        "Ready-to-cook frozen meals and ice creams", now);
        var personalCare   = new Category("Personal Care",       "Skincare, hair care and hygiene products", now);
        var homeCleaning   = new Category("Home & Cleaning",     "Detergents, floor cleaners and household supplies", now);
        var health         = new Category("Health & Wellness",   "Vitamins, supplements and wellness products", now);
        var electronics    = new Category("Electronics & Accessories", "Cables, earbuds, chargers and gadgets", now);
        var petCare        = new Category("Pet Care",            "Food, treats and care for your furry friends", now);
        var bakery         = new Category("Bread & Bakery",      "Fresh breads, buns, biscuits and baked goods", now);

        db.Categories.AddRange(
            fruitsVeg, dairy, grocery, snacks, beverages,
            frozen, personalCare, homeCleaning, health,
            electronics, petCare, bakery);

        // ── Products ────────────────────────────────────────────────────────────
        db.Products.AddRange(

            // ── Fruits & Vegetables (12) ─────────────────────────────────────────
            new Product(fruitsVeg.CategoryId, "Apple (Red) 1kg",
                "Farm-fresh Indian red apples, rich in fibre and antioxidants",
                80m, $"{ImgBase}apple.svg", 150, now),

            new Product(fruitsVeg.CategoryId, "Banana 6pcs",
                "Ripe Cavendish bananas, naturally sweet and energy-packed",
                40m, $"{ImgBase}banana.svg", 200, now),

            new Product(fruitsVeg.CategoryId, "Orange 4pcs",
                "Juicy Nagpur oranges, high in Vitamin C",
                80m, $"{ImgBase}orange.svg", 120, now),

            new Product(fruitsVeg.CategoryId, "Alphonso Mango 3pcs",
                "Premium Ratnagiri Alphonso mangoes, the king of fruits",
                180m, $"{ImgBase}mango.svg", 80, now),

            new Product(fruitsVeg.CategoryId, "Green Grapes 500g",
                "Seedless green grapes, crisp and refreshing",
                99m, $"{ImgBase}grapes-green.svg", 100, now),

            new Product(fruitsVeg.CategoryId, "Tomato 1kg",
                "Fresh ripe tomatoes, ideal for curries and salads",
                35m, $"{ImgBase}tomato.svg", 180, now),

            new Product(fruitsVeg.CategoryId, "Potato 2kg",
                "Fresh washed potatoes, perfect for everyday cooking",
                50m, $"{ImgBase}potato.svg", 200, now),

            new Product(fruitsVeg.CategoryId, "Onion 1kg",
                "Fresh red onions, a kitchen staple for all Indian recipes",
                40m, $"{ImgBase}onion.svg", 200, now),

            new Product(fruitsVeg.CategoryId, "Fresh Spinach 250g",
                "Tender baby spinach leaves, washed and ready to cook",
                30m, $"{ImgBase}spinach.svg", 100, now),

            new Product(fruitsVeg.CategoryId, "Carrot 500g",
                "Crunchy orange carrots, rich in beta-carotene",
                40m, $"{ImgBase}carrot.svg", 150, now),

            new Product(fruitsVeg.CategoryId, "Cucumber 500g",
                "Cool and hydrating cucumbers, great for salads",
                25m, $"{ImgBase}cucumber.svg", 120, now),

            new Product(fruitsVeg.CategoryId, "Lemon 6pcs",
                "Tangy fresh lemons for cooking, dressings and drinks",
                30m, $"{ImgBase}lemon.svg", 150, now),

            // ── Dairy & Breakfast (10) ───────────────────────────────────────────
            new Product(dairy.CategoryId, "Amul Full Cream Milk 1L",
                "Pasteurised full cream milk with rich taste and high nutrition",
                64m, $"{ImgBase}amul-milk.svg", 200, now),

            new Product(dairy.CategoryId, "Amul Dahi 400g",
                "Thick and creamy set curd, made with pure milk",
                40m, $"{ImgBase}curd.svg", 150, now),

            new Product(dairy.CategoryId, "Amul Paneer 200g",
                "Soft and fresh cottage cheese, ideal for curries and tikkas",
                95m, $"{ImgBase}paneer.svg", 100, now),

            new Product(dairy.CategoryId, "Amul Butter 100g",
                "Pasteurised salted butter, perfect for spreading and cooking",
                58m, $"{ImgBase}amul-butter.svg", 120, now),

            new Product(dairy.CategoryId, "Britannia Cheese Slices 10pcs",
                "Processed cheese slices, great for sandwiches and burgers",
                130m, $"{ImgBase}cheese-slices.svg", 80, now),

            new Product(dairy.CategoryId, "Patanjali Pure Ghee 500ml",
                "100% pure cow ghee, made by traditional bilona method",
                350m, $"{ImgBase}ghee.svg", 60, now),

            new Product(dairy.CategoryId, "Farm Fresh Eggs 12pcs",
                "Nutritious cage-free eggs, great source of protein",
                90m, $"{ImgBase}eggs.svg", 150, now),

            new Product(dairy.CategoryId, "Kellogg's Cornflakes 500g",
                "Classic golden cornflakes, a nutritious start to the day",
                135m, $"{ImgBase}cornflakes.svg", 90, now),

            new Product(dairy.CategoryId, "Quaker Oats 500g",
                "100% whole grain rolled oats, high in fibre and protein",
                165m, $"{ImgBase}oats.svg", 80, now),

            new Product(dairy.CategoryId, "Amul Mango Lassi 200ml",
                "Refreshing mango-flavoured yoghurt drink, chilled and delicious",
                30m, $"{ImgBase}mango-lassi.svg", 200, now),

            // ── Grocery & Staples (12) ───────────────────────────────────────────
            new Product(grocery.CategoryId, "India Gate Basmati Rice 5kg",
                "Premium aged long-grain basmati rice with natural aroma",
                499m, $"{ImgBase}basmati-rice.svg", 80, now),

            new Product(grocery.CategoryId, "Aashirvaad Wheat Flour 5kg",
                "Whole wheat atta stone-ground from hand-picked grains",
                185m, $"{ImgBase}wheat-flour.svg", 100, now),

            new Product(grocery.CategoryId, "Tata Sampann Toor Dal 1kg",
                "Premium split pigeon peas, high in plant protein and fibre",
                130m, $"{ImgBase}toor-dal.svg", 120, now),

            new Product(grocery.CategoryId, "Tata Sampann Masoor Dal 1kg",
                "Red lentils with natural goodness, cooks in minutes",
                110m, $"{ImgBase}masoor-dal.svg", 100, now),

            new Product(grocery.CategoryId, "Chana Dal 500g",
                "Split Bengal gram, rich in protein and dietary fibre",
                65m, $"{ImgBase}chana-dal.svg", 100, now),

            new Product(grocery.CategoryId, "Sugar 1kg",
                "Fine-grain refined white sugar for everyday cooking",
                50m, $"{ImgBase}sugar.svg", 200, now),

            new Product(grocery.CategoryId, "Tata Iodised Salt 1kg",
                "Free-flowing iodised salt enriched with natural minerals",
                22m, $"{ImgBase}iodised-salt.svg", 300, now),

            new Product(grocery.CategoryId, "Fortune Sunflower Oil 1L",
                "Light and healthy refined sunflower cooking oil",
                145m, $"{ImgBase}sunflower-oil.svg", 90, now),

            new Product(grocery.CategoryId, "DiSano Olive Oil 500ml",
                "Extra virgin olive oil, cold pressed from the finest olives",
                499m, $"{ImgBase}olive-oil.svg", 40, now),

            new Product(grocery.CategoryId, "MDH Turmeric Powder 100g",
                "Pure haldi with deep colour, earthy aroma and health benefits",
                55m, $"{ImgBase}turmeric-powder.svg", 150, now),

            new Product(grocery.CategoryId, "Everest Red Chilli Powder 100g",
                "Vibrant red chilli powder with rich colour and medium heat",
                60m, $"{ImgBase}red-chilli.svg", 130, now),

            new Product(grocery.CategoryId, "Everest Coriander Powder 100g",
                "Fresh ground coriander powder with a mild, citrusy flavour",
                45m, $"{ImgBase}coriander-powder.svg", 120, now),

            // ── Snacks & Munchies (10) ───────────────────────────────────────────
            new Product(snacks.CategoryId, "Lay's Classic Salted Chips 52g",
                "Light and crispy salted potato chips, perfect party snack",
                20m, $"{ImgBase}lays-chips.svg", 300, now),

            new Product(snacks.CategoryId, "Kurkure Masala Munch 90g",
                "Crunchy corn puffs with tangy masala spice blend",
                20m, $"{ImgBase}kurkure.svg", 300, now),

            new Product(snacks.CategoryId, "Parle-G Original Biscuits 800g",
                "India's most loved glucose biscuit, great with tea",
                55m, $"{ImgBase}parle-g.svg", 200, now),

            new Product(snacks.CategoryId, "Oreo Original Cookies 300g",
                "Classic chocolate sandwich cookies with cream filling",
                130m, $"{ImgBase}oreo.svg", 150, now),

            new Product(snacks.CategoryId, "Haldiram's Bhujia 200g",
                "Crispy bikaneri bhujia sev with authentic spice mix",
                65m, $"{ImgBase}bhujia.svg", 180, now),

            new Product(snacks.CategoryId, "McVitie's Digestive 250g",
                "Wholesome whole-wheat digestive biscuits, lightly sweetened",
                80m, $"{ImgBase}mcvities.svg", 140, now),

            new Product(snacks.CategoryId, "Amul Dark Chocolate 150g",
                "Rich 55% cocoa dark chocolate, smooth and indulgent",
                99m, $"{ImgBase}dark-chocolate.svg", 100, now),

            new Product(snacks.CategoryId, "Act II Butter Popcorn 100g",
                "Microwave-ready buttery popcorn, movie night favourite",
                45m, $"{ImgBase}butter-popcorn.svg", 200, now),

            new Product(snacks.CategoryId, "Roasted Peanuts 200g",
                "Crunchy salted roasted peanuts, high in protein",
                45m, $"{ImgBase}peanuts.svg", 150, now),

            new Product(snacks.CategoryId, "Haldiram's Rice Murukku 200g",
                "Traditional south Indian rice chakli, crispy and savoury",
                65m, $"{ImgBase}murukku.svg", 120, now),

            // ── Beverages (10) ───────────────────────────────────────────────────
            new Product(beverages.CategoryId, "Tropicana Orange Juice 1L",
                "100% real orange juice with no added sugar or preservatives",
                110m, $"{ImgBase}tropicana-orange.svg", 100, now),

            new Product(beverages.CategoryId, "Frooti Mango Drink 200ml",
                "Fresh-N-Juicy mango fruit drink, a childhood favourite",
                20m, $"{ImgBase}frooti.svg", 300, now),

            new Product(beverages.CategoryId, "Bisleri Mineral Water 1L",
                "Pure and safe mineral water in BPA-free bottle",
                20m, $"{ImgBase}mineral-water.svg", 400, now),

            new Product(beverages.CategoryId, "Tetley Green Tea 25 Bags",
                "Light and refreshing green tea, rich in antioxidants",
                120m, $"{ImgBase}green-tea.svg", 100, now),

            new Product(beverages.CategoryId, "Nescafe Classic Coffee 50g",
                "Intense and aromatic instant coffee for the perfect morning cup",
                185m, $"{ImgBase}nescafe.svg", 80, now),

            new Product(beverages.CategoryId, "Raw Pressery Coconut Water 200ml",
                "Natural tender coconut water with electrolytes, no added sugar",
                45m, $"{ImgBase}coconut-water.svg", 150, now),

            new Product(beverages.CategoryId, "Red Bull Energy Drink 250ml",
                "The world's most popular energy drink for extra performance",
                130m, $"{ImgBase}red-bull.svg", 100, now),

            new Product(beverages.CategoryId, "Minute Maid Nimbu Fresh 400ml",
                "Zingy lemon drink with the freshness of real nimbu",
                25m, $"{ImgBase}nimbu-pani.svg", 200, now),

            new Product(beverages.CategoryId, "Rooh Afza Rose Sherbet 750ml",
                "The original rose-flavoured refreshing sherbet concentrate",
                185m, $"{ImgBase}rooh-afza.svg", 60, now),

            new Product(beverages.CategoryId, "Blue Tokai Cold Brew 200ml",
                "Smooth and bold cold brew coffee, ready to drink",
                140m, $"{ImgBase}cold-brew.svg", 50, now),

            // ── Frozen Foods (8) ─────────────────────────────────────────────────
            new Product(frozen.CategoryId, "Safal Frozen Green Peas 500g",
                "Flash-frozen green peas retaining full nutrition and sweetness",
                75m, $"{ImgBase}frozen-peas.svg", 80, now),

            new Product(frozen.CategoryId, "Safal Frozen Sweet Corn 500g",
                "Sweet and tender corn kernels, frozen at peak freshness",
                80m, $"{ImgBase}frozen-corn.svg", 80, now),

            new Product(frozen.CategoryId, "McCain French Fries 400g",
                "Golden oven-ready French fries, crispy and delicious",
                150m, $"{ImgBase}frozen-fries.svg", 70, now),

            new Product(frozen.CategoryId, "Aashirvaad Parathas 5pcs",
                "Frozen whole wheat parathas, ready in 2 minutes on a tawa",
                95m, $"{ImgBase}frozen-paratha.svg", 60, now),

            new Product(frozen.CategoryId, "Haldiram's Frozen Samosa 10pcs",
                "Crispy fried samosas filled with spiced potato, just heat and eat",
                130m, $"{ImgBase}frozen-samosa.svg", 50, now),

            new Product(frozen.CategoryId, "Amul Vanilla Ice Cream 1L",
                "Creamy and rich vanilla ice cream made with real milk",
                175m, $"{ImgBase}vanilla-ice-cream.svg", 80, now),

            new Product(frozen.CategoryId, "Safal Mixed Vegetable Pack 500g",
                "Colourful mix of frozen peas, carrots, corn and beans",
                65m, $"{ImgBase}frozen-veg-mix.svg", 70, now),

            new Product(frozen.CategoryId, "Dr. Oetker Thin Pizza Base 2pcs",
                "Crispy thin pizza bases, ready to top and bake in minutes",
                85m, $"{ImgBase}pizza-base.svg", 60, now),

            // ── Personal Care (10) ───────────────────────────────────────────────
            new Product(personalCare.CategoryId, "Colgate MaxFresh 150g",
                "Cooling crystals toothpaste for 12-hour fresh breath",
                89m, $"{ImgBase}colgate.svg", 150, now),

            new Product(personalCare.CategoryId, "Oral-B Soft Toothbrush 2pcs",
                "Soft-bristle toothbrush for gentle and effective cleaning",
                60m, $"{ImgBase}oral-b.svg", 200, now),

            new Product(personalCare.CategoryId, "Head & Shoulders Shampoo 340ml",
                "Anti-dandruff shampoo for up to 100% dandruff-free hair",
                269m, $"{ImgBase}head-shoulders.svg", 100, now),

            new Product(personalCare.CategoryId, "Dove Conditioner 180ml",
                "Intense repair conditioner for strong and smooth hair",
                185m, $"{ImgBase}dove-conditioner.svg", 80, now),

            new Product(personalCare.CategoryId, "Pond's Bright Beauty Face Wash 100g",
                "Brightening face wash with Vitamin B3 for glowing skin",
                155m, $"{ImgBase}ponds-facewash.svg", 90, now),

            new Product(personalCare.CategoryId, "Nivea Soft Cream 100ml",
                "Lightweight moisturising cream with Jojoba Oil and Vitamin E",
                165m, $"{ImgBase}nivea-cream.svg", 80, now),

            new Product(personalCare.CategoryId, "Dettol Hand Wash Refill 750ml",
                "Antibacterial hand wash with 10x better germ protection",
                185m, $"{ImgBase}dettol-handwash.svg", 100, now),

            new Product(personalCare.CategoryId, "Axe Dark Temptation Deo 150ml",
                "Long-lasting anti-perspirant deodorant with dark chocolate scent",
                199m, $"{ImgBase}axe-deo.svg", 80, now),

            new Product(personalCare.CategoryId, "Lotus Sunscreen SPF 50 50g",
                "Broad-spectrum UV protection with light, non-greasy formula",
                299m, $"{ImgBase}lotus-sunscreen.svg", 60, now),

            new Product(personalCare.CategoryId, "Vaseline Lip Therapy 20g",
                "Soothing lip balm with pure petroleum jelly for chapped lips",
                99m, $"{ImgBase}vaseline-lip.svg", 100, now),

            // ── Home & Cleaning (8) ──────────────────────────────────────────────
            new Product(homeCleaning.CategoryId, "Vim Dishwash Bar 250g (2pcs)",
                "Powerful grease-cutting dishwash bar for sparkling clean utensils",
                35m, $"{ImgBase}vim-bar.svg", 200, now),

            new Product(homeCleaning.CategoryId, "Pril Lemon Dish Wash 500ml",
                "Active lime dishwash liquid that cuts through tough grease",
                99m, $"{ImgBase}pril-liquid.svg", 150, now),

            new Product(homeCleaning.CategoryId, "Ariel Matic Detergent 1kg",
                "Advanced front-load detergent powder for stain-free clothes",
                259m, $"{ImgBase}ariel-detergent.svg", 80, now),

            new Product(homeCleaning.CategoryId, "Comfort Fabric Conditioner 860ml",
                "Premium fabric softener for long-lasting freshness",
                199m, $"{ImgBase}comfort-fabric.svg", 60, now),

            new Product(homeCleaning.CategoryId, "Lizol Citrus Floor Cleaner 1L",
                "Powerful disinfectant floor cleaner that kills 99.9% germs",
                195m, $"{ImgBase}lizol-cleaner.svg", 70, now),

            new Product(homeCleaning.CategoryId, "Harpic Max Toilet Cleaner 500ml",
                "Under-rim toilet cleaner that removes stains and limescale",
                115m, $"{ImgBase}harpic.svg", 90, now),

            new Product(homeCleaning.CategoryId, "Air Wick Freshmatic Spray 250ml",
                "Automatic air freshener with continuous fragrance release",
                359m, $"{ImgBase}air-freshener.svg", 50, now),

            new Product(homeCleaning.CategoryId, "Colin Glass Cleaner 500ml",
                "Crystal-clear glass and surface cleaner with no streaks",
                140m, $"{ImgBase}colin.svg", 80, now),


            // ── Health & Wellness (8) ────────────────────────────────────────────
            new Product(health.CategoryId, "Limcee Vitamin C 500mg 15 Tabs",
                "Chewable Vitamin C tablets for immunity and healthy skin",
                22m, $"{ImgBase}vitamin-c.svg", 200, now),

            new Product(health.CategoryId, "HealthVit Vitamin D3 60 Caps",
                "High-potency Vitamin D3 for bone health and immunity",
                299m, $"{ImgBase}vitamin-d.svg", 80, now),

            new Product(health.CategoryId, "Carbamide Forte Omega-3 60 Caps",
                "Triple-strength fish oil capsules for heart and brain health",
                399m, $"{ImgBase}omega-3.svg", 60, now),

            new Product(health.CategoryId, "MuscleBlaze Whey Protein 500g",
                "Premium whey isolate blend for muscle gain and recovery",
                999m, $"{ImgBase}whey-protein.svg", 40, now),

            new Product(health.CategoryId, "RiteBite Protein Bar 67g",
                "High-protein nutritional bar with 20g protein per serve",
                65m, $"{ImgBase}protein-bar.svg", 120, now),

            new Product(health.CategoryId, "Revital H Multivitamin 30 Caps",
                "Complete daily multivitamin with 12 vitamins and 8 minerals",
                299m, $"{ImgBase}revital.svg", 80, now),

            new Product(health.CategoryId, "Dettol Hand Sanitiser 200ml",
                "Kills 99.9% germs in 10 seconds, no water needed",
                119m, $"{ImgBase}hand-sanitiser.svg", 150, now),

            new Product(health.CategoryId, "3M N95 Protective Mask 5pcs",
                "FFP2-rated respirator for fine dust and airborne particles",
                299m, $"{ImgBase}n95-mask.svg", 100, now),

            // ── Electronics & Accessories (10) ───────────────────────────────────
            new Product(electronics.CategoryId, "Anker USB-C Cable 1m 65W",
                "Braided fast-charge cable supporting 65W PD, nylon braided",
                699m, $"{ImgBase}usb-c-cable.svg", 80, now),

            new Product(electronics.CategoryId, "boAt Airdopes 141 Earbuds",
                "Bluetooth 5.1 earbuds with 42-hour playtime and IPX4 rating",
                1299m, $"{ImgBase}wireless-earbuds.svg", 50, now),

            new Product(electronics.CategoryId, "Adjustable Phone Stand",
                "Foldable aluminium desktop stand for phones and tablets",
                199m, $"{ImgBase}phone-stand.svg", 100, now),

            new Product(electronics.CategoryId, "Mi Power Bank 3i 10000mAh",
                "Slim dual-port power bank with 18W fast charging",
                799m, $"{ImgBase}power-bank.svg", 40, now),

            new Product(electronics.CategoryId, "Tempered Glass Screen Protector",
                "9H hardness screen protector with oleophobic coating",
                199m, $"{ImgBase}screen-protector.svg", 100, now),

            new Product(electronics.CategoryId, "Spigen Slim Armor Phone Case",
                "Dual-layer protective case with raised edges for camera safety",
                599m, $"{ImgBase}phone-case.svg", 60, now),

            new Product(electronics.CategoryId, "Belkin 20W USB-C PD Adapter",
                "Compact 20W USB-C Power Delivery wall charger for fast charging",
                1499m, $"{ImgBase}usb-adapter.svg", 30, now),

            new Product(electronics.CategoryId, "JBL Go 3 Portable Speaker",
                "Waterproof Bluetooth speaker with bold JBL Pro Sound",
                2499m, $"{ImgBase}bluetooth-speaker.svg", 25, now),

            new Product(electronics.CategoryId, "Cable Management Organiser Box",
                "Desktop cable box to neatly hide power strips and cables",
                349m, $"{ImgBase}cable-box.svg", 50, now),

            new Product(electronics.CategoryId, "Bluetooth Selfie Stick Tripod",
                "Extendable tripod with wireless remote shutter for phones",
                499m, $"{ImgBase}selfie-stick.svg", 60, now),

            // ── Pet Care (6) ─────────────────────────────────────────────────────
            new Product(petCare.CategoryId, "Pedigree Adult Dog Food 1.2kg",
                "Complete and balanced dry dog food for adult dogs",
                465m, $"{ImgBase}pedigree.svg", 50, now),

            new Product(petCare.CategoryId, "Whiskas Adult Cat Food 480g",
                "Tender meat pieces in gravy for adult cats, ocean fish flavour",
                199m, $"{ImgBase}whiskas.svg", 60, now),

            new Product(petCare.CategoryId, "Purina Beggin Dog Treats 65g",
                "Bacon-flavoured soft dog treats for training and rewards",
                225m, $"{ImgBase}dog-treats.svg", 80, now),

            new Product(petCare.CategoryId, "Temptations Cat Treats 60g",
                "Crunchy outside, soft inside cat treats for daily rewarding",
                175m, $"{ImgBase}cat-treats.svg", 80, now),

            new Product(petCare.CategoryId, "Beaphar Dog Shampoo 250ml",
                "Gentle pH-balanced shampoo for shiny and healthy dog coat",
                299m, $"{ImgBase}pet-shampoo.svg", 40, now),

            new Product(petCare.CategoryId, "Fresh Step Cat Litter 3.6kg",
                "Clumping clay litter with activated charcoal for odour control",
                549m, $"{ImgBase}cat-litter.svg", 30, now),

            // ── Bread & Bakery (8) ───────────────────────────────────────────────
            new Product(bakery.CategoryId, "Modern White Bread 400g",
                "Soft and fluffy white bread, perfect for sandwiches and toast",
                35m, $"{ImgBase}white-bread.svg", 200, now),

            new Product(bakery.CategoryId, "Britannia Whole Wheat Bread 400g",
                "100% whole wheat bread with added fibre for healthy eating",
                55m, $"{ImgBase}whole-wheat-bread.svg", 150, now),

            new Product(bakery.CategoryId, "English Oven Burger Buns 4pcs",
                "Soft sesame-topped buns, ideal for home-style burgers",
                65m, $"{ImgBase}burger-buns.svg", 100, now),

            new Product(bakery.CategoryId, "Harvest Gold Sandwich Bread 600g",
                "Extra-large sandwich loaf, stays fresh for longer",
                55m, $"{ImgBase}sandwich-bread.svg", 120, now),

            new Product(bakery.CategoryId, "Bonn Multigrain Bread 400g",
                "Wholesome multigrain bread with seeds for added texture",
                65m, $"{ImgBase}multigrain-bread.svg", 100, now),

            new Product(bakery.CategoryId, "Buttery Croissant 3pcs",
                "Flaky, buttery croissants baked fresh for a continental breakfast",
                95m, $"{ImgBase}croissant.svg", 80, now),

            new Product(bakery.CategoryId, "Treat Choco Muffin 4pcs",
                "Soft chocolate muffins with a gooey centre, great as a snack",
                75m, $"{ImgBase}chocolate-muffin.svg", 100, now),

            new Product(bakery.CategoryId, "Parle Premium Rusk 300g",
                "Crunchy toasted rusk biscuits, a classic accompaniment for tea",
                55m, $"{ImgBase}rusk.svg", 150, now));

        await db.SaveChangesAsync(ct);
    }
}
