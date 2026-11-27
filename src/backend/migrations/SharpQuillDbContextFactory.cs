using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SharpQuill.Migrations;

public class SharpQuillDbContextFactory : IDesignTimeDbContextFactory<SharpQuillDbContext>
{
    public SharpQuillDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SharpQuillDbContext>();

        // Default connection string for migrations - override with environment variable if needed
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=cms;Username=cms_user;Password=cms_password";

        optionsBuilder.UseNpgsql(connectionString);

        // Suppress PendingModelChangesWarning for version upgrade scenarios
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

        return new SharpQuillDbContext(optionsBuilder.Options);
    }
}
