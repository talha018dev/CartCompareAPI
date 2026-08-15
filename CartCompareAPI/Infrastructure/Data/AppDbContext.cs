using System;
using CartCompareApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CartCompareAPI.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StoreProduct> StoreProducts => Set<StoreProduct>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
}
