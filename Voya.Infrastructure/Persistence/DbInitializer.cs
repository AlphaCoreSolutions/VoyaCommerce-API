using Voya.Core.Entities;
using Voya.Core.Enums; // Required for StoreStatus

namespace Voya.Infrastructure.Persistence;

public static class DbInitializer
{
	public static void Seed(VoyaDbContext context)
	{
		// 1. Ensure Database is created
		context.Database.EnsureCreated();

		// 2. Check if we already have data

		if (context.Products.Any())
		{
			return; // DB has been seeded
		}

		// ==========================================
		// 3. CREATE CATEGORIES (Main -> Sub -> Sub-Sub)
		// ==========================================

		// --- Main Categories ---
		var electronics = new Category { Name = "Electronics", IconUrl = "https://img.icons8.com/ios/50/electronics.png", ColorHex = "#007AFF" };
		var fashion = new Category { Name = "Fashion", IconUrl = "https://img.icons8.com/ios/50/clothes.png", ColorHex = "#FF2D55" };
		var home = new Category { Name = "Home & Living", IconUrl = "https://img.icons8.com/ios/50/living-room.png", ColorHex = "#FF9500" };
		var sports = new Category { Name = "Sports", IconUrl = "https://img.icons8.com/ios/50/dumbbell.png", ColorHex = "#4CD964" };

		// Check if categories exist to prevent duplicates if seed re-runs partially
		if (!context.Categories.Any())
		{
			context.Categories.AddRange(electronics, fashion, home, sports);
			context.SaveChanges();

			// --- Electronics Sub-Categories ---
			var phones = new Category { Name = "Smartphones", ParentId = electronics.Id };
			var laptops = new Category { Name = "Laptops", ParentId = electronics.Id };
			var audio = new Category { Name = "Audio", ParentId = electronics.Id };
			var gaming = new Category { Name = "Gaming", ParentId = electronics.Id };

			// --- Fashion Sub-Categories ---
			var men = new Category { Name = "Men", ParentId = fashion.Id };
			var women = new Category { Name = "Women", ParentId = fashion.Id };
			var accessories = new Category { Name = "Accessories", ParentId = fashion.Id };

			// --- Home Sub-Categories ---
			var furniture = new Category { Name = "Furniture", ParentId = home.Id };
			var decor = new Category { Name = "Decor", ParentId = home.Id };

			context.Categories.AddRange(phones, laptops, audio, gaming, men, women, accessories, furniture, decor);
			context.SaveChanges();

			// --- Sub-Sub Categories (Deep Hierarchy) ---
			var gamingLaptops = new Category { Name = "Gaming Laptops", ParentId = laptops.Id };
			var ultrabooks = new Category { Name = "Ultrabooks", ParentId = laptops.Id };
			var sneakers = new Category { Name = "Sneakers", ParentId = men.Id };
			var dresses = new Category { Name = "Dresses", ParentId = women.Id };

			context.Categories.AddRange(gamingLaptops, ultrabooks, sneakers, dresses);
			context.SaveChanges();
		}

		// Re-fetch categories to ensure we have IDs if they already existed
		var phonesCat = context.Categories.First(c => c.Name == "Smartphones");
		var laptopsCat = context.Categories.First(c => c.Name == "Laptops");
		var audioCat = context.Categories.First(c => c.Name == "Audio");
		var gamingCat = context.Categories.First(c => c.Name == "Gaming");
		var menCat = context.Categories.First(c => c.Name == "Men");
		var womenCat = context.Categories.First(c => c.Name == "Women");
		var accessoriesCat = context.Categories.First(c => c.Name == "Accessories");
		var furnitureCat = context.Categories.First(c => c.Name == "Furniture");
		var decorCat = context.Categories.First(c => c.Name == "Decor");
		var sportsCat = context.Categories.First(c => c.Name == "Sports");
		var gamingLaptopsCat = context.Categories.First(c => c.Name == "Gaming Laptops");
		var ultrabooksCat = context.Categories.First(c => c.Name == "Ultrabooks");
		var sneakersCat = context.Categories.First(c => c.Name == "Sneakers");
		var dressesCat = context.Categories.First(c => c.Name == "Dresses");

		// ==========================================
		// 4. CREATE STORE OWNER & STORE (Fixes Foreign Key Error)
		// ==========================================

		var sellerEmail = "official@voya.com";
		var sellerUser = context.Users.FirstOrDefault(u => u.Email == sellerEmail);

		if (sellerUser == null)
		{
			sellerUser = new User
			{
				Email = sellerEmail,
				FullName = "Voya Official",
				PasswordHash = "seeded_hash", // In real app, use hasher
				PhoneNumber = "+1234567890",
				IsGoldMember = true,
				CreatedAt = DateTime.UtcNow
			};
			context.Users.Add(sellerUser);
			context.SaveChanges();
		}

		var storeName = "Voya Official Store";
		var officialStore = context.Stores.FirstOrDefault(s => s.Name == storeName);

		if (officialStore == null)
		{
			officialStore = new Store
			{
				OwnerId = sellerUser.Id,
				Name = storeName,
				Description = "The official flagship store for Voya Commerce.",
				BusinessEmail = "business@voya.com",
				Status = StoreStatus.Active, // Auto-approve for seed
				Rating = 5.0,
				CreatedAt = DateTime.UtcNow
			};
			context.Stores.Add(officialStore);
			context.SaveChanges();
		}

		var storeId = officialStore.Id; // <--- REAL ID FROM DB

		// ==========================================
		// 5. CREATE PRODUCTS
		// ==========================================

		var products = new List<Product>();

		// --- ELECTRONICS: PHONES ---
		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = phonesCat.Id,
			Name = "iPhone 15 Pro Max",
			Description = "Titanium design, A17 Pro chip, and the most powerful iPhone camera system ever.",
			BasePrice = 1199.99m,
			DiscountPrice = 1099.99m,
			StockQuantity = 50,
			MainImageUrl = "https://images.unsplash.com/photo-1696446701796-da61225697cc?auto=format&fit=crop&w=800&q=80",
			GalleryImages = new List<string> { "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=800&q=80" },
			Tags = new List<string> { "iphone", "apple", "smartphone", "titanium", "5g" },
			Options = new List<ProductOption> {
				CreateOption("Storage", "256GB", 0, "512GB", 100),
				CreateOption("Color", "Natural Titanium", 0, "Blue Titanium", 0)
			}
		});

		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = phonesCat.Id,
			Name = "Samsung Galaxy S24 Ultra",
			Description = "AI-powered flagship with S-Pen support and 200MP camera.",
			BasePrice = 1299.99m,
			StockQuantity = 30,
			MainImageUrl = "https://images.unsplash.com/photo-1610945415295-d9bbf067e59c?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "samsung", "android", "galaxy", "ai", "stylus" },
			Options = new List<ProductOption> { CreateOption("Color", "Titanium Gray", 0, "Violet", 0) }
		});

		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = phonesCat.Id,
			Name = "Google Pixel 8 Pro",
			Description = "The AI phone from Google with advanced photo editing magic.",
			BasePrice = 999.00m,
			DiscountPrice = 799.00m, // Big Flash Sale
			StockQuantity = 15,
			MainImageUrl = "https://images.unsplash.com/photo-1598327105666-5b89351aff5b?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "google", "pixel", "android", "camera" }
		});

		// --- ELECTRONICS: LAPTOPS ---
		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = gamingLaptopsCat.Id,
			Name = "Alienware x16 Gaming Laptop",
			Description = "16-inch beast with RTX 4090 and 480Hz display.",
			BasePrice = 3299.99m,
			StockQuantity = 5,
			MainImageUrl = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "laptop", "gaming", "alienware", "rtx", "nvidia" },
			Options = new List<ProductOption> { CreateOption("RAM", "32GB", 0, "64GB", 200) }
		});

		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = ultrabooksCat.Id,
			Name = "MacBook Air M3",
			Description = "Supercharged by M3. Impossibly thin and light.",
			BasePrice = 1099.00m,
			StockQuantity = 100,
			MainImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca4?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "apple", "macbook", "laptop", "m3", "ultrabook" },
			Options = new List<ProductOption> { CreateOption("Color", "Midnight", 0, "Starlight", 0) }
		});

		// --- ELECTRONICS: AUDIO ---
		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = audioCat.Id,
			Name = "Sony WH-1000XM5",
			Description = "Industry-leading noise canceling headphones.",
			BasePrice = 349.99m,
			StockQuantity = 40,
			MainImageUrl = "https://images.unsplash.com/photo-1618366712010-f4ae9c647dcb?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "headphones", "sony", "audio", "noise cancelling", "wireless" }
		});

		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = audioCat.Id,
			Name = "AirPods Pro (2nd Gen)",
			Description = "Rich audio with 2x more Active Noise Cancellation.",
			BasePrice = 249.00m,
			DiscountPrice = 199.00m,
			StockQuantity = 200,
			MainImageUrl = "https://images.unsplash.com/photo-1603351154351-5cf99bc32f2d?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "apple", "airpods", "earbuds", "wireless" }
		});

		// --- ELECTRONICS: GAMING ---
		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = gamingCat.Id,
			Name = "PlayStation 5 Slim",
			Description = "Play Has No Limits. Breathtaking immersion.",
			BasePrice = 499.99m,
			StockQuantity = 12,
			MainImageUrl = "https://images.unsplash.com/photo-1606813907291-d86efa9b94db?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "gaming", "console", "ps5", "sony" }
		});

		// --- FASHION: MEN ---
		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = menCat.Id,
			Name = "Vintage Oversized T-Shirt",
			Description = "100% Cotton, heavy weight fabric. Perfect for streetwear.",
			BasePrice = 35.00m,
			StockQuantity = 100,
			MainImageUrl = "https://images.unsplash.com/photo-1583743814966-8936f5b7be1a?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "tshirt", "fashion", "men", "black", "streetwear" },
			Options = new List<ProductOption> { CreateOption("Size", "M", 0, "L", 0, "XL", 2) }
		});

		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = sneakersCat.Id,
			Name = "Air Jordan 1 High OG",
			Description = "The classic that started it all.",
			BasePrice = 180.00m,
			StockQuantity = 8,
			MainImageUrl = "https://images.unsplash.com/photo-1552346154-21d32810aba3?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "shoes", "sneakers", "nike", "jordan", "fashion" },
			Options = new List<ProductOption> { CreateOption("Size", "US 9", 0, "US 10", 0, "US 11", 0) }
		});

		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = menCat.Id,
			Name = "Denim Jacket",
			Description = "Classic blue denim jacket with sherpa lining.",
			BasePrice = 89.99m,
			StockQuantity = 25,
			MainImageUrl = "https://images.unsplash.com/photo-1576871337632-b9aef4c17ab9?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "jacket", "denim", "blue", "winter", "coat" }
		});

		// --- FASHION: WOMEN ---
		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = dressesCat.Id,
			Name = "Floral Summer Dress",
			Description = "Lightweight and breathable. Ideal for beach days.",
			BasePrice = 49.99m,
			DiscountPrice = 29.99m,
			StockQuantity = 40,
			MainImageUrl = "https://images.unsplash.com/photo-1572804013309-59a88b7e92f1?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "dress", "summer", "floral", "women", "beach" },
			Options = new List<ProductOption> { CreateOption("Size", "S", 0, "M", 0, "L", 0) }
		});

		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = accessoriesCat.Id,
			Name = "Leather Crossbody Bag",
			Description = "Genuine leather bag for everyday essentials.",
			BasePrice = 120.00m,
			StockQuantity = 15,
			MainImageUrl = "https://images.unsplash.com/photo-1548036328-c9fa89d128fa?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "bag", "accessory", "leather", "fashion", "purse" }
		});

		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = womenCat.Id,
			Name = "High-Waist Yoga Leggings",
			Description = "Buttery soft fabric for yoga and running.",
			BasePrice = 45.00m,
			StockQuantity = 60,
			MainImageUrl = "https://images.unsplash.com/photo-1506619216599-9d16d0903dfd?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "leggings", "yoga", "gym", "activewear", "pants" }
		});

		// --- HOME & LIVING ---
		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = furnitureCat.Id,
			Name = "Mid-Century Modern Sofa",
			Description = "Velvet 3-seater sofa in emerald green.",
			BasePrice = 899.00m,
			StockQuantity = 3,
			MainImageUrl = "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "sofa", "furniture", "living room", "green", "velvet" }
		});

		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = decorCat.Id,
			Name = "Minimalist Table Lamp",
			Description = "Warm light for your reading nook.",
			BasePrice = 39.99m,
			StockQuantity = 100,
			MainImageUrl = "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "lamp", "light", "decor", "home" }
		});

		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = decorCat.Id,
			Name = "Ceramic Plant Pot Set",
			Description = "Set of 3 white ceramic pots for indoor plants.",
			BasePrice = 25.00m,
			StockQuantity = 50,
			MainImageUrl = "https://images.unsplash.com/photo-1485955900006-10f4d324d411?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "plant", "pot", "garden", "decor", "white" }
		});

		// --- SPORTS ---
		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = sportsCat.Id,
			Name = "Adjustable Dumbbell Set",
			Description = "5-50 lbs per hand. Space saving design.",
			BasePrice = 299.00m,
			StockQuantity = 20,
			MainImageUrl = "https://images.unsplash.com/photo-1638536532686-d610adfc8e5c?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "gym", "weights", "fitness", "home gym" }
		});

		products.Add(new Product
		{
			StoreId = storeId,
			CategoryId = sportsCat.Id,
			Name = "Yoga Mat - Non Slip",
			Description = "Eco-friendly material with alignment lines.",
			BasePrice = 29.99m,
			StockQuantity = 80,
			MainImageUrl = "https://images.unsplash.com/photo-1601925260368-ae2f83cf8b7f?auto=format&fit=crop&w=800&q=80",
			Tags = new List<string> { "yoga", "mat", "exercise", "pilates" }
		});

		// ... Add 30 more products ...
		for (int i = 1; i <= 30; i++)
		{
			var isTech = i % 2 == 0;
			products.Add(new Product
			{
				StoreId = storeId,
				CategoryId = isTech ? accessoriesCat.Id : decorCat.Id,
				Name = isTech ? $"Tech Gadget #{i}" : $"Home Decor Item #{i}",
				Description = "A wonderful addition to your collection.",
				BasePrice = 10.00m + i,
				StockQuantity = 50,
				MainImageUrl = isTech
					? "https://images.unsplash.com/photo-1519389950473-47ba0277781c?auto=format&fit=crop&w=800&q=80" // Tech
					: "https://images.unsplash.com/photo-1584622050111-993a426fbf0a?auto=format&fit=crop&w=800&q=80", // Decor
				Tags = new List<string> { "generic", isTech ? "tech" : "home", "sale" }
			});
		}

		// ==========================================
		// 6. CREATE VOUCHERS
		// ==========================================
		if (!context.Vouchers.Any())
		{
			context.Vouchers.AddRange(
				new Voucher
				{
					Code = "WELCOME20",
					Description = "$20 Off your first order",
					Type = DiscountType.FixedAmount,
					Value = 20.00m,
					StartDate = DateTime.UtcNow,
					EndDate = DateTime.UtcNow.AddYears(1),
					MaxUsesPerUser = 1
				},
				new Voucher
				{
					Code = "SUMMER10",
					Description = "10% Off Summer Sale",
					Type = DiscountType.Percentage,
					Value = 10.00m,
					StartDate = DateTime.UtcNow,
					EndDate = DateTime.UtcNow.AddMonths(3),
					MaxUsesPerUser = 10
				},
				new Voucher
				{
					Code = "MYSTERY20",
					Description = "20% Off Mystery Reward",
					Type = DiscountType.Percentage,
					Value = 20.00m,
					StartDate = DateTime.UtcNow,
					EndDate = DateTime.UtcNow.AddDays(1),
					MaxUsesPerUser = 1
				}
			);
			context.SaveChanges();
		}

		context.Products.AddRange(products);
		context.SaveChanges();
	}

	// --- HELPER FOR CLEANER OPTION CREATION ---
	private static ProductOption CreateOption(string name, string v1, decimal p1, string? v2 = null, decimal p2 = 0, string? v3 = null, decimal p3 = 0)
	{
		var values = new List<ProductOptionValue>
		{
            // Id is generated automatically by Guid.NewGuid() in the Entity constructor
            new ProductOptionValue { Label = v1, PriceModifier = p1 }
		};

		if (v2 != null)
			values.Add(new ProductOptionValue { Label = v2, PriceModifier = p2 });

		if (v3 != null)
			values.Add(new ProductOptionValue { Label = v3, PriceModifier = p3 });

		return new ProductOption
		{
			Name = name,
			Values = values
		};
	}
}