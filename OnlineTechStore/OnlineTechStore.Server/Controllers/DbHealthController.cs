using Microsoft.AspNetCore.Mvc;
using OnlineTechStore.Server.Data;

namespace OnlineTechStore.Server.Controllers
{
    [ApiController]
    [Route("db")]
    public class DbHealthController : ControllerBase
    {
        private readonly OnlineTechStoreDbContext _db;

        public DbHealthController(OnlineTechStoreDbContext db)
        {
            _db = db;
        }

        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            try
            {
                var canConnect = await _db.Database.CanConnectAsync();
                if (canConnect)
                    return Ok(new { canConnect = true });
                return StatusCode(503, new { canConnect = false, message = "Cannot connect to DB." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { canConnect = false, error = ex.Message });
            }
        }
    }
}