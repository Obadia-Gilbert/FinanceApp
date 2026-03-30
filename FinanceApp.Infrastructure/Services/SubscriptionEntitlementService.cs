using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;

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
        {
            user.GooglePurchaseToken = googlePurchaseToken ?? externalTransactionId;
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
        user.SubscriptionAssignedAt = DateTimeOffset.UtcNow;

        await _userManager.UpdateAsync(user);
    }
}
