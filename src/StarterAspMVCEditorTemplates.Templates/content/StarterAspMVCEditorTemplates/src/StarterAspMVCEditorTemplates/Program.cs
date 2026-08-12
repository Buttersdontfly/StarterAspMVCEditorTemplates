using Microsoft.EntityFrameworkCore;
using StarterAspMVCEditorTemplates.Data;
#if (UseIdentity)
using Microsoft.AspNetCore.Identity;
using StarterAspMVCEditorTemplates.Services;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// SEAM: database provider.
// To use another provider, change this call AND the PackageReference in
// ../../Directory.Build.props, then delete Migrations/ and re-run
// `dotnet ef migrations add Initial`. The shipped migrations are
// SQLite-generated and are not portable across providers.
//
// SqliteDatabasePath makes the Data Source absolute under the content root and
// creates the folder, because SQLite does not create directories and a relative
// path follows the current working directory rather than the project. Delete
// that file too when you move to a server-based provider.
var connectionString = SqliteDatabasePath.Resolve(
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "No 'DefaultConnection' connection string was found in configuration."),
    builder.Environment.ContentRootPath);

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

#if (UseIdentity)
// SEAM: email sender.
// The development sender writes to the console, to .eml files under
// App_Data/mail/, and to the /dev/mailbox page. Replace this single
// registration with an SMTP or transactional-email implementation.
//
// Included only with Identity: without the account flows nothing sends mail, so
// the sender, the mailbox page and their tests would all be dead code. Add an
// IAppEmailSender of your own when your app needs to send something.
builder.Services.AddSingleton<IAppEmailSender, DevConsoleEmailSender>();
#endif

#if (UseIdentity)
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        // Deliberately relaxed so the seeded password works out of the box.
        // Tighten before you ship anything real.
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;

        // Set to true to require the emailed confirmation link before sign-in.
        // The link is printed to the console and listed at /dev/mailbox.
        options.SignIn.RequireConfirmedAccount = false;

        // SEAM: username identity.
        // Relevant if you separate username from email — see
        // Identity/AccountIdentityConventions.cs and documentation/seams.md.
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IEmailSender<IdentityUser>, IdentityEmailSenderAdapter>();

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
    // Fail fast and legibly when the Migrations folder is missing. Without this,
    // MigrateAsync quietly does nothing, the database is created empty, and the
    // first Identity query dies with "SQLite Error 1: no such table:
    // AspNetRoles" -- a stack trace that says nothing about the actual cause.
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

    // EF Core takes its own exclusive lock while applying migrations, so
    // concurrent startups are safe here without extra coordination.
    await db.Database.MigrateAsync();
#if (UseIdentity)
    // Seeding is "check, then insert", which DOES race: two instances can both
    // find the Admin role missing and both try to create it, and the loser gets
    // `UNIQUE constraint failed`. SeedAsync is written to treat "someone else
    // created it first" as success, so this is safe to call concurrently.
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
