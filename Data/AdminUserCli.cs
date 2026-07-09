using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Kidamooz.Data;

public static class AdminUserCli
{
    public static bool IsCommand(string[] args) =>
        args.Length > 0 && args[0] is "create-admin" or "reset-admin-password";

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("Default"));

            await using var db = new AppDbContext(optionsBuilder.Options);
            await db.Database.MigrateAsync();

            return args[0] switch
            {
                "create-admin" => await CreateAdminAsync(db, args),
                "reset-admin-password" => await ResetPasswordAsync(db, args),
                _ => PrintUsage()
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"خطا: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> CreateAdminAsync(AppDbContext db, string[] args)
    {
        var options = ParseOptions(args[1..]);
        if (!options.TryGetValue("email", out var email) ||
            !options.TryGetValue("password", out var password) ||
            !options.TryGetValue("name", out var name))
        {
            return PrintUsage();
        }

        var role = options.GetValueOrDefault("role", "editor");
        await AdminUserProvisioner.CreateAsync(db, email, password, name, role);
        Console.WriteLine($"ادمین ساخته شد: {email.Trim().ToLowerInvariant()} ({role})");
        return 0;
    }

    private static async Task<int> ResetPasswordAsync(AppDbContext db, string[] args)
    {
        var options = ParseOptions(args[1..]);
        if (!options.TryGetValue("email", out var email) ||
            !options.TryGetValue("password", out var password))
        {
            return PrintUsage();
        }

        await AdminUserProvisioner.ResetPasswordAsync(db, email, password);
        Console.WriteLine($"رمز عبور به‌روز شد: {email.Trim().ToLowerInvariant()}");
        return 0;
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
                continue;

            var key = args[i][2..];
            var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true";

            result[key] = value;
        }

        return result;
    }

    private static int PrintUsage()
    {
        Console.WriteLine("""
        استفاده:

          dotnet run -- create-admin --email admin@kidamooz.com --password "YourPass123" --name "مدیر سیستم" --role admin
          dotnet run -- reset-admin-password --email admin@kidamooz.com --password "NewPass123"

        نقش‌ها: admin | editor
        """);
        return 1;
    }
}
