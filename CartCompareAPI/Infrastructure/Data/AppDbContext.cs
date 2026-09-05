using System;
using CartCompareAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StoreProduct> StoreProducts => Set<StoreProduct>();
    public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StoreProduct>()
            .HasIndex(x => new { x.StoreId, x.ExternalProductId })
            .IsUnique();

        modelBuilder.Entity<StoreProduct>()
            .HasOne(x => x.Product)
            .WithMany(x => x.StoreProducts)
            .HasForeignKey(x => x.ProductId)
            .IsRequired(false);

            modelBuilder.Entity<Product>()
            .HasIndex(x => x.CanonicalKey)
            .IsUnique();

        modelBuilder.Entity<PriceHistory>()
            .HasIndex(x => new { x.StoreProductId, x.RecordedAt });
    }


}
