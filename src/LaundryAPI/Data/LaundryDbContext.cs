using LaundryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LaundryAPI.Data;

public class LaundryDbContext(DbContextOptions<LaundryDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ServiceOffering> ServiceOfferings => Set<ServiceOffering>();
    public DbSet<LaundryOrder> Orders => Set<LaundryOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceOffering>().Property(s => s.Unit).HasConversion<string>().HasMaxLength(5);
        var order = modelBuilder.Entity<LaundryOrder>();
        order.Property(o => o.Code).HasMaxLength(14)
            .HasComputedColumnSql("'PG-' + CONVERT(varchar(11), [Id])", stored: true);
        order.HasIndex(o => o.Code).IsUnique();
        order.HasOne<Customer>().WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
        var line = modelBuilder.Entity<OrderLine>();
        line.Property(l => l.Unit).HasConversion<string>().HasMaxLength(5);
        line.HasOne<ServiceOffering>().WithMany().HasForeignKey(l => l.ServiceId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrderStatusEvent>().HasOne(e => e.Account).WithMany()
            .HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
