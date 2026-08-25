using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SampleIdentityApp.Identity;

namespace SampleIdentityApp.Data;

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
