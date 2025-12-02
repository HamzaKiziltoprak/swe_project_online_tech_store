using Microsoft.EntityFrameworkCore;
namespace OnlineTechStore.Server.Data
{
    public class OnlineTechStoreDbContext : DbContext
    {
        public OnlineTechStoreDbContext(DbContextOptions<OnlineTechStoreDbContext> options)
            : base(options)
        {
        }

        public DbSet<OnlineTechStore.Server.Models.User> Users { get; set; }
        public DbSet<OnlineTechStore.Server.Models.Category> Categories { get; set; }
        public DbSet<OnlineTechStore.Server.Models.Product> Products { get; set; }
        public DbSet<OnlineTechStore.Server.Models.ProductSpecification> ProductSpecification { get; set; }
        public DbSet<OnlineTechStore.Server.Models.ProductImage> ProductImages { get; set; }
        public DbSet<OnlineTechStore.Server.Models.ProductReview> ProductReviews { get; set; }
        public DbSet<OnlineTechStore.Server.Models.CartItem> CartItems { get; set; }
        public DbSet<OnlineTechStore.Server.Models.Order> Orders { get; set; }
        public DbSet<OnlineTechStore.Server.Models.OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    
    }
}
