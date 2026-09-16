using Kidamooz.Data;
using Kidamooz.Infrastructure.Auth;
using Kidamooz.Infrastructure.Storage;
using Kidamooz.Repositories;
using Kidamooz.Repositories.Interfaces;
using Kidamooz.Services;
using Microsoft.EntityFrameworkCore;

namespace Kidamooz.Infrastructure.Startup;

public static class ApplicationServiceExtensions
{
    public static void AddKidamoozServices(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

        var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt settings are required");

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

        builder.Services.AddSingleton<JwtTokenService>();

        builder.AddKidamoozStorage();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
        builder.Services.AddScoped<IStoryRepository, StoryRepository>();
        builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
        builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        builder.Services.AddScoped<IAudienceRepository, AudienceRepository>();
        builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
        builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
        builder.Services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();

        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IAdminUserService, AdminUserService>();
        builder.Services.AddScoped<IAdminMemberService, AdminMemberService>();
        builder.Services.AddScoped<IAdminChallengeService, AdminChallengeService>();
        builder.Services.AddScoped<IAuditService, AuditService>();
        builder.Services.AddScoped<ICatalogService, CatalogService>();
        builder.Services.AddScoped<IDashboardService, DashboardService>();
        builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
        builder.Services.AddScoped<IDeviceService, DeviceService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<IMediaStorageService, LiaraMediaStorageService>();
        builder.Services.AddScoped<IMediaService, MediaService>();
        builder.Services.AddScoped<IAudienceService, AudienceService>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<IStoryService, StoryService>();
        builder.Services.AddScoped<IPublicService, PublicService>();
        builder.Services.AddScoped<IStoryDraftService, StoryDraftService>();
        builder.Services.AddScoped<IMemberAuthService, MemberAuthService>();
        builder.Services.AddScoped<MemberOtpService>();
        if (builder.Configuration["Otp:Provider"]?.Equals("faraz", StringComparison.OrdinalIgnoreCase) == true)
            builder.Services.AddHttpClient<IMemberOtpSender, FarazOtpSender>(client => client.Timeout = TimeSpan.FromSeconds(15))
                .RemoveAllLoggers();
        else
            builder.Services.AddHttpClient<IMemberOtpSender, SmsIrOtpSender>(client => client.Timeout = TimeSpan.FromSeconds(15))
                .RemoveAllLoggers();
        builder.Services.AddScoped<IMemberContext, MemberContext>();
        builder.Services.AddScoped<IChildProfileService, ChildProfileService>();
        builder.Services.AddScoped<IMemberFavoriteService, MemberFavoriteService>();
        builder.Services.AddScoped<IMemberEngagementService, MemberEngagementService>();
        builder.Services.AddScoped<IAdminStoryOfTheDayService, AdminStoryOfTheDayService>();

        builder.AddKidamoozExternalServices();
        builder.AddKidamoozApi(jwtSettings);
    }
}
