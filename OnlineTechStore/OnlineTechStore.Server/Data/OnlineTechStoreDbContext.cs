using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineTechStore.Server.Models;

namespace OnlineTechStore.Server.Data
{
    public class OnlineTechStoreDbContext : IdentityDbContext<ApplicationUser>
    {
        public OnlineTechStoreDbContext(DbContextOptions<OnlineTechStoreDbContext> options)
            : base(options)
        {
        }

    }
}
