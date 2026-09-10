using System.Diagnostics;
using Kidamooz.Data;

namespace Kidamooz.Infrastructure.Startup;

public static class ApplicationLifecycleExtensions
{
    public static void InitializeKidamoozDatabaseOnStarted(this WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() => _ = InitializeDatabaseAsync(app));
    }

    public static void OpenKidamoozSwaggerOnStarted(this WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() => OpenSwaggerInBrowser(app));
    }

    private static async Task InitializeDatabaseAsync(WebApplication application)
    {
        try
        {
            await using var scope = application.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DbInitializer.InitializeAsync(db);
            application.Logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            application.Logger.LogError(ex, "Database initialization failed");
        }
    }

    private static void OpenSwaggerInBrowser(WebApplication app)
    {
        var swaggerUrl = app.Urls
            .Select(url => $"{url.TrimEnd('/')}/swagger")
            .FirstOrDefault(url => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            ?? $"{app.Urls.FirstOrDefault()?.TrimEnd('/') ?? "http://localhost:5042"}/swagger";

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = swaggerUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Could not open Swagger at {SwaggerUrl}", swaggerUrl);
        }
    }
}
