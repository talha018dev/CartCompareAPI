using CartCompareAPI.Infrastructure.Data;
using CartCompareAPI.Ingestion.Shwapno;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<ShwapnoDairyImporter>();

        return services;
    }
}
