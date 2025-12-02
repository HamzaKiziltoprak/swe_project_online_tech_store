using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OnlineTechStore.Server.Data;
using OnlineTechStore.Server.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure EF Core (Npgsql) using Neon connection string
var neonConnection = builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? Environment.GetEnvironmentVariable("NEON_CONNECTION");

if (string.IsNullOrWhiteSpace(neonConnection))
{
    throw new InvalidOperationException("Neon connection string is not configured. Set ConnectionStrings:DefaultConnection in appsettings.json or NEON_CONNECTION environment variable.");
}

builder.Services.AddDbContext<OnlineTechStoreDbContext>(options =>
    options.UseNpgsql(neonConnection, npgsqlOptions =>
    {
        // Optional: enable retry on failure for transient Neon/network issues
        npgsqlOptions.EnableRetryOnFailure();
    })
);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<OnlineTechStoreDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
