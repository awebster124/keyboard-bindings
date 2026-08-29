using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KeyboardBindings.Api.Data;

/// <summary>
/// Lets `dotnet ef` build the context without running the application: the EF tools use this instead of
/// executing Program.cs, which avoids running the startup Migrate() during commands like `migrations add`.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new AppDbContext(options);
    }
}
