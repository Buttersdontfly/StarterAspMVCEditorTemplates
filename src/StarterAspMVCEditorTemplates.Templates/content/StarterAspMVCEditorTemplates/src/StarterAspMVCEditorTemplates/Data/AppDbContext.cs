using Microsoft.EntityFrameworkCore;
#if (UseIdentity)
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
#endif

namespace StarterAspMVCEditorTemplates.Data;

#if (UseIdentity)
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    // Add your own DbSet<T> properties here.
}
#else
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Add your own DbSet<T> properties here, then run
    // `dotnet ef migrations add Initial` to create the first migration.
}
#endif
