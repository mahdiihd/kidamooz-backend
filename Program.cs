using System.Diagnostics;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Kidamooz.Data;
using Kidamooz.Infrastructure.Auth;
using Kidamooz.Infrastructure.Storage;
using Kidamooz.Repositories;
using Kidamooz.Repositories.Interfaces;
using Kidamooz.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

if (AdminUserCli.IsCommand(args))
{
    Environment.Exit(await AdminUserCli.RunAsync(args));
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<LiaraSettings>(builder.Configuration.GetSection("Liara"));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt settings are required");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var liara = builder.Configuration.GetSection("Liara").Get<LiaraSettings>()
        ?? throw new InvalidOperationException("Liara settings are required");

    var config = new AmazonS3Config
    {
        ServiceURL = liara.EndpointUrl,
        ForcePathStyle = true
    };

    var credentials = new BasicAWSCredentials(liara.AccessKey, liara.SecretKey);
    return new AmazonS3Client(credentials, config);
});

builder.Services.AddSingleton<LiaraSettings>(sp =>
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LiaraSettings>>().Value);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IStoryRepository, StoryRepository>();
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAudienceRepository, AudienceRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMediaStorageService, LiaraMediaStorageService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IAudienceService, AudienceService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IStoryService, StoryService>();
builder.Services.AddScoped<IPublicService, PublicService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

builder.Services.AddAuthorization();

var adminOrigins = builder.Configuration.GetSection("Cors:AdminOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.WithOrigins(adminOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Kidamooz API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Admin");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(OpenSwaggerInBrowser);
}

app.Run();

void OpenSwaggerInBrowser()
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
