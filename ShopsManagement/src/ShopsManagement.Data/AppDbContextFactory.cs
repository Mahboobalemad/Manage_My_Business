using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShopsManagement.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // SQLite Connection String for Design-Time / Migration generation
        optionsBuilder.UseSqlite("Data Source=ShopsManagement.db");

        return new AppDbContext(optionsBuilder.Options);
    }
}
