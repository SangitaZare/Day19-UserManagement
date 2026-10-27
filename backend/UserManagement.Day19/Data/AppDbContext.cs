using Microsoft.EntityFrameworkCore;
using UserManagement.Day19.Models;

namespace UserManagement.Day19.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
}