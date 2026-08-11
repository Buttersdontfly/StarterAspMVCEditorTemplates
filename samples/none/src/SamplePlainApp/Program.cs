using Microsoft.EntityFrameworkCore;
using SamplePlainApp.Data;
using SamplePlainApp.Services;

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

// SEAM: email sender.
// The development sender writes to the console, to .eml files under
// App_Data/mail/, and to the /dev/mailbox page. Replace this single
// registration with an SMTP or transactional-email implementation.
builder.Services.AddSingleton<IAppEmailSender, DevConsoleEmailSender>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();


    await db.Database.MigrateAsync();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Exposed so the integration tests can drive the app with WebApplicationFactory.
public partial class Program;
