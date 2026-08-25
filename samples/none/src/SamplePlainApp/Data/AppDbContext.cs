using Microsoft.EntityFrameworkCore;

namespace SamplePlainApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{

}
