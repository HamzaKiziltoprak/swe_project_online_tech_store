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
using System.Threading.Tasks;
using Xunit;

namespace Tests.Controllers
{
    public class ProductsControllerTests
    {
        private readonly DataContext _context;
        private readonly Mock<ILogger<ProductsController>> _mockLogger;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            // Bellekte çalışan bir test veritabanı oluşturuyoruz. Bu sayede her test izole şekilde çalışıyor.
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;
            _context = new DataContext(options);

            // Controller'ın loglama bağımlılığını taklit ediyoruz.
            _mockLogger = new Mock<ILogger<ProductsController>>();

            // Controller'ı gerçek bir context ve mock logger ile oluşturuyoruz.
            _controller = new ProductsController(
                _context,
                _mockLogger.Object
            );
        }

        // Ürün listeleme filtre testleri

        [Fact]
        public async Task GetProducts_ShouldReturnFilteredList_WhenSearchTermIsProvided()
        {
            // Test için kategori ekliyoruz.
            var cat1 = new Category { CategoryID = 1, CategoryName = "Electronics" };
            _context.Categories.Add(cat1);

            // Farklı markalara ait ürünleri ekleyip arama filtresinin doğru çalışıp çalışmadığını test edeceğiz.
            _context.Products.Add(new Product { ProductID = 1, ProductName = "iPhone 13", BrandID = 1, Category = cat1, IsActive = true, Price = 1000, Stock = 10, Description="D", ImageUrl="I" });
            _context.Products.Add(new Product { ProductID = 2, ProductName = "Samsung S21", BrandID = 1, Category = cat1, IsActive = true, Price = 900, Stock = 10, Description="D", ImageUrl="I" });
            _context.Products.Add(new Product { ProductID = 3, ProductName = "MacBook", BrandID = 1, Category = cat1, IsActive = true, Price = 2000, Stock = 10, Description="D", ImageUrl="I" });
            await _context.SaveChangesAsync();

            // Apple içeren ürünleri filtrelemek için arama parametresi oluşturuyoruz.
            var filter = new ProductFilterParams { SearchTerm = "Apple" };

            // Controller'dan listeyi alıyoruz.
            var result = await _controller.GetProducts(filter);

            // Dönen yapının başarılı bir yanıt olup olmadığını kontrol ediyoruz.
            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PagedResult<ProductListDto>>>(actionResult.Value);
            var data = apiResponse.Data;

            // Apple markalı 2 ürünün gelmesi gerekiyor.
            Assert.Equal(2, data.TotalCount);
            Assert.Contains(data.Items, p => p.ProductName == "iPhone 13");
            Assert.Contains(data.Items, p => p.ProductName == "MacBook");
        }

        [Fact]
        public async Task GetProducts_ShouldFilterByPriceRange()
        {
            // Kategori ekliyoruz.
            var cat1 = new Category { CategoryID = 1, CategoryName = "Test" };
            _context.Categories.Add(cat1);

            // Fiyat aralığı filtresinin çalışmasını test etmek için ürünler ekliyoruz.
            _context.Products.Add(new Product { ProductID = 1, ProductName = "Cheap", Price = 50, Category = cat1, IsActive=true, BrandID=1, Description="D", ImageUrl="I", Stock=1 });
            _context.Products.Add(new Product { ProductID = 2, ProductName = "Mid", Price = 150, Category = cat1, IsActive=true, BrandID=1, Description="D", ImageUrl="I", Stock=1 });
            _context.Products.Add(new Product { ProductID = 3, ProductName = "Expensive", Price = 500, Category = cat1, IsActive=true, BrandID=1, Description="D", ImageUrl="I", Stock=1 });
            await _context.SaveChangesAsync();

            // 100-200 aralığına uyan sadece Mid ürünü olacak.
            var filter = new ProductFilterParams { MinPrice = 100, MaxPrice = 200 };

            var result = await _controller.GetProducts(filter);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<PagedResult<ProductListDto>>>(actionResult.Value);
            
            Assert.Equal(1, apiResponse.Data.TotalCount);
            Assert.Equal("Mid", apiResponse.Data.Items[0].ProductName);
        }

        // Ürün ID ile detay çekme testleri

        [Fact]
        public async Task GetProductById_ShouldReturnProduct_WhenIdExists()
        {
            // Test için kategori ve hedef ürünü seedliyoruz.
            var cat = new Category { CategoryID = 1, CategoryName = "Cat" };
            _context.Categories.Add(cat);
            var product = new Product { ProductID = 10, ProductName = "Target", Price = 100, Category = cat, BrandID=1, Description="D", ImageUrl="I", Stock=1, IsActive=true };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Controller'dan ID ile ürünü istiyoruz.
            var result = await _controller.GetProductById(10);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductDetailDto>>(actionResult.Value);
            
            // Doğru ürün dönmüş mü kontrol ediyoruz.
            Assert.Equal("Target", apiResponse.Data.ProductName);
        }

        [Fact]
        public async Task GetProductById_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            // Var olmayan ID ile çağırdığımızda not found bekliyoruz.
            var result = await _controller.GetProductById(999);

