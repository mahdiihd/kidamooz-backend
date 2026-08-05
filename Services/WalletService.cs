using System.Security.Cryptography;
using System.Text;
using Kidamooz.Data;
using Kidamooz.Domain.Entities;
using Kidamooz.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Kidamooz.Services;

public class WalletService(
    AppDbContext db,
    IHostEnvironment environment,
    IConfiguration configuration,
    ILogger<WalletService> logger) : IWalletService
{
    public async Task<MemberWalletDto> GetWalletAsync(string userId, CancellationToken ct = default)
    {
        var user = await db.AppUsers.AsNoTracking().FirstAsync(x => x.Id == userId, ct);
        return ToDto(user);
    }

    public CoverEntitlement ResolveCreateEntitlement(AppUser user)
    {
        if (!user.FreeAiCoverUsed)
            return new CoverEntitlement(CoverEntitlementKind.Free, null);

        return ResolvePaidEntitlement(user);
    }

    public CoverEntitlement ResolvePaidEntitlement(AppUser user)
    {
        var isPlus = MemberEngagementService.IsPlusActive(user);
        if (!isPlus)
            return new CoverEntitlement(CoverEntitlementKind.None, "need_plus");

        if (user.CreditBalance < CoverCreditPricing.CoverPriceTomans)
            return new CoverEntitlement(CoverEntitlementKind.None, "charge_credit");

        return new CoverEntitlement(CoverEntitlementKind.Paid, null);
    }

    public async Task MarkFreeCoverUsedAsync(string userId, CancellationToken ct = default)
    {
        var user = await db.AppUsers.FirstAsync(x => x.Id == userId, ct);
        if (user.FreeAiCoverUsed)
            return;
        user.FreeAiCoverUsed = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task ChargeCoverAsync(string userId, Guid draftId, CancellationToken ct = default)
    {
        var user = await db.AppUsers.FirstAsync(x => x.Id == userId, ct);
        if (user.CreditBalance < CoverCreditPricing.CoverPriceTomans)
            throw new InvalidOperationException("اعتبار کافی برای کاور هوش مصنوعی نیست.");

        user.CreditBalance -= CoverCreditPricing.CoverPriceTomans;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        db.CreditLedgers.Add(new CreditLedger
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = -CoverCreditPricing.CoverPriceTomans,
            Kind = CreditLedgerKinds.AiCoverSpend,
            Ref = $"cover:{draftId:N}",
            Note = "کسر بابت کاور هوش مصنوعی",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<MemberWalletDto> ConfirmBazaarPurchaseAsync(
        string userId,
        ConfirmBazaarPurchaseRequestDto request,
        CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentException("درخواست خرید نامعتبر است.");

        var productId = (request.ProductId ?? string.Empty).Trim();
        var purchaseToken = (request.PurchaseToken ?? string.Empty).Trim();
        var orderId = (request.OrderId ?? string.Empty).Trim();
        var amount = CoverCreditPricing.AmountForProduct(productId)
            ?? throw new ArgumentException("بسته اعتباری نامعتبر است.");

        if (string.IsNullOrWhiteSpace(purchaseToken))
            throw new ArgumentException("توکن خرید الزامی است.");

        var refKey = $"bazaar:{purchaseToken}";
        if (await db.CreditLedgers.AsNoTracking().AnyAsync(x => x.Ref == refKey, ct))
        {
            var existing = await db.AppUsers.AsNoTracking().FirstAsync(x => x.Id == userId, ct);
            return ToDto(existing);
        }

        EnsureBazaarPurchaseAllowed(request);

        var user = await db.AppUsers.FirstAsync(x => x.Id == userId, ct);
        user.CreditBalance += amount;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        db.CreditLedgers.Add(new CreditLedger
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = amount,
            Kind = CreditLedgerKinds.BazaarPurchase,
            Ref = refKey,
            Note = string.IsNullOrWhiteSpace(orderId) ? productId : $"{productId}:{orderId}",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Bazaar credit granted user={UserId} product={ProductId} amount={Amount}",
            userId,
            productId,
            amount);
        return ToDto(user);
    }

    public async Task<MemberWalletDto> AdminGrantAsync(
        string userId,
        long amountTomans,
        string? note,
        CancellationToken ct = default)
    {
        if (amountTomans == 0)
            throw new ArgumentException("مبلغ اعتبار نمی‌تواند صفر باشد.");

        var user = await db.AppUsers.FirstOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new KeyNotFoundException("کاربر یافت نشد.");

        user.CreditBalance += amountTomans;
        if (user.CreditBalance < 0)
            user.CreditBalance = 0;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        db.CreditLedgers.Add(new CreditLedger
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = amountTomans,
            Kind = CreditLedgerKinds.AdminGrant,
            Ref = $"admin:{Guid.NewGuid():N}",
            Note = string.IsNullOrWhiteSpace(note) ? "شارژ توسط ادمین" : note.Trim()[..Math.Min(note.Trim().Length, 500)],
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return ToDto(user);
    }

    private void EnsureBazaarPurchaseAllowed(ConfirmBazaarPurchaseRequestDto request)
    {
        var rsaKey = configuration["Bazaar:RsaPublicKey"]
            ?? Environment.GetEnvironmentVariable("BAZAAR_RSA_PUBLIC_KEY")
            ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(rsaKey) &&
            !string.IsNullOrWhiteSpace(request.PurchaseData) &&
            !string.IsNullOrWhiteSpace(request.DataSignature))
        {
            if (!VerifyRsaSignature(rsaKey, request.PurchaseData!, request.DataSignature!))
                throw new InvalidOperationException("امضای خرید بازار معتبر نیست.");
            return;
        }

        if (environment.IsDevelopment())
            return;

        var allowUnsigned = string.Equals(
            Environment.GetEnvironmentVariable("BAZAAR_ALLOW_UNSIGNED"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (allowUnsigned)
            return;

        throw new InvalidOperationException(
            "تأیید خرید بازار پیکربندی نشده است. کلید RSA بازار را تنظیم کنید.");
    }

    private static bool VerifyRsaSignature(string publicKeyPem, string data, string signatureBase64)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem.ToCharArray());
            var payload = Encoding.UTF8.GetBytes(data);
            var signature = Convert.FromBase64String(signatureBase64);
            return rsa.VerifyData(payload, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    private static MemberWalletDto ToDto(AppUser user)
    {
        var isPlus = MemberEngagementService.IsPlusActive(user);
        return new MemberWalletDto(
            user.CreditBalance,
            isPlus,
            user.FreeAiCoverUsed,
            CoverCreditPricing.CoverPriceTomans,
            CoverCreditPricing.Packages.ToList());
    }
}
