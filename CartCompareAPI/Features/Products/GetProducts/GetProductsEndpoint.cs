using CartCompareAPI.Features.Products.GetProducts;

namespace CartCompareApi.Features.Products.GetProducts;

public static class Endpoint
{
    public static void MapGetProducts(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products",
            async (
                GetProductsRequest request,
                GetProductsHandler handler) =>
            {
                var result = await handler.Handle(request);
                if(result is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(result);
            });
    }
}