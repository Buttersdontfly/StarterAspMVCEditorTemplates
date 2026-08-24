using Microsoft.EntityFrameworkCore;
using StarterAspMVCEditorTemplates.Data;
#if (UseIdentity)
using Microsoft.AspNetCore.Identity;
using StarterAspMVCEditorTemplates.Identity;
using StarterAspMVCEditorTemplates.Services;
#endif
#if (UsePepper)
using StarterAspMVCEditorTemplates.Protection;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "No 'DefaultConnection' connection string was found in configuration.");

#if (UseSqlite)
connectionString = SqliteDatabasePath.Resolve(connectionString, builder.Environment.ContentRootPath);
#endif

builder.Services.AddDbContext<AppDbContext>(options =>
#if (UseSqlite)
    options.UseSqlite(connectionString));
#endif
#if (UseSqlServer)
    options.UseSqlServer(connectionString));
#endif

#if (UseIdentity)
builder.Services.AddSingleton<IAppEmailSender, DevConsoleEmailSender>();
#endif

#if (UseIdentity)
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
        options.SignIn.RequireConfirmedAccount = false;
#if (UseProtectedData)
        options.Stores.ProtectPersonalData = true;
#endif
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddRoles<ApplicationRole>()
    .AddSignInManager<ApplicationSignInManager>()
#if (UseProtectedData)
    .AddPersonalDataProtection<LookupProtector, KeyRing>()
#endif
    .AddDefaultTokenProviders();

#if (UsePepper)
builder.Services.AddOptions<PepperOptions>()
    .Bind(builder.Configuration.GetSection(PepperOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PepperedPasswordHasher<ApplicationUser>>();
#endif
#if (UseProtectedData)
builder.Services.AddOptions<LookupProtectionOptions>()
    .Bind(builder.Configuration.GetSection(LookupProtectionOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
#endif

builder.Services.AddScoped<IEmailSender<ApplicationUser>, IdentityEmailSenderAdapter>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});
#endif

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

#if (UseIdentity)
    if (!db.Database.GetMigrations().Any())
    {
        throw new InvalidOperationException(
            """
            This project has no EF Core migrations, so the database has no tables.

            Create the first migration:
                dotnet ef migrations add Initial --project src/<YourProject>

            If you are working on the TEMPLATE rather than a generated project,
            run build/Generate-Migrations.ps1 instead: migrations ship inside the
            template and are not committed by the template's own build.
            """);
    }
#endif
    await db.Database.MigrateAsync();
#if (UseIdentity)
    await SeedData.SeedAsync(scope.ServiceProvider);
#endif
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

#if (UseIdentity)
app.UseAuthentication();
#endif
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Exposed so the integration tests can drive the app with WebApplicationFactory.
public partial class Program;
