using CartCompareAPI.Features.Shared;

namespace CartCompareAPI.Features.Products.EditProduct;

public static class EditProductEndpoint
{
    public static void MapEditProduct(this IEndpointRouteBuilder app) =>
        app.MapPut("/api/products/{id:guid}", async (Guid id, EditProductRequest request, EditProductHandler handler) =>
        {
            var result = await handler.Handle(id, request);
            return !result.Found ? Results.NotFound()
                : result.Error is not null ? Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error] })
                : Results.NoContent();
        }).WithName("EditProduct").WithTags("Products");
}
