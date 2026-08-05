using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;

namespace Kidamooz.Services;

public static class CoverCreditPricing
{
    public const long CoverPriceTomans = 50_000;

    public static readonly CreditPackageDto[] Packages =
    [
        new("credit_100k", 100_000, "بسته ۱۰۰ هزار تومانی"),
        new("credit_250k", 250_000, "بسته ۲۵۰ هزار تومانی"),
        new("credit_500k", 500_000, "بسته ۵۰۰ هزار تومانی")
    ];

    public static long? AmountForProduct(string productId) =>
        Packages.FirstOrDefault(p =>
            string.Equals(p.ProductId, productId, StringComparison.OrdinalIgnoreCase))?.AmountTomans;
}

public enum CoverEntitlementKind
{
    None,
    Free,
    Paid
}

public readonly record struct CoverEntitlement(CoverEntitlementKind Kind, string? UpsellCode);

public interface IWalletService
{
    Task<MemberWalletDto> GetWalletAsync(string userId, CancellationToken ct = default);
    CoverEntitlement ResolveCreateEntitlement(AppUser user);
    CoverEntitlement ResolvePaidEntitlement(AppUser user);
    Task MarkFreeCoverUsedAsync(string userId, CancellationToken ct = default);
    Task ChargeCoverAsync(string userId, Guid draftId, CancellationToken ct = default);
    Task<MemberWalletDto> ConfirmBazaarPurchaseAsync(
        string userId,
        ConfirmBazaarPurchaseRequestDto request,
        CancellationToken ct = default);
    Task<MemberWalletDto> AdminGrantAsync(
        string userId,
        long amountTomans,
        string? note,
        CancellationToken ct = default);
}
