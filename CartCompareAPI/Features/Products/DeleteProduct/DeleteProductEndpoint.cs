namespace CartCompareAPI.Features.Products.DeleteProduct;

public static class DeleteProductEndpoint
{
    public static void MapDeleteProduct(this IEndpointRouteBuilder app) =>
        app.MapDelete("/api/products/{id:guid}", async (Guid id, DeleteProductHandler handler) =>
        {
            var result = await handler.Handle(id);
            return !result.Found ? Results.NotFound()
                : result.IsConflict ? Results.Conflict(new { message = result.Error })
                : Results.NoContent();
        }).WithName("DeleteProduct").WithTags("Products");
}
