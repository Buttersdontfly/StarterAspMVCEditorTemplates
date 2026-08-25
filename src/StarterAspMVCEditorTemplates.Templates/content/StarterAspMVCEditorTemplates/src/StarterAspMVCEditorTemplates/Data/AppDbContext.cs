using Microsoft.EntityFrameworkCore;
#if (UseIdentity)
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using StarterAspMVCEditorTemplates.Identity;
#endif

namespace StarterAspMVCEditorTemplates.Data;

#if (UseIdentity)
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
	
	
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<IdentityUserRole<Guid>>().ToTable("AspNetUserRoles");
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly); 
    }

}
#else
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{

}
#endif
