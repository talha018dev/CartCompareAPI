using CartCompareAPI.Domain.Entities;
using CartCompareAPI.Features.Shared;
using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Features.Brands;

public sealed class BrandHandler(AppDbContext db)
{
    public Task<List<BrandResponse>> GetAllAsync() => db.Brands.AsNoTracking().OrderBy(x => x.Name).Select(x => new BrandResponse(x.Id, x.Name, x.Slug)).ToListAsync();
    public Task<BrandResponse?> GetByIdAsync(Guid id) => db.Brands.AsNoTracking().Where(x => x.Id == id).Select(x => new BrandResponse(x.Id, x.Name, x.Slug)).FirstOrDefaultAsync();
    public async Task<CrudResult<BrandResponse>> CreateAsync(UpsertBrandRequest request)
    {
        var error = await ValidateAsync(request); if (error is not null) return CrudResult<BrandResponse>.Invalid(error);
        var brand = new Brand { Id = Guid.NewGuid(), Name = request.Name.Trim(), Slug = request.Slug.Trim().ToLowerInvariant() };
        db.Brands.Add(brand); await db.SaveChangesAsync(); return CrudResult<BrandResponse>.Success(new(brand.Id, brand.Name, brand.Slug));
    }
    public async Task<CrudResult> UpdateAsync(Guid id, UpsertBrandRequest request)
    {
        var brand = await db.Brands.FindAsync(id); if (brand is null) return CrudResult.NotFound();
        var error = await ValidateAsync(request, id); if (error is not null) return CrudResult.Invalid(error);
        brand.Name = request.Name.Trim(); brand.Slug = request.Slug.Trim().ToLowerInvariant(); await db.SaveChangesAsync(); return CrudResult.Success();
    }
    public async Task<CrudResult> DeleteAsync(Guid id)
    {
        var brand = await db.Brands.FindAsync(id); if (brand is null) return CrudResult.NotFound();
        if (await db.Products.AnyAsync(x => x.BrandId == id)) return CrudResult.Conflict("This brand cannot be deleted because products reference it.");
        db.Brands.Remove(brand); await db.SaveChangesAsync(); return CrudResult.Success();
    }
    private async Task<string?> ValidateAsync(UpsertBrandRequest request, Guid? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug)) return "Name and slug are required.";
        var slug = request.Slug.Trim().ToLowerInvariant(); return await db.Brands.AnyAsync(x => x.Slug == slug && x.Id != currentId) ? "A brand with this slug already exists." : null;
    }
}
