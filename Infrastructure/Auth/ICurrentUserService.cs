namespace Kidamooz.Infrastructure.Auth;

public interface ICurrentUserService
{
    string? Email { get; }
    Guid? UserId { get; }
}
