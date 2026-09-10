using Amazon.Runtime;
using Amazon.S3;
using Kidamooz.Infrastructure.Storage;

namespace Kidamooz.Infrastructure.Startup;

public static class StorageServiceExtensions
{
    public static void AddKidamoozStorage(this WebApplicationBuilder builder)
    {
        var liaraSettings = LiaraConfiguration.Load(builder.Configuration);

        if (builder.Environment.IsProduction() &&
            (string.IsNullOrWhiteSpace(liaraSettings.AccessKey) ||
             string.IsNullOrWhiteSpace(liaraSettings.SecretKey) ||
             liaraSettings.AccessKey.Contains("YOUR_", StringComparison.Ordinal) ||
             liaraSettings.SecretKey.Contains("YOUR_", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Liara storage credentials are not configured for production.");
        }

        builder.Services.AddSingleton<IAmazonS3>(_ =>
        {
            var liara = liaraSettings;

            var config = new AmazonS3Config
            {
                ServiceURL = liara.EndpointUrl,
                ForcePathStyle = true,
                AuthenticationRegion = "us-east-1",
                Timeout = TimeSpan.FromSeconds(30),
                MaxErrorRetry = 2,
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            };

            var credentials = new BasicAWSCredentials(liara.AccessKey, liara.SecretKey);
            return new AmazonS3Client(credentials, config);
        });

        builder.Services.AddSingleton(liaraSettings);
        builder.Services.AddSingleton<IMediaUrlNormalizer, MediaUrlNormalizer>();
    }
}
