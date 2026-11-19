using LaundryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LaundryAPI.Data;

public class LaundryDbContext(DbContextOptions<LaundryDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
}
