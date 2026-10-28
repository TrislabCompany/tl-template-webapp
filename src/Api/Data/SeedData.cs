using Api.Features.Items;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

/// <summary>
/// The data a new database starts with. The migrator applies it after the
/// migrations; it adds nothing when there is data already.
/// </summary>
public static class SeedData
{
    public static async Task ApplyAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        if (await db.Items.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Items.AddRange(
            new Item { Name = "Read the README" },
            new Item { Name = "Run the app with docker compose" },
            new Item { Name = "Replace this sample with the product's first feature" });
        await db.SaveChangesAsync(cancellationToken);
    }
}
