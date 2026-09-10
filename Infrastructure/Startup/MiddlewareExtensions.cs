using Microsoft.AspNetCore.Diagnostics;

namespace Kidamooz.Infrastructure.Startup;

public static class MiddlewareExtensions
{
    public static void UseKidamoozExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var ex = feature?.Error;
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    message = ex?.Message ?? "خطای داخلی سرور",
                    detail = ex?.GetType().Name
                });
            });
        });
    }

    public static void UseKidamoozSecurityHeaders(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";

                var path = context.Request.Path.Value ?? string.Empty;
                if (!path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
                {
                    headers["Content-Security-Policy"] =
                        "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
                }

                return Task.CompletedTask;
            });

            await next();
        });
    }
}
