using System.Security.Cryptography;
using SharpQuill.Core.Authors;
using SharpQuill.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not configured");

var optionsBuilder = new DbContextOptionsBuilder<SharpQuillDbContext>();
optionsBuilder.UseNpgsql(connectionString);

// Suppress PendingModelChangesWarning - triggered by EF Core version upgrades.
// Safe because we use EF Core only for migrations, not runtime queries.
optionsBuilder.ConfigureWarnings(warnings =>
    warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

using var context = new SharpQuillDbContext(optionsBuilder.Options);

Console.WriteLine("Applying database migrations...");
context.Database.Migrate();
Console.WriteLine("Migrations applied successfully.");

// First-run bootstrap: when there are no authors, create the first admin.
// ADMIN_EMAIL / ADMIN_PASSWORD (min 12 chars) configure it; without a password a random one is generated and printed once.
if (!context.Authors.Any())
{
    var email = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
    email = string.IsNullOrWhiteSpace(email) ? "admin@example.com" : AuthorRules.NormalizeEmail(email);
    if (!AuthorRules.IsValidEmail(email))
    {
        Console.Error.WriteLine($"ADMIN_EMAIL '{email}' is not a valid email address.");
        return 1;
    }

    var password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
    var generated = string.IsNullOrEmpty(password);
    if (generated)
    {
        password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18)).Replace('+', 'A').Replace('/', 'B');
    }
    else if (!AuthorRules.IsValidPassword(password))
    {
        Console.Error.WriteLine($"ADMIN_PASSWORD must be at least {AuthorRules.MinPasswordLength} characters and not only spaces.");
        return 1;
    }

    context.Authors.Add(new Author
    {
        Email = email,
        Name = "Admin",
        Role = "admin",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
        CreatedAt = DateTime.UtcNow
    });
    context.SaveChanges();

    Console.WriteLine($"Created first admin '{email}'.");
    if (generated)
    {
        Console.WriteLine($"Generated password (shown once, change it after signing in): {password}");
    }
}

return 0;
