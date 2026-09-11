using Kidamooz.Data;
using Kidamooz.Infrastructure.Startup;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

if (AdminUserCli.IsCommand(args))
{
    Environment.Exit(await AdminUserCli.RunAsync(args));
}

builder.AddKidamoozServices();

var app = builder.Build();

app.UseKidamoozExceptionHandler();
app.InitializeKidamoozDatabaseOnStarted();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseKidamoozSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseCors("Admin");
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.OpenKidamoozSwaggerOnStarted();
}

app.Run();
