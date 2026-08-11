using Microsoft.EntityFrameworkCore;

namespace SamplePlainApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Add your own DbSet<T> properties here, then run
    // `dotnet ef migrations add Initial` to create the first migration.
}
