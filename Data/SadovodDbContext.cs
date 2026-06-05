using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sadovod.Models.Entities;

namespace Sadovod.Data;

public class SadovodDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public SadovodDbContext(DbContextOptions<SadovodDbContext> options,
                            IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ShopSetting> ShopSettings => Set<ShopSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(b =>
        {
            b.ToTable("Roles");
            b.Property(x => x.Name).HasMaxLength(50).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users", t =>
            {
                t.HasTrigger("trg_Users_Update");
            });
            b.Property(x => x.Login).HasMaxLength(100).IsRequired();
            b.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            b.Property(x => x.FullName).HasMaxLength(150);
            b.Property(x => x.Phone).HasMaxLength(20);
            b.Property(x => x.RoleId).HasDefaultValue(2);
            b.HasIndex(x => x.Login).IsUnique();
            b.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.ToTable("Categories");
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("Products", t =>
            {
                // Сообщаем EF Core о триггерах — он переключится
                // с MERGE…OUTPUT на INSERT + SCOPE_IDENTITY()
                t.HasTrigger("trg_Products_Insert");
                t.HasTrigger("trg_Products_Update");
                t.HasTrigger("trg_Products_Delete");
            });
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Price).HasColumnType("decimal(10,2)");
            b.Property(x => x.Variety).HasMaxLength(100);
            b.Property(x => x.ImageUrl).HasMaxLength(500);
            b.HasOne(x => x.Category).WithMany(c => c.Products).HasForeignKey(x => x.CategoryId);
        });

        modelBuilder.Entity<OrderStatus>(b =>
        {
            b.ToTable("OrderStatuses");
            b.Property(x => x.Name).HasMaxLength(50).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("Orders");
            b.Property(x => x.OrderDate).HasDefaultValueSql("GETDATE()");
            b.Property(x => x.StatusId).HasDefaultValue(1);
            b.Property(x => x.TotalAmount).HasColumnType("decimal(10,2)").HasDefaultValue(0m);
            b.HasOne(x => x.User).WithMany(u => u.Orders).HasForeignKey(x => x.UserId);
            b.HasOne(x => x.Status).WithMany(s => s.Orders).HasForeignKey(x => x.StatusId);
        });

        modelBuilder.Entity<OrderItem>(b =>
        {
            b.ToTable("OrderItems");
            b.Property(x => x.UnitPrice).HasColumnType("decimal(10,2)");
            b.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Product).WithMany(p => p.OrderItems).HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<ShopSetting>(b =>
        {
            b.ToTable("ShopSettings");
            b.Property(x => x.Email).HasMaxLength(100);
            b.Property(x => x.Phone).HasMaxLength(20);
            b.Property(x => x.Address).HasMaxLength(300);
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable("AuditLog");
            b.Property(x => x.TableName).HasMaxLength(50).IsRequired();
            b.Property(x => x.Operation).HasMaxLength(1).IsRequired();
            b.Property(x => x.ChangeDate).HasDefaultValueSql("GETDATE()");
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PushSubscription>(b =>
        {
            b.ToTable("PushSubscriptions");
            b.Property(x => x.Endpoint).HasMaxLength(500).IsRequired();
            b.Property(x => x.P256dh).HasMaxLength(200).IsRequired();
            b.Property(x => x.Auth).HasMaxLength(100).IsRequired();
            b.HasOne(x => x.User).WithMany(u => u.PushSubscriptions).HasForeignKey(x => x.UserId);
        });

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await SetSessionUserContextAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        SetSessionUserContextAsync(CancellationToken.None).GetAwaiter().GetResult();
        return base.SaveChanges();
    }

    private async Task SetSessionUserContextAsync(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (Database.GetDbConnection().State != ConnectionState.Open)
            await Database.OpenConnectionAsync(ct);

        await using var cmd = Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "dbo.SetSessionUserContext";
        cmd.CommandType = CommandType.StoredProcedure;
        var p = new SqlParameter("@UserId", SqlDbType.Int)
        {
            Value = userId.HasValue ? userId.Value : DBNull.Value
        };
        cmd.Parameters.Add(p);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private int? GetCurrentUserId()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return null;
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idClaim, out var id) ? id : null;
    }
}
