using LaundryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LaundryAPI.Data;

public class LaundryDbContext(DbContextOptions<LaundryDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ServiceOffering> ServiceOfferings => Set<ServiceOffering>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<ServiceOffering>().Property(s => s.Unit).HasConversion<string>().HasMaxLength(5);
}
