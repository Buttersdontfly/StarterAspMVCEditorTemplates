using Microsoft.EntityFrameworkCore;
using SampleIdentityApp.Data;
using Microsoft.AspNetCore.Identity;
using SampleIdentityApp.Identity;
using SampleIdentityApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "No 'DefaultConnection' connection string was found in configuration.");

connectionString = SqliteDatabasePath.Resolve(connectionString, builder.Environment.ContentRootPath);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddRoles<ApplicationRole>()
    .AddSignInManager<ApplicationSignInManager>()
    .AddDefaultTokenProviders();


if (builder.Environment.IsDevelopment()) {
    builder.Services.AddSingleton<IAppEmailSender, DevConsoleEmailSender>();
    builder.Services.AddScoped<IEmailSender<ApplicationUser>, IdentityEmailSenderAdapter>();
    builder.Services.AddScoped<IDataInitializer, DevelopmentDataSeeder>();
}

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!db.Database.GetMigrations().Any())
    {
        throw new InvalidOperationException();
    }
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<IDataInitializer>();
    await seeder.SeedDataAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();

// Exposed so the integration tests can drive the app with WebApplicationFactory.
public partial class Program;
