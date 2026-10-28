using Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Api.Features.Items;

/// <summary>The sample feature: list, add, change and delete items, under /api/items.</summary>
public static class ItemEndpoints
{
    public sealed record ItemRequest(string Name, bool Done = false);

    public static void MapItems(this IEndpointRouteBuilder api)
    {
        var items = api.MapGroup("/items").WithTags("Items");

        items.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
            await db.Items.AsNoTracking().OrderBy(i => i.Id).ToListAsync(ct));

        items.MapPost("/", async Task<Results<Created<Item>, ValidationProblem>> (ItemRequest request, AppDbContext db, CancellationToken ct) =>
        {
            if (Validate(request) is { } problem)
            {
                return problem;
            }

            var item = new Item { Name = request.Name.Trim(), Done = request.Done };
            db.Items.Add(item);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/items/{item.Id}", item);
        });

        items.MapPut("/{id:int}", async Task<Results<Ok<Item>, NotFound, ValidationProblem>> (int id, ItemRequest request, AppDbContext db, CancellationToken ct) =>
        {
            if (Validate(request) is { } problem)
            {
                return problem;
            }

            var item = await db.Items.FindAsync([id], ct);
            if (item is null)
            {
                return TypedResults.NotFound();
            }

            item.Name = request.Name.Trim();
            item.Done = request.Done;
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(item);
        });

        items.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>> (int id, AppDbContext db, CancellationToken ct) =>
            await db.Items.Where(i => i.Id == id).ExecuteDeleteAsync(ct) == 0
                ? TypedResults.NotFound()
                : TypedResults.NoContent());
    }

    private static ValidationProblem? Validate(ItemRequest request) =>
        string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > Item.NameMaxLength
            ? TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = [$"The name is required, at most {Item.NameMaxLength} characters."],
            })
            : null;
}