            var actionResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        // Ürün oluşturma testleri

        [Fact]
        public async Task CreateProduct_ShouldReturnCreated_WhenDataIsValid()
        {
            // Sistemde mevcut bir kategori ekliyoruz.
            _context.Categories.Add(new Category { CategoryID = 1, CategoryName = "Existing Cat" });
            await _context.SaveChangesAsync();

            // Yeni ürün oluşturmak için DTO hazırlıyoruz.
            var dto = new CreateProductDto
            {
                ProductName = "New Product",
                BrandID = 1,
                Description = "Desc",
                Price = 99.99m,
                Stock = 50,
                CategoryID = 1,
                ImageUrl = "img.png",
                IsActive = true
            };

            // Controller çağrısı
            var result = await _controller.CreateProduct(dto);

            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductDetailDto>>(actionResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal("New Product", apiResponse.Data.ProductName);

            // Ürün gerçekten veritabanına eklenmiş mi kontrol ediyoruz.
            var dbProduct = await _context.Products.FirstOrDefaultAsync(p => p.ProductName == "New Product");
            Assert.NotNull(dbProduct);
        }

        [Fact]
        public async Task CreateProduct_ShouldReturnBadRequest_WhenCategoryDoesNotExist()
        {
            // Var olmayan kategori ID veriyoruz.
            var dto = new CreateProductDto { CategoryID = 99, ProductName = "Fail", BrandID=1, Description="D", ImageUrl="I", Price=10, Stock=1 };

            var result = await _controller.CreateProduct(dto);

            var actionResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductDetailDto>>(actionResult.Value);
            Assert.Contains("Category with ID 99 does not exist", apiResponse.Message);
        }

        // Ürün teknik özellikleri testleri

        [Fact]
        public async Task AddProductSpecification_ShouldReturnCreated_WhenProductExists()
        {
            // Özellik ekleyeceğimiz ürünü oluşturuyoruz.
            _context.Products.Add(new Product { ProductID = 1, ProductName = "P1", CategoryID=1, BrandID=1, Description="D", ImageUrl="I", Price=10, Stock=1 });
            await _context.SaveChangesAsync();

            var specDto = new CreateProductSpecificationDto { SpecName = "Color", SpecValue = "Red" };

            var result = await _controller.AddProductSpecification(1, specDto);

            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProductSpecificationDto>>(actionResult.Value);
            
            Assert.Equal("Color", apiResponse.Data.SpecName);

            // Özellik gerçekten eklenmiş mi kontrol ediyoruz.
            var dbSpec = await _context.ProductSpecifications.FirstOrDefaultAsync(s => s.ProductID == 1);
            Assert.Equal("Red", dbSpec.SpecValue);
        }

        [Fact]
        public async Task DeleteProductSpecification_ShouldReturnOk_WhenSpecExists()
        {
            // Silme işlemini test etmek için önce ürün ve özellik ekliyoruz.
            _context.Products.Add(new Product { ProductID = 1, ProductName = "P1", CategoryID=1, BrandID=1, Description="D", ImageUrl="I", Price=10, Stock=1 });
            _context.ProductSpecifications.Add(new ProductSpecification { SpecID = 100, ProductID = 1, SpecName = "Size", SpecValue = "L" });
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteProductSpecification(1, 100);

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            
            // Silme sonrası özellik veritabanında yok olmalı.
            var dbSpec = await _context.ProductSpecifications.FindAsync(100);
            Assert.Null(dbSpec);
        }
        
        // Öne çıkan ürünler testleri

        [Fact]
        public async Task GetFeaturedProducts_ShouldReturnActiveAndInStockProducts()
        {
            // Ürünlerin filtrelenebilmesi için kategori ekliyoruz.
            var cat = new Category { CategoryID = 1, CategoryName = "Cat" };
            _context.Categories.Add(cat);
            
            // Aktif ve stoğu olan ürün, listede dönmeli.
            _context.Products.Add(new Product { ProductID = 1, ProductName = "Valid", IsActive = true, Stock = 5, Category = cat, BrandID=1, Description="D", ImageUrl="I", Price=10, CreatedAt = DateTime.UtcNow });
            
            // Aktif olmayan ürün dönmemeli.
            _context.Products.Add(new Product { ProductID = 2, ProductName = "Inactive", IsActive = false, Stock = 5, Category = cat, BrandID=1, Description="D", ImageUrl="I", Price=10, CreatedAt = DateTime.UtcNow });
            
            // Stoğu olmayan ürün dönmemeli.
            _context.Products.Add(new Product { ProductID = 3, ProductName = "NoStock", IsActive = true, Stock = 0, Category = cat, BrandID=1, Description="D", ImageUrl="I", Price=10, CreatedAt = DateTime.UtcNow });

            await _context.SaveChangesAsync();

            var result = await _controller.GetFeaturedProducts();

            var actionResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<ProductListDto>>>(actionResult.Value);
            
            // Sadece aktif ve stoğu olan ürün gelmeli.
            Assert.Single(apiResponse.Data);
            Assert.Equal("Valid", apiResponse.Data[0].ProductName);
        }
    }
}
