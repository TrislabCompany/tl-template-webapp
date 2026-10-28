using Api.Features.Items;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

/// <summary>
/// The EF Core context of the API. The migrator applies its migrations
/// (Data/Migrations) before each revision of the API starts.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Item> Items => Set<Item>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(item =>
        {
            item.ToTable("items");
            item.Property(i => i.Name).HasMaxLength(Item.NameMaxLength);
        });
    }
}

public static class DataServices
{
    /// <summary>Registers AppDbContext on ConnectionStrings:Database (Platform/PlatformConfiguration.cs).</summary>
    public static IServiceCollection AddAppDbContext(this IServiceCollection services, IConfiguration configuration) =>
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Database")));
}
