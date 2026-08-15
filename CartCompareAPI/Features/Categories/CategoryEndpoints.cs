using CartCompareAPI.Features.Shared;

namespace CartCompareAPI.Features.Categories;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories").WithTags("Categories");
        group.MapGet("", async (CategoryHandler handler) => Results.Ok(await handler.GetAllAsync())).WithName("GetCategories");
        group.MapGet("/{id:guid}", async (Guid id, CategoryHandler handler) =>
        {
            var category = await handler.GetByIdAsync(id);
            return category is null ? Results.NotFound() : Results.Ok(category);
        }).WithName("GetCategoryById");
        group.MapPost("", async (UpsertCategoryRequest request, CategoryHandler handler) =>
        {
            var result = await handler.CreateAsync(request);
            return result.Error is not null ? Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error] })
                : Results.Created($"/api/categories/{result.Value!.Id}", result.Value);
        }).WithName("CreateCategory");
        group.MapPut("/{id:guid}", async (Guid id, UpsertCategoryRequest request, CategoryHandler handler) => ToResult(await handler.UpdateAsync(id, request))).WithName("UpdateCategory");
        group.MapDelete("/{id:guid}", async (Guid id, CategoryHandler handler) => ToResult(await handler.DeleteAsync(id))).WithName("DeleteCategory");
    }

    private static IResult ToResult(CrudResult result) => !result.Found ? Results.NotFound()
        : result.IsConflict ? Results.Conflict(new { message = result.Error })
        : result.Error is not null ? Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error] })
        : Results.NoContent();
}
