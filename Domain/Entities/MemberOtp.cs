namespace Kidamooz.Domain.Entities;

public class MemberOtp
{
    public string Mobile { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset LastSentAt { get; set; }
    public DateTimeOffset WindowStartedAt { get; set; }
    public int SendCount { get; set; }
    public int Attempts { get; set; }
}
