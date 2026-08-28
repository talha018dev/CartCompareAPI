using CartCompareAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using CartCompareApi.Ingestion.Shwapno;

namespace CartCompareAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<ShwapnoDairyImporter>();

        return services;
    }
}
