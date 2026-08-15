namespace CartCompareAPI.Features.Products.GetProductById;

public static class GetProductByIdEndpoint
{
    public static void MapGetProductById(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/products/{id:guid}", async (Guid id, GetProductByIdHandler handler) =>
        {
            var product = await handler.Handle(id);
            return product is null ? Results.NotFound() : Results.Ok(product);
        }).WithName("GetProductById").WithTags("Products");
}
