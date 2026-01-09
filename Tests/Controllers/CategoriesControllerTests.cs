using Backend.Controllers;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
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
    public class CategoriesControllerTests : IDisposable
    {
        private readonly DataContext _context;
        private readonly Mock<ILogger<CategoriesController>> _mockLogger;
        private readonly CategoriesController _controller;

        public CategoriesControllerTests()
        {
            // 1. InMemory DB Setup
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DataContext(options);
            _mockLogger = new Mock<ILogger<CategoriesController>>();

            // 2. Seed Data
            SeedDatabase();

            // 3. Controller Setup
            _controller = new CategoriesController(_context, _mockLogger.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private void SeedDatabase()
        {
            // Parent Categories
            var electronics = new Category { CategoryID = 1, CategoryName = "Electronics" };
            var clothing = new Category { CategoryID = 2, CategoryName = "Clothing" };

            // Sub Categories
            var laptops = new Category { CategoryID = 3, CategoryName = "Laptops", ParentCategoryID = 1, ParentCategory = electronics };
            var phones = new Category { CategoryID = 4, CategoryName = "Phones", ParentCategoryID = 1, ParentCategory = electronics };

            // Products
            var product1 = new Product { ProductID = 1, ProductName = "MacBook", CategoryID = 3, Category = laptops, Description = "Desc", ImageUrl = "url" };
            
            _context.Categories.AddRange(electronics, clothing, laptops, phones);
            _context.Products.Add(product1);
            _context.SaveChanges();
        }

        #region GetCategories Tests

        [Fact]
        public async Task GetCategories_ShouldReturnOnlyParentCategories_WithSubCategories()
        {
            // Act
            var result = await _controller.GetCategories();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<CategoryDetailDto>>>(okResult.Value);
            
            Assert.True(response!.Success);
            
            // Sadece ParentCategoryID == null olanlar gelmeli (Electronics, Clothing)
            Assert.Equal(2, response.Data!.Count);
            
            var electronics = response.Data.First(c => c.CategoryName == "Electronics");
            
            // Electronics'in 2 alt kategorisi olmalı (Laptops, Phones)
            Assert.Equal(2, electronics.SubCategories!.Count);
        }

        #endregion

        #region GetCategoryById Tests

        [Fact]
        public async Task GetCategoryById_ShouldReturnCategoryDetail_WhenExists()
        {
            // Act
            var result = await _controller.GetCategoryById(1); // Electronics

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<CategoryDetailDto>>(okResult.Value);
            
            Assert.True(response!.Success);
            Assert.Equal("Electronics", response.Data!.CategoryName);
            Assert.Equal(2, response.Data.SubCategories!.Count);
        }

        [Fact]
        public async Task GetCategoryById_ShouldReturnNotFound_WhenCategoryDoesNotExist()
        {
            // Act
            var result = await _controller.GetCategoryById(99);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<CategoryDetailDto>>(notFoundResult.Value);
            Assert.Equal("ID 99 ile kategori bulunamadı", response!.Message);
        }

        #endregion

        #region GetSubCategories Tests

        [Fact]
        public async Task GetSubCategories_ShouldReturnChildren_WhenParentExists()
        {
            // Act
            var result = await _controller.GetSubCategories(1); // Electronics

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<CategoryDto>>>(okResult.Value);
            
            Assert.True(response!.Success);
            Assert.Equal(2, response.Data!.Count); // Laptops, Phones
            Assert.Equal("Electronics", response.Data[0].ParentCategoryName);
        }

        [Fact]
        public async Task GetSubCategories_ShouldReturnNotFound_WhenParentDoesNotExist()
        {
            // Act
            var result = await _controller.GetSubCategories(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region CreateCategory Tests

        [Fact]
        public async Task CreateCategory_ShouldReturnCreated_WhenValid()
        {
            // Arrange
            var dto = new CreateCategoryDto { CategoryName = "Home" };

            // Act
            var result = await _controller.CreateCategory(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<ApiResponse<CategoryDetailDto>>(createdResult.Value);
            
            Assert.True(response!.Success);
            Assert.Equal("Home", response.Data!.CategoryName);
            
            // DB Check
            Assert.NotNull(await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Home"));
        }

        [Fact]
        public async Task CreateCategory_ShouldReturnBadRequest_WhenNameExists()
        {
            // Arrange
            var dto = new CreateCategoryDto { CategoryName = "Electronics" }; // Already exists

            // Act
            var result = await _controller.CreateCategory(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<CategoryDetailDto>>(badRequestResult.Value);
            Assert.Contains("zaten mevcut", response!.Message);
        }

        [Fact]
        public async Task CreateCategory_ShouldReturnBadRequest_WhenParentCategoryNotFound()
        {
            // Arrange
            var dto = new CreateCategoryDto { CategoryName = "Sub", ParentCategoryID = 99 };

            // Act
            var result = await _controller.CreateCategory(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("ana kategori bulunamadı", ((ApiResponse<CategoryDetailDto>)badRequestResult.Value!).Message);
        }

        #endregion

        #region UpdateCategory Tests

        [Fact]
        public async Task UpdateCategory_ShouldReturnOk_WhenUpdateSuccessful()
        {
            // Arrange
            var dto = new UpdateCategoryDto { CategoryName = "Computers" };

            // Act: Rename "Electronics" (ID 1) -> "Computers"
            var result = await _controller.UpdateCategory(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<CategoryDetailDto>>(okResult.Value);
            
            Assert.Equal("Computers", response!.Data!.CategoryName);
            
            // DB Check
            var category = await _context.Categories.FindAsync(1);
            Assert.Equal("Computers", category!.CategoryName);
        }

        [Fact]
        public async Task UpdateCategory_ShouldReturnBadRequest_WhenSelfParenting()
        {
            // Arrange: Try to set ParentID = 1 for Category ID 1
            var dto = new UpdateCategoryDto { CategoryName = "Electronics", ParentCategoryID = 1 };

            // Act
            var result = await _controller.UpdateCategory(1, dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("kendisinin ana kategorisi olamaz", ((ApiResponse<CategoryDetailDto>)badRequestResult.Value!).Message);
        }

        [Fact]
        public async Task UpdateCategory_ShouldReturnBadRequest_WhenNameConflict()
        {
            // Arrange: Rename "Clothing" (ID 2) to "Electronics" (ID 1)
            var dto = new UpdateCategoryDto { CategoryName = "Electronics" };

            // Act
            var result = await _controller.UpdateCategory(2, dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("başka kategori zaten mevcut", ((ApiResponse<CategoryDetailDto>)badRequestResult.Value!).Message);
        }

        #endregion

        #region DeleteCategory Tests

        [Fact]
        public async Task DeleteCategory_ShouldReturnOk_WhenCategoryCanBeDeleted()
        {
            // Arrange
            // Create a temporary empty category
            var emptyCat = new Category { CategoryName = "Empty" };
            _context.Categories.Add(emptyCat);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteCategory(emptyCat.CategoryID);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response!.Success);

            // DB Check
            Assert.Null(await _context.Categories.FindAsync(emptyCat.CategoryID));
        }

        [Fact]
        public async Task DeleteCategory_ShouldReturnBadRequest_WhenCategoryHasProducts()
        {
            // Arrange
            // "Laptops" (ID 3) has products
            
            // Act
            var result = await _controller.DeleteCategory(3);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.Contains("ürüne sahip olduğu için silinemez", response!.Message);
        }

        [Fact]
        public async Task DeleteCategory_ShouldReturnBadRequest_WhenCategoryHasSubCategories()
        {
            // Arrange
            // "Electronics" (ID 1) has subcategories (Laptops, Phones)

            // Act
            var result = await _controller.DeleteCategory(1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.Contains("alt kategoriye sahip olduğu için silinemez", response!.Message);
        }

        #endregion

        #region PermanentDeleteCategory Tests

        [Fact]
        public async Task PermanentDeleteCategory_ShouldDeleteEverything_WhenCalled()
        {
            // Arrange
            // Delete "Electronics" (ID 1) permanently. Should delete:
            // 1. Electronics (Category)
            // 2. Laptops, Phones (SubCategories)
            // 3. Products in Laptops (MacBook)

            // Act
            var result = await _controller.PermanentDeleteCategory(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response!.Success);

            // Verifications
            Assert.Null(await _context.Categories.FindAsync(1)); // Electronics
            Assert.Null(await _context.Categories.FindAsync(3)); // Laptops
            Assert.Null(await _context.Products.FindAsync(1));   // MacBook (Product in Laptops)
        }

        #endregion

        #region SearchCategories Tests

        [Fact]
        public async Task SearchCategories_ShouldReturnMatchingCategories()
        {
            // Act
            var result = await _controller.SearchCategories("lap"); // Should match "Laptops"

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<List<CategoryDto>>>(okResult.Value);
            
            Assert.True(response!.Success);
            
            // DÜZELTME: Null kontrolü ve veriye güvenli erişim
            Assert.NotNull(response.Data);
            Assert.Single(response.Data);
            
            Assert.Equal("Laptops", response.Data[0].CategoryName);
        }

        [Fact]
        public async Task SearchCategories_ShouldReturnBadRequest_WhenTermIsEmpty()
        {
            // Act
            var result = await _controller.SearchCategories("");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        #endregion
    }
}