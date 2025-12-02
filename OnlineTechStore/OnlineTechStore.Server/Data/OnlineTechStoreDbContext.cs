using Microsoft.EntityFrameworkCore;
namespace OnlineTechStore.Server.Data
{
    public class OnlineTechStoreDbContext : DbContext
    {
        public OnlineTechStoreDbContext(DbContextOptions<OnlineTechStoreDbContext> options)
            : base(options)
        {
        }

    }
}
