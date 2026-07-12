namespace Kidamooz.Infrastructure.Push;

public class FirebaseSettings
{
    public string ProjectId { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ProjectId) &&
        !string.IsNullOrWhiteSpace(ClientEmail) &&
        !string.IsNullOrWhiteSpace(PrivateKey);
}
