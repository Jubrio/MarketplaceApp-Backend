using MarketplaceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceApp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Promotion> Promotions => Set<Promotion>(); 
    public DbSet<ProductPromotion> ProductPromotions => Set<ProductPromotion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User — email unique
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();

        // Shop → Vendor (1 vendeur = 1 boutique)
        modelBuilder.Entity<Shop>()
            .HasOne(s => s.Vendor)
            .WithOne(u => u.Shop)
            .HasForeignKey<Shop>(s => s.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Order → Buyer
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Buyer)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Order → Shop
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Shop)
            .WithMany(s => s.Orders)
            .HasForeignKey(o => o.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        // Payment → Order (1-1)
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithOne(o => o.Payment)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Review → OrderItem (1-1)
        modelBuilder.Entity<Review>()
            .HasOne(r => r.OrderItem)
            .WithOne(oi => oi.Review)
            .HasForeignKey<Review>(r => r.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Review → Buyer
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Buyer)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category auto-référencée
        modelBuilder.Entity<Category>()
            .HasOne(c => c.Parent)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Promotion — code unique
        modelBuilder.Entity<Promotion>()
            .HasIndex(p => p.Code)
            .IsUnique();

        // Précision décimales
        modelBuilder.Entity<Product>()
            .Property(p => p.Price).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Order>()
            .Property(o => o.CommissionAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Order>()
            .Property(o => o.VendorAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Shop>()
            .Property(s => s.CommissionRate).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount).HasColumnType("decimal(18,2)");
    }
}