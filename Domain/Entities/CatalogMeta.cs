namespace Kidamooz.Domain.Entities;

public class CatalogMeta
{
    public int Id { get; set; } = 1;
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
