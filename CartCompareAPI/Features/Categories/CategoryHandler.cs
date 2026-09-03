using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Features.Shared;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Features.Categories;

public sealed class CategoryHandler(AppDbContext db)
{
    public Task<List<CategoryResponse>> GetAllAsync() => db.Categories.AsNoTracking().OrderBy(x => x.Name)
        .Select(x => new CategoryResponse(x.Id, x.Name, x.Slug)).ToListAsync();

    public Task<CategoryResponse?> GetByIdAsync(Guid id) => db.Categories.AsNoTracking().Where(x => x.Id == id)
        .Select(x => new CategoryResponse(x.Id, x.Name, x.Slug)).FirstOrDefaultAsync();

    public async Task<CrudResult<CategoryResponse>> CreateAsync(UpsertCategoryRequest request)
    {
        var error = await ValidateAsync(request);
        if (error is not null) return CrudResult<CategoryResponse>.Invalid(error);
        var category = new Category { Id = Guid.NewGuid(), Name = request.Name.Trim(), Slug = request.Slug.Trim().ToLowerInvariant() };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return CrudResult<CategoryResponse>.Success(new(category.Id, category.Name, category.Slug));
    }

    public async Task<CrudResult> UpdateAsync(Guid id, UpsertCategoryRequest request)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return CrudResult.NotFound();
        var error = await ValidateAsync(request, id);
        if (error is not null) return CrudResult.Invalid(error);
        category.Name = request.Name.Trim();
        category.Slug = request.Slug.Trim().ToLowerInvariant();
        await db.SaveChangesAsync();
        return CrudResult.Success();
    }

    public async Task<CrudResult> DeleteAsync(Guid id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return CrudResult.NotFound();
        if (await db.Products.AnyAsync(x => x.CategoryId == id)) return CrudResult.Conflict("This category cannot be deleted because products reference it.");
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return CrudResult.Success();
    }

    private async Task<string?> ValidateAsync(UpsertCategoryRequest request, Guid? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug)) return "Name and slug are required.";
        var slug = request.Slug.Trim().ToLowerInvariant();
        return await db.Categories.AnyAsync(x => x.Slug == slug && x.Id != currentId) ? "A category with this slug already exists." : null;
    }
}
