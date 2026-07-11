namespace Kidamooz.Infrastructure.Storage;

public static class LiaraConfiguration
{
    public static LiaraSettings Load(IConfiguration configuration)
    {
        var settings = configuration.GetSection("Liara").Get<LiaraSettings>()
            ?? throw new InvalidOperationException("Liara settings are required");

        ApplyEnvironmentOverrides(settings);
        settings.PublicBaseUrl = ResolvePublicBaseUrl(settings);
        return settings;
    }

    public static string ResolvePublicBaseUrl(LiaraSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.PublicBaseUrl))
            return settings.PublicBaseUrl.TrimEnd('/');

        if (string.IsNullOrWhiteSpace(settings.BucketName) || string.IsNullOrWhiteSpace(settings.EndpointUrl))
            return string.Empty;

        var host = new Uri(settings.EndpointUrl).Host;
        return $"http://{settings.BucketName}.{host}";
    }

    private static void ApplyEnvironmentOverrides(LiaraSettings settings)
    {
        settings.EndpointUrl = FirstNonEmpty(
            Environment.GetEnvironmentVariable("LIARA_ENDPOINT_URL"),
            Environment.GetEnvironmentVariable("LIARA_ENDPOINT"),
            settings.EndpointUrl);

        settings.AccessKey = FirstNonEmpty(
            Environment.GetEnvironmentVariable("LIARA_ACCESS_KEY"),
            settings.AccessKey);

        settings.SecretKey = FirstNonEmpty(
            Environment.GetEnvironmentVariable("LIARA_SECRET_KEY"),
            settings.SecretKey);

        settings.BucketName = FirstNonEmpty(
            Environment.GetEnvironmentVariable("BUCKET_NAME"),
            Environment.GetEnvironmentVariable("LIARA_BUCKET_NAME"),
            settings.BucketName);

        settings.PublicBaseUrl = FirstNonEmpty(
            Environment.GetEnvironmentVariable("LIARA_PUBLIC_BASE_URL"),
            settings.PublicBaseUrl);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }
}
