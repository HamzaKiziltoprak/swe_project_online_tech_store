using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Backend.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<Role>>();
            var context = services.GetRequiredService<DataContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("--- Seeding Database Started ---");

            // Seed Roles
            string[] roleNames = { "Admin", "Customer", "ProductManager", "CompanyOwner" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    logger.LogInformation($"Creating role {roleName}...");
                    await roleManager.CreateAsync(new Role { Name = roleName });
                }
            }

            // Seed Admin User
            var adminEmail = "admin@example.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                logger.LogInformation("Admin user not found. Creating new admin user...");
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true
                };
                // WARNING: Use a strong password and store it securely
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    logger.LogInformation("Admin user created successfully. Assigning Admin role...");
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    logger.LogError("Failed to create admin user.");
                    foreach(var error in result.Errors)
                    {
                        logger.LogError(error.Description);
                    }
                }
            }
            else
            {
                logger.LogInformation("Admin user found. Checking role assignment...");
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    logger.LogInformation("Admin user is not in Admin role. Assigning role...");
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    logger.LogInformation("Admin user is already in Admin role.");
                }
            }

            // Seed Categories
            await SeedCategoriesAsync(context, logger);

            // Seed Brands
            await SeedBrandsAsync(context, logger);

            // Seed Products
            await SeedProductsAsync(context, logger);

            // Seed Product Specifications
            await SeedProductSpecificationsAsync(context, logger);

            logger.LogInformation("--- Seeding Database Finished ---");
        }

        private static async Task SeedCategoriesAsync(DataContext context, ILogger<Program> logger)
        {
            if (await context.Categories.AnyAsync())
            {
                logger.LogInformation("Categories already exist. Skipping category seeding.");
                return;
            }

            logger.LogInformation("Seeding categories...");

            var categories = new List<Category>
            {
                new Category { CategoryName = "Processors" },
                new Category { CategoryName = "Graphics Cards" },
                new Category { CategoryName = "Motherboards" },
                new Category { CategoryName = "RAM Memory" },
                new Category { CategoryName = "Storage Devices" },
                new Category { CategoryName = "Power Supplies" },
                new Category { CategoryName = "Cooling Systems" },
                new Category { CategoryName = "Cases" }
            };

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
            logger.LogInformation($"Seeded {categories.Count} categories successfully.");
        }

        private static async Task SeedBrandsAsync(DataContext context, ILogger<Program> logger)
        {
            if (await context.Brands.AnyAsync())
            {
                logger.LogInformation("Brands already exist. Skipping brand seeding.");
                return;
            }

            logger.LogInformation("Seeding brands...");

            var brands = new List<Brand>
            {
                new Brand { BrandName = "AMD", Description = "Advanced Micro Devices - Leading processor and graphics card manufacturer", IsActive = true },
                new Brand { BrandName = "Intel", Description = "World's leading semiconductor chip manufacturer", IsActive = true },
                new Brand { BrandName = "NVIDIA", Description = "Leader in visual computing and AI technologies", IsActive = true },
                new Brand { BrandName = "ASUS", Description = "Leading technology company focused on motherboards and components", IsActive = true },
                new Brand { BrandName = "MSI", Description = "Micro-Star International - Gaming hardware manufacturer", IsActive = true },
                new Brand { BrandName = "Corsair", Description = "High-performance gaming peripherals and components", IsActive = true },
                new Brand { BrandName = "G.Skill", Description = "Performance memory modules manufacturer", IsActive = true },
                new Brand { BrandName = "Samsung", Description = "Leading technology conglomerate and storage solutions", IsActive = true },
                new Brand { BrandName = "Western Digital", Description = "WD - Data storage solutions and hard drives", IsActive = true },
                new Brand { BrandName = "EVGA", Description = "Graphics cards and power supply manufacturer", IsActive = true },
                new Brand { BrandName = "Cooler Master", Description = "PC cooling solutions and cases manufacturer", IsActive = true },
                new Brand { BrandName = "NZXT", Description = "Premium PC cases and cooling solutions", IsActive = true },
                new Brand { BrandName = "be quiet!", Description = "German manufacturer of quiet PC components", IsActive = true },
                new Brand { BrandName = "Gigabyte", Description = "Motherboards, graphics cards, and PC components", IsActive = true },
                new Brand { BrandName = "Seagate", Description = "Hard drive and storage solutions manufacturer", IsActive = true }
            };

            context.Brands.AddRange(brands);
            await context.SaveChangesAsync();
            logger.LogInformation($"Seeded {brands.Count} brands successfully.");
        }

        private static async Task SeedProductsAsync(DataContext context, ILogger<Program> logger)
        {
            if (await context.Products.AnyAsync())
            {
                logger.LogInformation("Products already exist. Skipping product seeding.");
                return;
            }

            logger.LogInformation("Seeding products...");

            var categories = await context.Categories.ToListAsync();
            if (categories.Count == 0)
            {
                logger.LogWarning("No categories found. Please seed categories first.");
                return;
            }

            var brands = await context.Brands.ToListAsync();
            if (brands.Count == 0)
            {
                logger.LogWarning("No brands found. Please seed brands first.");
                return;
            }

            var products = new List<Product>
            {
                // Processors
                new Product
                {
                    ProductName = "AMD Ryzen 5 5600X",
                    Description = "6-core, 12-thread processor with 3.7 GHz base clock",
                    Price = 299.99m,
                    Stock = 25,
                    BrandID = brands.FirstOrDefault(b => b.BrandName == "AMD")?.BrandID ?? 1,
                    CategoryID = categories.FirstOrDefault(c => c.CategoryName == "Processors")?.CategoryID ?? 1,
                    ImageUrl = "https://via.placeholder.com/300x300?text=Ryzen+5600X"
                },
                new Product
                {
                    ProductName = "Intel Core i7-12700K",
                    Description = "12-core, 20-thread processor with up to 5.0 GHz",
                    Price = 389.99m,
                    Stock = 18,
                    BrandID = brands.FirstOrDefault(b => b.BrandName == "Intel")?.BrandID ?? 2,
                    CategoryID = categories.FirstOrDefault(c => c.CategoryName == "Processors")?.CategoryID ?? 1,
                    ImageUrl = "https://via.placeholder.com/300x300?text=i7-12700K"
                },

                // Graphics Cards
                new Product
                {
                    ProductName = "NVIDIA RTX 3070 Ti",
                    Description = "8GB GDDR6X, 384-bit memory interface, excellent for 4K gaming",
                    Price = 799.99m,
                    Stock = 12,
                    BrandID = brands.FirstOrDefault(b => b.BrandName == "NVIDIA")?.BrandID ?? 3,
                    CategoryID = categories.FirstOrDefault(c => c.CategoryName == "Graphics Cards")?.CategoryID ?? 2,
                    ImageUrl = "https://via.placeholder.com/300x300?text=RTX+3070+Ti"
                },
                new Product
                {
                    ProductName = "AMD Radeon RX 6800 XT",
                    Description = "16GB GDDR6, 256-bit memory, high performance RDNA2 architecture",
                    Price = 649.99m,
                    Stock = 15,
                    BrandID = brands.FirstOrDefault(b => b.BrandName == "AMD")?.BrandID ?? 1,
                    CategoryID = categories.FirstOrDefault(c => c.CategoryName == "Graphics Cards")?.CategoryID ?? 2,
                    ImageUrl = "https://via.placeholder.com/300x300?text=RX+6800+XT"
                },

                // Motherboards
                new Product
                {
                    ProductName = "ASUS ROG Strix B550-F",
                    Description = "Socket AM4, PCIe 4.0, WiFi 6, Premium features",
                    Price = 199.99m,
                    Stock = 20,
                    BrandID = brands.FirstOrDefault(b => b.BrandName == "ASUS")?.BrandID ?? 4,
                    CategoryID = categories.FirstOrDefault(c => c.CategoryName == "Motherboards")?.CategoryID ?? 3,
                    ImageUrl = "https://via.placeholder.com/300x300?text=ROG+Strix+B550"
                },
                new Product
                {
                    ProductName = "MSI MPG Z690 Edge",
                    Description = "LGA1700 Socket, DDR5 Ready, PCIe 5.0",
                    Price = 269.99m,
                    Stock = 16,
                    BrandID = brands.FirstOrDefault(b => b.BrandName == "MSI")?.BrandID ?? 5,
                    CategoryID = categories.FirstOrDefault(c => c.CategoryName == "Motherboards")?.CategoryID ?? 3,
                    ImageUrl = "https://via.placeholder.com/300x300?text=MPG+Z690+Edge"
                },

                // RAM Memory
                new Product
                {
                    ProductName = "Corsair Vengeance RGB Pro 16GB (2x8GB)",
                    Description = "DDR4, 3600MHz, RGB Lighting, CAS Latency 18",
                    Price = 79.99m,
                    Stock = 30,
                    BrandID = brands.FirstOrDefault(b => b.BrandName == "Corsair")?.BrandID ?? 6,
                    CategoryID = categories.FirstOrDefault(c => c.CategoryName == "RAM Memory")?.CategoryID ?? 4,
                    ImageUrl = "https://via.placeholder.com/300x300?text=Vengeance+RGB+Pro"
                },
                new Product
                {
                    ProductName = "G.Skill Trident Z5 32GB (2x16GB)",
                    Description = "DDR5, 6000MHz, Gaming Performance, EXPO",
                    Price = 199.99m,
                    Stock = 22,
                    BrandID = brands.FirstOrDefault(b => b.BrandName == "G.Skill")?.BrandID ?? 7,
                    CategoryID = categories.FirstOrDefault(c => c.CategoryName == "RAM Memory")?.CategoryID ?? 4,
                    ImageUrl = "https://via.placeholder.com/300x300?text=Trident+Z5"
                },

                // Storage
                new Product
                {
                    ProductName = "Samsung 970 EVO Plus 1TB",
                    Description = "NVMe M.2 SSD, PCIe 4.0, Read speed up to 4500MB/s",
                    Price = 119.99m,
                    Stock = 35,
                    BrandID = brands.FirstOrDefault(b => b.BrandName == "Samsung")?.BrandID ?? 8,
                    CategoryID = categories.FirstOrDefault(c => c.CategoryName == "Storage Devices")?.CategoryID ?? 5,
                    ImageUrl = "https://via.placeholder.com/300x300?text=970+EVO+Plus"
                },
                new Product
                {
                    ProductName = "WD Blue 4TB HDD",
                    Description = "3.5inch, 5400 RPM, Reliable storage solution",
                    Price = 89.99m,
                    Stock = 28,
                    BrandID = brands.FirstOrDefault(b => b.BrandName == "Western Digital")?.BrandID ?? 9,
                    CategoryID = categories.FirstOrDefault(c => c.CategoryName == "Storage Devices")?.CategoryID ?? 5,
                    ImageUrl = "https://via.placeholder.com/300x300?text=WD+Blue+4TB"
                },

                // Power Supplies
                new Product
                {
                    ProductName = "Corsair RM850x",
                    Description = "850W, 80+ Gold Certified, Modular Cables",
                    Price = 139.99m,
                    Stock = 18,
                    BrandID = brands.FirstOrDefault(b => b.BrandName == "Corsair")?.BrandID ?? 6,
                    CategoryID = categories.FirstOrDefault(c => c.CategoryName == "Power Supplies")?.CategoryID ?? 6,
                    ImageUrl = "https://via.placeholder.com/300x300?text=RM850x"
                },
                new Product
                {
                    ProductName = "EVGA SuperNOVA 750 G5",
                    Description = "750W, 80+ Gold, ATX 3.0 Ready, Compact",
                    Price = 129.99m,
                    Stock = 21,
                    BrandID = brands.FirstOrDefault(b => b.BrandName == "EVGA")?.BrandID ?? 10,
                    CategoryID = categories.FirstOrDefault(c => c.CategoryName == "Power Supplies")?.CategoryID ?? 6,
                    ImageUrl = "https://via.placeholder.com/300x300?text=SuperNOVA+750"
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
            logger.LogInformation($"Seeded {products.Count} products successfully.");
        }

        private static async Task SeedProductSpecificationsAsync(DataContext context, ILogger<Program> logger)
        {
            if (await context.ProductSpecifications.AnyAsync())
            {
                logger.LogInformation("Product specifications already exist. Skipping specification seeding.");
                return;
            }

            logger.LogInformation("Seeding product specifications...");

            var products = await context.Products.ToListAsync();
            if (products.Count == 0)
            {
                logger.LogWarning("No products found. Please seed products first.");
                return;
            }

            var specifications = new List<ProductSpecification>();

            // AMD Ryzen 5 5600X specifications
            var ryzen5 = products.FirstOrDefault(p => p.ProductName == "AMD Ryzen 5 5600X");
            if (ryzen5 != null)
            {
                specifications.AddRange(new List<ProductSpecification>
                {
                    new ProductSpecification { ProductID = ryzen5.ProductID, SpecName = "Cores", SpecValue = "6" },
                    new ProductSpecification { ProductID = ryzen5.ProductID, SpecName = "Threads", SpecValue = "12" },
                    new ProductSpecification { ProductID = ryzen5.ProductID, SpecName = "Base Clock", SpecValue = "3.7 GHz" },
                    new ProductSpecification { ProductID = ryzen5.ProductID, SpecName = "Boost Clock", SpecValue = "4.6 GHz" },
                    new ProductSpecification { ProductID = ryzen5.ProductID, SpecName = "TDP", SpecValue = "65W" },
                    new ProductSpecification { ProductID = ryzen5.ProductID, SpecName = "Architecture", SpecValue = "Zen 3" },
                    new ProductSpecification { ProductID = ryzen5.ProductID, SpecName = "Socket", SpecValue = "AM4" }
                });
            }

            // Intel Core i7-12700K specifications
            var intel12700k = products.FirstOrDefault(p => p.ProductName == "Intel Core i7-12700K");
            if (intel12700k != null)
            {
                specifications.AddRange(new List<ProductSpecification>
                {
                    new ProductSpecification { ProductID = intel12700k.ProductID, SpecName = "Cores", SpecValue = "12" },
                    new ProductSpecification { ProductID = intel12700k.ProductID, SpecName = "Threads", SpecValue = "20" },
                    new ProductSpecification { ProductID = intel12700k.ProductID, SpecName = "Base Clock", SpecValue = "3.6 GHz" },
                    new ProductSpecification { ProductID = intel12700k.ProductID, SpecName = "Boost Clock", SpecValue = "5.0 GHz" },
                    new ProductSpecification { ProductID = intel12700k.ProductID, SpecName = "TDP", SpecValue = "125W" },
                    new ProductSpecification { ProductID = intel12700k.ProductID, SpecName = "Architecture", SpecValue = "Alder Lake" },
                    new ProductSpecification { ProductID = intel12700k.ProductID, SpecName = "Socket", SpecValue = "LGA1700" }
                });
            }

            // RTX 3070 Ti specifications
            var rtx3070ti = products.FirstOrDefault(p => p.ProductName == "NVIDIA RTX 3070 Ti");
            if (rtx3070ti != null)
            {
                specifications.AddRange(new List<ProductSpecification>
                {
                    new ProductSpecification { ProductID = rtx3070ti.ProductID, SpecName = "VRAM", SpecValue = "8GB GDDR6X" },
                    new ProductSpecification { ProductID = rtx3070ti.ProductID, SpecName = "Memory Interface", SpecValue = "256-bit" },
                    new ProductSpecification { ProductID = rtx3070ti.ProductID, SpecName = "CUDA Cores", SpecValue = "2432" },
                    new ProductSpecification { ProductID = rtx3070ti.ProductID, SpecName = "Boost Clock", SpecValue = "2.67 GHz" },
                    new ProductSpecification { ProductID = rtx3070ti.ProductID, SpecName = "TDP", SpecValue = "290W" },
                    new ProductSpecification { ProductID = rtx3070ti.ProductID, SpecName = "Architecture", SpecValue = "Ampere" },
                    new ProductSpecification { ProductID = rtx3070ti.ProductID, SpecName = "DirectX", SpecValue = "12 Ultimate" }
                });
            }

            // RTX 3060 Ti specifications
            var rtx3060ti = products.FirstOrDefault(p => p.ProductName == "NVIDIA RTX 3060 Ti");
            if (rtx3060ti != null)
            {
                specifications.AddRange(new List<ProductSpecification>
                {
                    new ProductSpecification { ProductID = rtx3060ti.ProductID, SpecName = "VRAM", SpecValue = "8GB GDDR6" },
                    new ProductSpecification { ProductID = rtx3060ti.ProductID, SpecName = "Memory Interface", SpecValue = "256-bit" },
                    new ProductSpecification { ProductID = rtx3060ti.ProductID, SpecName = "CUDA Cores", SpecValue = "1920" },
                    new ProductSpecification { ProductID = rtx3060ti.ProductID, SpecName = "Boost Clock", SpecValue = "2.67 GHz" },
                    new ProductSpecification { ProductID = rtx3060ti.ProductID, SpecName = "TDP", SpecValue = "200W" },
                    new ProductSpecification { ProductID = rtx3060ti.ProductID, SpecName = "Architecture", SpecValue = "Ampere" },
                    new ProductSpecification { ProductID = rtx3060ti.ProductID, SpecName = "DirectX", SpecValue = "12 Ultimate" }
                });
            }

            // ASUS ROG STRIX Z690-E specifications
            var z690e = products.FirstOrDefault(p => p.ProductName == "ASUS ROG STRIX Z690-E");
            if (z690e != null)
            {
                specifications.AddRange(new List<ProductSpecification>
                {
                    new ProductSpecification { ProductID = z690e.ProductID, SpecName = "Socket", SpecValue = "LGA1700" },
                    new ProductSpecification { ProductID = z690e.ProductID, SpecName = "Form Factor", SpecValue = "ATX" },
                    new ProductSpecification { ProductID = z690e.ProductID, SpecName = "RAM Slots", SpecValue = "4 (DDR5)" },
                    new ProductSpecification { ProductID = z690e.ProductID, SpecName = "Max RAM", SpecValue = "192GB" },
                    new ProductSpecification { ProductID = z690e.ProductID, SpecName = "PCIe Slots", SpecValue = "3 x M.2" },
                    new ProductSpecification { ProductID = z690e.ProductID, SpecName = "Chipset", SpecValue = "Intel Z690" },
                    new ProductSpecification { ProductID = z690e.ProductID, SpecName = "Wi-Fi", SpecValue = "6E" }
                });
            }

            // G.SKILL Trident Z5 specifications
            var tridentz5 = products.FirstOrDefault(p => p.ProductName == "G.SKILL Trident Z5 DDR5");
            if (tridentz5 != null)
            {
                specifications.AddRange(new List<ProductSpecification>
                {
                    new ProductSpecification { ProductID = tridentz5.ProductID, SpecName = "Capacity", SpecValue = "32GB (16GBx2)" },
                    new ProductSpecification { ProductID = tridentz5.ProductID, SpecName = "Type", SpecValue = "DDR5" },
                    new ProductSpecification { ProductID = tridentz5.ProductID, SpecName = "Speed", SpecValue = "6000MHz" },
                    new ProductSpecification { ProductID = tridentz5.ProductID, SpecName = "CAS Latency", SpecValue = "30" },
                    new ProductSpecification { ProductID = tridentz5.ProductID, SpecName = "Voltage", SpecValue = "1.25V" },
                    new ProductSpecification { ProductID = tridentz5.ProductID, SpecName = "Form Factor", SpecValue = "DIMM" }
                });
            }

            // Samsung 980 Pro specifications
            var samsung980pro = products.FirstOrDefault(p => p.ProductName == "Samsung 980 Pro NVMe SSD");
            if (samsung980pro != null)
            {
                specifications.AddRange(new List<ProductSpecification>
                {
                    new ProductSpecification { ProductID = samsung980pro.ProductID, SpecName = "Capacity", SpecValue = "1TB" },
                    new ProductSpecification { ProductID = samsung980pro.ProductID, SpecName = "Interface", SpecValue = "NVMe M.2" },
                    new ProductSpecification { ProductID = samsung980pro.ProductID, SpecName = "Type", SpecValue = "SSD" },
                    new ProductSpecification { ProductID = samsung980pro.ProductID, SpecName = "Read Speed", SpecValue = "7100 MB/s" },
                    new ProductSpecification { ProductID = samsung980pro.ProductID, SpecName = "Write Speed", SpecValue = "6000 MB/s" },
                    new ProductSpecification { ProductID = samsung980pro.ProductID, SpecName = "Form Factor", SpecValue = "M.2 2280" }
                });
            }

            // Seagate Barracuda specifications
            var barracuda = products.FirstOrDefault(p => p.ProductName == "Seagate Barracuda 4TB");
            if (barracuda != null)
            {
                specifications.AddRange(new List<ProductSpecification>
                {
                    new ProductSpecification { ProductID = barracuda.ProductID, SpecName = "Capacity", SpecValue = "4TB" },
                    new ProductSpecification { ProductID = barracuda.ProductID, SpecName = "Interface", SpecValue = "SATA III" },
                    new ProductSpecification { ProductID = barracuda.ProductID, SpecName = "Type", SpecValue = "HDD" },
                    new ProductSpecification { ProductID = barracuda.ProductID, SpecName = "RPM", SpecValue = "5400" },
                    new ProductSpecification { ProductID = barracuda.ProductID, SpecName = "Cache", SpecValue = "256MB" },
                    new ProductSpecification { ProductID = barracuda.ProductID, SpecName = "Form Factor", SpecValue = "3.5 inch" }
                });
            }

            // Corsair RM850x specifications
            var rm850x = products.FirstOrDefault(p => p.ProductName == "Corsair RM850x");
            if (rm850x != null)
            {
                specifications.AddRange(new List<ProductSpecification>
                {
                    new ProductSpecification { ProductID = rm850x.ProductID, SpecName = "Wattage", SpecValue = "850W" },
                    new ProductSpecification { ProductID = rm850x.ProductID, SpecName = "Efficiency", SpecValue = "80+ Gold" },
                    new ProductSpecification { ProductID = rm850x.ProductID, SpecName = "Type", SpecValue = "Fully Modular" },
                    new ProductSpecification { ProductID = rm850x.ProductID, SpecName = "Cables", SpecValue = "Modular" },
                    new ProductSpecification { ProductID = rm850x.ProductID, SpecName = "Fan", SpecValue = "120mm Fluid Dynamic" },
                    new ProductSpecification { ProductID = rm850x.ProductID, SpecName = "Warranty", SpecValue = "10 Years" }
                });
            }

            // EVGA SuperNOVA specifications
            var supernova = products.FirstOrDefault(p => p.ProductName == "EVGA SuperNOVA 750 G5");
            if (supernova != null)
            {
                specifications.AddRange(new List<ProductSpecification>
                {
                    new ProductSpecification { ProductID = supernova.ProductID, SpecName = "Wattage", SpecValue = "750W" },
                    new ProductSpecification { ProductID = supernova.ProductID, SpecName = "Efficiency", SpecValue = "80+ Gold" },
                    new ProductSpecification { ProductID = supernova.ProductID, SpecName = "Type", SpecValue = "Fully Modular" },
                    new ProductSpecification { ProductID = supernova.ProductID, SpecName = "Cables", SpecValue = "Modular" },
                    new ProductSpecification { ProductID = supernova.ProductID, SpecName = "Fan", SpecValue = "120mm" },
                    new ProductSpecification { ProductID = supernova.ProductID, SpecName = "Warranty", SpecValue = "10 Years" }
                });
            }

            if (specifications.Count > 0)
            {
                context.ProductSpecifications.AddRange(specifications);
                await context.SaveChangesAsync();
                logger.LogInformation($"Seeded {specifications.Count} product specifications successfully.");
            }
            else
            {
                logger.LogWarning("No specifications were created. Check if products exist.");
            }
        }
    }
}
