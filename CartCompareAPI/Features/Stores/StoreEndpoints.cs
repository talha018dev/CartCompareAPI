using CartCompareApi.Domain.Entities;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Features.Stores;

public static class StoreEndpoints
{
    public static void MapStoreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stores").WithTags("Stores");

        group.MapGet("", async (AppDbContext db) => Results.Ok(await db.Stores.AsNoTracking()
            .OrderBy(x => x.Name).Select(x => new StoreResponse(x.Id, x.Name, x.Slug, x.WebsiteUrl, x.IsActive)).ToListAsync()))
            .WithName("GetStores");
        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var store = await db.Stores.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new StoreResponse(x.Id, x.Name, x.Slug, x.WebsiteUrl, x.IsActive)).FirstOrDefaultAsync();
            return store is null ? Results.NotFound() : Results.Ok(store);
        }).WithName("GetStoreById");

        group.MapPost("", async (UpsertStoreRequest request, AppDbContext db) =>
        {
            var error = await Validate(request, db);
            if (error is not null) return error;
            var store = new Store
            {
                Id = Guid.NewGuid(), Name = request.Name.Trim(), Slug = request.Slug.Trim().ToLowerInvariant(),
                WebsiteUrl = request.WebsiteUrl?.Trim(), IsActive = request.IsActive
            };
            db.Stores.Add(store);
            await db.SaveChangesAsync();
            return Results.Created($"/api/stores/{store.Id}", new StoreResponse(store.Id, store.Name, store.Slug, store.WebsiteUrl, store.IsActive));
        }).WithName("CreateStore");

        group.MapPut("/{id:guid}", async (Guid id, UpsertStoreRequest request, AppDbContext db) =>
        {
            var store = await db.Stores.FindAsync(id);
            if (store is null) return Results.NotFound();
            var error = await Validate(request, db, id);
            if (error is not null) return error;
            store.Name = request.Name.Trim();
            store.Slug = request.Slug.Trim().ToLowerInvariant();
            store.WebsiteUrl = request.WebsiteUrl?.Trim();
            store.IsActive = request.IsActive;
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("UpdateStore");

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var store = await db.Stores.FindAsync(id);
            if (store is null) return Results.NotFound();
            if (await db.StoreProducts.AnyAsync(x => x.StoreId == id))
                return Results.Conflict(new { message = "This store cannot be deleted because store listings reference it." });
            db.Stores.Remove(store);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("DeleteStore");
    }

    private static async Task<IResult?> Validate(UpsertStoreRequest request, AppDbContext db, Guid? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["Name and slug are required."] });
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await db.Stores.AnyAsync(x => x.Slug == slug && x.Id != currentId))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["slug"] = ["A store with this slug already exists."] });
        return null;
    }
}

public sealed record UpsertStoreRequest(string Name, string Slug, string? WebsiteUrl, bool IsActive = true);
public sealed record StoreResponse(Guid Id, string Name, string Slug, string? WebsiteUrl, bool IsActive);
