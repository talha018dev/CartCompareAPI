using CartCompareAPI.Features.Products.GetProducts;
using CartCompareAPI.Features.Products.GetProductById;
using CartCompareAPI.Features.Products.CreateProduct;
using CartCompareAPI.Features.Products.EditProduct;
using CartCompareAPI.Features.Products.DeleteProduct;
using CartCompareAPI.Features.Brands;
using CartCompareAPI.Features.Categories;
using CartCompareAPI.Features.Stores;

namespace CartCompareAPI.Features.Products;

public static class ProductsModule
{
    public static IServiceCollection AddProductFeatures(this IServiceCollection services)
    {
        services.AddScoped<GetProductsHandler>();
        services.AddScoped<GetProductByIdHandler>();
        services.AddScoped<CreateProductHandler>();
        services.AddScoped<EditProductHandler>();
        services.AddScoped<DeleteProductHandler>();
        services.AddScoped<CategoryHandler>();
        services.AddScoped<BrandHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetProducts();
        app.MapGetProductById();
        app.MapCreateProduct();
        app.MapEditProduct();
        app.MapDeleteProduct();
        app.MapCategoryEndpoints();
        app.MapBrandEndpoints();
        app.MapStoreEndpoints();

        return app;
    }
}
