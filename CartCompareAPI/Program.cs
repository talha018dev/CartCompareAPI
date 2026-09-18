using CartCompareAPI.Canonicalization;
using CartCompareAPI.Features.Products;
using CartCompareAPI.Infrastructure;
using CartCompareAPI.Infrastructure.Data;
using CartCompareAPI.Ingestion.Shwapno;
using CartCompareAPI.Ingestion.Shwapno.Browser;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCanonicalization(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProductFeatures();
builder.Services.AddShwapnoIngestion();
builder.Services.AddScoped<ShwapnoIngestionKeyFilter>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ShwapnoIngestionExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevelopment", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri) && uri.IsLoopback)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<ShwapnoBrowserClient>();


WebApplication app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await app.InitialiseDatabaseAsync();

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors("LocalDevelopment");
}

app.UseAuthorization();

app.MapProductEndpoints();
app.MapControllers();

app.Run();
