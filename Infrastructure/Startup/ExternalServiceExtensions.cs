using Kidamooz.Infrastructure.Ai;
using Kidamooz.Infrastructure.Push;

namespace Kidamooz.Infrastructure.Startup;

public static class ExternalServiceExtensions
{
    public static void AddKidamoozExternalServices(this WebApplicationBuilder builder)
    {
        var firebaseSettings = builder.Configuration.GetSection("Firebase").Get<FirebaseSettings>() ?? new FirebaseSettings();
        ApplyFirebaseEnvOverrides(firebaseSettings);
        builder.Services.AddSingleton(firebaseSettings);
        builder.Services.AddHttpClient("firebase");
        builder.Services.AddSingleton<IPushNotificationSender, FirebasePushNotificationSender>();

        var geminiSettings = builder.Configuration.GetSection("Gemini").Get<GeminiSettings>() ?? new GeminiSettings();
        ApplyGeminiEnvOverrides(geminiSettings);
        builder.Services.AddSingleton(geminiSettings);
        builder.Services.AddHttpClient("gemini", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
        });
        builder.Services.AddSingleton<IGeminiStoryClient, GeminiStoryClient>();
        builder.Services.AddSingleton<ICoverImageGenerator, GeminiCoverImageGenerator>();
        builder.Services.Configure<NarrationSettings>(builder.Configuration.GetSection(NarrationSettings.SectionName));
        builder.Services.AddSingleton<IAudioNarrationService, EdgeTtsAudioNarrationService>();
    }

    private static void ApplyFirebaseEnvOverrides(FirebaseSettings settings)
    {
        settings.ProjectId = Environment.GetEnvironmentVariable("Firebase__ProjectId")
            ?? Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID")
            ?? settings.ProjectId;
        settings.ClientEmail = Environment.GetEnvironmentVariable("Firebase__ClientEmail")
            ?? Environment.GetEnvironmentVariable("FIREBASE_CLIENT_EMAIL")
            ?? settings.ClientEmail;
        settings.PrivateKey = Environment.GetEnvironmentVariable("Firebase__PrivateKey")
            ?? Environment.GetEnvironmentVariable("FIREBASE_PRIVATE_KEY")
            ?? settings.PrivateKey;
    }

    private static void ApplyGeminiEnvOverrides(GeminiSettings settings)
    {
        settings.ApiKey = Environment.GetEnvironmentVariable("Gemini__ApiKey")
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? settings.ApiKey;
        settings.Model = Environment.GetEnvironmentVariable("Gemini__Model")
            ?? Environment.GetEnvironmentVariable("GEMINI_MODEL")
            ?? settings.Model;
        settings.CoverImageModel = Environment.GetEnvironmentVariable("Gemini__CoverImageModel")
            ?? Environment.GetEnvironmentVariable("GEMINI_COVER_IMAGE_MODEL")
            ?? settings.CoverImageModel;
        settings.BaseUrl = Environment.GetEnvironmentVariable("Gemini__BaseUrl")
            ?? Environment.GetEnvironmentVariable("GEMINI_BASE_URL")
            ?? settings.BaseUrl;
    }
}
