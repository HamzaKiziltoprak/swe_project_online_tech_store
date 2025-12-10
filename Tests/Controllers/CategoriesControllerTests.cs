using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Controllers
{
    public class CategoriesControllerTests
    {
        private readonly DataContext _context;
        private readonly Mock<ILogger<CategoriesController>> _mockLogger;
        private readonly CategoriesController _controller;

        public CategoriesControllerTests()
        {
            // Testler için geçici ve izole bir veritabanı oluşturuluyor
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;
            _context = new DataContext(options);

            // Controller'ın logger bağımlılığı için mock nesne oluşturuluyor
            _mockLogger = new Mock<ILogger<CategoriesController>>();

            // Controller test ortamında gerçek DB yerine InMemory DB ve mock logger ile çalışıyor
            _controller = new CategoriesController(
                _context,
                _mockLogger.Object
            );
        }

        // Kategorilerin hiyerarşik yapıda doğru döndüğü senaryo test ediliyor
        [Fact]
        public async Task GetCategories_ShouldReturnRootCategoriesWithSubCategories()
        {
            // Test verisi: iki ana kategori, bir alt kategori
            var root1 = new Category { CategoryID = 1, CategoryName = "Electronics", ParentCategoryID = null };
            var root2 = new Category { CategoryID = 2, CategoryName = "Clothing", ParentCategoryID = null };
            var sub1 = new Category { CategoryID = 3, CategoryName = "Laptops", ParentCategoryID = 1 };

            _context.Categories.Add(root1);
            _context.Categories.Add(root2);
            _context.Categories.Add(sub1);
            await _context.SaveChangesAsync();

            // Controller çağrılıyor
            var result = await _controller.GetCategories();

            // Dönen cevabın türü kontrol ediliyor
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<CategoryDetailDto>>>(actionResult.Value);

            // Sadece üst seviye kategoriler dönmeli
            Assert.Equal(2, apiResponse.Data.Count);

            // Elektronik kategorisinin alt kategorisi doğru geliyor mu
            var electronics = apiResponse.Data.FirstOrDefault(c => c.CategoryName == "Electronics");
            Assert.NotNull(electronics);
            Assert.Single(electronics.SubCategories);
            Assert.Equal("Laptops", electronics.SubCategories[0].CategoryName);
        }

        // ID üzerinden kategori bulunduğu zaman doğru döndüğünü test ediyoruz
        [Fact]
        public async Task GetCategoryById_ShouldReturnCategory_WhenExists()
        {
            // Test verisi: bir parent kategori ve ona bağlı bir child kategori
            var parent = new Category { CategoryID = 10, CategoryName = "Parent" };
            var child = new Category { CategoryID = 11, CategoryName = "Child", ParentCategoryID = 10 };

            _context.Categories.AddRange(parent, child);
            await _context.SaveChangesAsync();

            // Controller çağrısı
            var result = await _controller.GetCategoryById(11);

            // Dönen response tipi kontrol ediliyor
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CategoryDetailDto>>(actionResult.Value);

            // Bilgiler doğru yansıtılıyor mu
            Assert.Equal("Child", apiResponse.Data.CategoryName);
            Assert.Equal("Parent", apiResponse.Data.ParentCategoryName);
        }

        // ID olmayan bir kategori istenirse NotFound dönmesi bekleniyor
        [Fact]
        public async Task GetCategoryById_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            var result = await _controller.GetCategoryById(99);
            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        // Yeni kategori başarıyla yaratılıyorsa CreatedAtAction dönmeli
        [Fact]
        public async Task CreateCategory_ShouldReturnCreated_WhenValid()
        {
            var dto = new CreateCategoryDto { CategoryName = "New Cat" };

            var result = await _controller.CreateCategory(dto);

            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CategoryDetailDto>>(actionResult.Value);

            // API doğru isimle kategori oluşturmuş mu
            Assert.Equal("New Cat", apiResponse.Data.CategoryName);

            // Veritabanında gerçekten oluşturulup oluşturulmadığı kontrol ediliyor
            var dbCat = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "New Cat");
            Assert.NotNull(dbCat);
        }

        // Zaten var olan kategori adı gönderildiğinde hata dönmesi bekleniyor
        [Fact]
        public async Task CreateCategory_ShouldReturnBadRequest_WhenNameExists()
        {
            _context.Categories.Add(new Category { CategoryID = 1, CategoryName = "Existing" });
            await _context.SaveChangesAsync();

            var dto = new CreateCategoryDto { CategoryName = "Existing" };

            var result = await _controller.CreateCategory(dto);

            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<CategoryDetailDto>>(actionResult.Value);

            Assert.Contains("zaten mevcut", apiResponse.Message);
        }

        // Ürünü olmayan bağımsız bir kategori silinebilmeli
        [Fact]
        public async Task DeleteCategory_ShouldReturnOk_WhenCategoryIsIndependent()
        {
            _context.Categories.Add(new Category { CategoryID = 50, CategoryName = "Delete Me" });
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteCategory(50);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(actionResult.Value);

            // İşlem başarılı mı
            Assert.True(apiResponse.Success);

            // Gerçekten veritabanından silinmiş mi
            var dbCat = await _context.Categories.FindAsync(50);
            Assert.Null(dbCat);
        }

        // Ürünü olan bir kategori silinememeli
        [Fact]
        public async Task DeleteCategory_ShouldReturnBadRequest_WhenCategoryHasProducts()
        {
            var cat = new Category { CategoryID = 60, CategoryName = "Has Products" };
            _context.Categories.Add(cat);

            _context.Products.Add(new Product
            {
                ProductID = 1,
                ProductName = "P",
                CategoryID = 60,
                Brand = "B",
                Description = "D",
                ImageUrl = "I",
                Price = 10,
                Stock = 1
            });

            await _context.SaveChangesAsync();

            var result = await _controller.DeleteCategory(60);

            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(actionResult.Value);

            Assert.Contains("ürüne sahip olduğu için silinemez", apiResponse.Message);
        }

        // Bir kategoriyi tamamen silerken tüm alt kategorilerin ve ürünlerin de silinmesi test ediliyor
        [Fact]
        public async Task PermanentDeleteCategory_ShouldDeleteEverything_Recursively()
        {
            // Test yapısı: ana kategori, alt kategori, alt kategoriye bağlı ürün
            var parent = new Category { CategoryID = 100, CategoryName = "Root" };
            var sub = new Category { CategoryID = 101, CategoryName = "Sub", ParentCategoryID = 100 };
            var product = new Product
            {
                ProductID = 500,
                ProductName = "P",
                CategoryID = 101,
                Brand = "B",
                Description = "D",
                ImageUrl = "I",
                Price = 10,
                Stock = 1
            };

            _context.Categories.Add(parent);
            _context.Categories.Add(sub);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Tracking temizleniyor, veriler yeni yükleniyormuş gibi davranılması için
            _context.ChangeTracker.Clear();

            var result = await _controller.PermanentDeleteCategory(100);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(actionResult.Value);

            Assert.True(apiResponse.Success);

            // Tüm öğelerin gerçekten silindiği kontrol ediliyor
            Assert.Null(await _context.Categories.FindAsync(100));
            Assert.Null(await _context.Categories.FindAsync(101));
            Assert.Null(await _context.Products.FindAsync(500));
        }
    }
}
