using CartCompareAPI.Ingestion.Shwapno;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Infrastructure.Data;

public static class DatabaseInitialization
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        var importer = scope.ServiceProvider.GetRequiredService<ShwapnoDairyImporter>();
        await importer.ImportAsync();
    }
}
