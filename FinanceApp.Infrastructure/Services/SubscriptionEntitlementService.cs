using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Services;

public sealed class SubscriptionEntitlementService : ISubscriptionEntitlementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<SubscriptionPurchaseRecord> _purchaseRecords;

    public SubscriptionEntitlementService(
        UserManager<ApplicationUser> userManager,
        IRepository<SubscriptionPurchaseRecord> purchaseRecords)
    {
        _userManager = userManager;
        _purchaseRecords = purchaseRecords;
    }

    public async Task<SubscriptionEntitlementResult> ApplyVerifiedEntitlementAsync(
        string userId,
        SubscriptionPlan plan,
        DateTimeOffset expiresAtUtc,
        SubscriptionBillingSource source,
        string externalTransactionId,
        string productId,
        string? notes,
        string? googlePurchaseToken = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return new SubscriptionEntitlementResult(false, "User not found.");

        user.SubscriptionPlan = plan;
        user.SubscriptionAssignedAt = DateTimeOffset.UtcNow;
        user.SubscriptionExpiresAtUtc = expiresAtUtc;
        user.SubscriptionBillingSource = source;

        if (source == SubscriptionBillingSource.Apple)
            user.AppleOriginalTransactionId = externalTransactionId;
        if (source == SubscriptionBillingSource.Google)
            user.GooglePurchaseToken = googlePurchaseToken ?? externalTransactionId;

        if (source == SubscriptionBillingSource.Web)
        {
            user.StripeSubscriptionId = externalTransactionId;
            if (!string.IsNullOrWhiteSpace(googlePurchaseToken))
                user.StripeCustomerId = googlePurchaseToken;
        }

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
            return new SubscriptionEntitlementResult(false, string.Join("; ", update.Errors.Select(e => e.Description)));

        await _purchaseRecords.AddAsync(new SubscriptionPurchaseRecord
        {
            UserId = userId,
            BillingSource = source,
            ProductId = productId,
            ExternalTransactionId = externalTransactionId,
            Plan = plan,
            ExpiresAtUtc = expiresAtUtc,
            Notes = notes
        });
        await _purchaseRecords.SaveChangesAsync();

        return new SubscriptionEntitlementResult(true, null);
    }

    public async Task SyncExpiredSubscriptionAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        if (user.SubscriptionPlan == SubscriptionPlan.Free)
            return;

        if (user.SubscriptionExpiresAtUtc == null || user.SubscriptionExpiresAtUtc > DateTimeOffset.UtcNow)
            return;

        user.SubscriptionPlan = SubscriptionPlan.Free;
        user.SubscriptionBillingSource = SubscriptionBillingSource.None;
        user.SubscriptionExpiresAtUtc = null;
        user.AppleOriginalTransactionId = null;
        user.GooglePurchaseToken = null;
        user.StripeCustomerId = null;
        user.StripeSubscriptionId = null;
        user.SubscriptionAssignedAt = DateTimeOffset.UtcNow;

        await _userManager.UpdateAsync(user);
    }

    public async Task<string?> FindUserIdByStripeCustomerIdAsync(
        string stripeCustomerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stripeCustomerId))
            return null;

        return await _userManager.Users
            .AsNoTracking()
            .Where(u => u.StripeCustomerId == stripeCustomerId)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string?> FindUserIdByAppleOriginalTransactionIdAsync(
        string originalTransactionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(originalTransactionId))
            return null;

        return await _userManager.Users
            .AsNoTracking()
            .Where(u => u.AppleOriginalTransactionId == originalTransactionId)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string?> FindUserIdByGooglePurchaseTokenAsync(
        string purchaseToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(purchaseToken))
            return null;

        return await _userManager.Users
            .AsNoTracking()
            .Where(u => u.GooglePurchaseToken == purchaseToken)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task RevokeStoreSubscriptionAsync(
        string userId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return;

        if (user.SubscriptionPlan == SubscriptionPlan.Free &&
            user.SubscriptionBillingSource == SubscriptionBillingSource.None)
            return;

        var previousPlan = user.SubscriptionPlan;
        var previousSource = user.SubscriptionBillingSource;

        user.SubscriptionPlan = SubscriptionPlan.Free;
        user.SubscriptionBillingSource = SubscriptionBillingSource.None;
        user.SubscriptionExpiresAtUtc = null;
        user.AppleOriginalTransactionId = null;
        user.GooglePurchaseToken = null;
        user.StripeCustomerId = null;
        user.StripeSubscriptionId = null;
        user.SubscriptionAssignedAt = DateTimeOffset.UtcNow;

        await _userManager.UpdateAsync(user);

        await _purchaseRecords.AddAsync(new SubscriptionPurchaseRecord
        {
            UserId = userId,
            BillingSource = previousSource,
            ProductId = "revoked",
            ExternalTransactionId = $"revoke-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            Plan = previousPlan,
            ExpiresAtUtc = null,
            Notes = notes
        });
        await _purchaseRecords.SaveChangesAsync();
    }
}
