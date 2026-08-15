using CartCompareAPI.Features.Products.GetProducts;

namespace CartCompareAPI.Features.Products;

public static class ProductsModule
{
    public static IServiceCollection AddProductFeatures(this IServiceCollection services)
    {
        services.AddScoped<GetProductsHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetProducts();

        return app;
    }
}
