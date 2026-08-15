using CartCompareAPI.Features.Shared;

namespace CartCompareAPI.Features.Brands;

public static class BrandEndpoints
{
    public static void MapBrandEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/brands").WithTags("Brands");
        group.MapGet("", async (BrandHandler handler) => Results.Ok(await handler.GetAllAsync())).WithName("GetBrands");
        group.MapGet("/{id:guid}", async (Guid id, BrandHandler handler) => { var brand = await handler.GetByIdAsync(id); return brand is null ? Results.NotFound() : Results.Ok(brand); }).WithName("GetBrandById");
        group.MapPost("", async (UpsertBrandRequest request, BrandHandler handler) => { var result = await handler.CreateAsync(request); return result.Error is not null ? Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error] }) : Results.Created($"/api/brands/{result.Value!.Id}", result.Value); }).WithName("CreateBrand");
        group.MapPut("/{id:guid}", async (Guid id, UpsertBrandRequest request, BrandHandler handler) => ToResult(await handler.UpdateAsync(id, request))).WithName("UpdateBrand");
        group.MapDelete("/{id:guid}", async (Guid id, BrandHandler handler) => ToResult(await handler.DeleteAsync(id))).WithName("DeleteBrand");
    }
    private static IResult ToResult(CrudResult result) => !result.Found ? Results.NotFound() : result.IsConflict ? Results.Conflict(new { message = result.Error }) : result.Error is not null ? Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error] }) : Results.NoContent();
}
