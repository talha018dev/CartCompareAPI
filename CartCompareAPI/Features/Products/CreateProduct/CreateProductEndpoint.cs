using CartCompareAPI.Features.Shared;

namespace CartCompareAPI.Features.Products.CreateProduct;

public static class CreateProductEndpoint
{
    public static void MapCreateProduct(this IEndpointRouteBuilder app) =>
        app.MapPost("/api/products", async (CreateProductRequest request, CreateProductHandler handler) =>
        {
            var result = await handler.Handle(request);
            return result.Error is not null
                ? Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error] })
                : Results.Created($"/api/products/{result.Value!.Id}", result.Value);
        }).WithName("CreateProduct").WithTags("Products");
}
