using Microsoft.AspNetCore.Http;

namespace CartCompareAPI.Features.Products.GetProducts;

public static class GetProductsEndpoint
{
    public static void MapGetProducts(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products",
            async (
                [AsParameters] GetProductsRequest request,
                GetProductsHandler handler) =>
            {
                var result = await handler.Handle(request);
                return Results.Ok(result);
            });
    }
}
