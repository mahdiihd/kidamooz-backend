namespace Kidamooz.Domain.Entities;

public class CreditLedger
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string? Ref { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public AppUser? User { get; set; }
}

public static class CreditLedgerKinds
{
    public const string BazaarPurchase = "bazaar_purchase";
    public const string AdminGrant = "admin_grant";
    public const string AiCoverSpend = "ai_cover_spend";
}
