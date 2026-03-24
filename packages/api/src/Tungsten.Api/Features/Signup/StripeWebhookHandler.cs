using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Signup;

public class StripeWebhookHandler(AppDbContext db, ILogger<StripeWebhookHandler> logger, IEmailService emailService, IConfiguration config)
{
    public async Task HandleCheckoutCompleted(
        string customerId, string subscriptionId,
        string companyName, string adminName, string adminEmail, string plan = "PRO")
    {
        if (await db.Users.AnyAsync(u => u.Email == adminEmail))
        {
            logger.LogWarning("Checkout completed but email {Email} already exists, skipping", adminEmail);
            return;
        }

        var basePrefix = GenerateSchemaPrefix(companyName);
        var prefix = basePrefix;
        var suffix = 2;
        while (await db.Tenants.AnyAsync(t => t.SchemaPrefix == prefix))
        {
            prefix = $"{basePrefix}_{suffix}";
            suffix++;
        }

        var (maxBatches, maxUsers) = PlanConfiguration.GetLimits(plan);
        var tenant = new TenantEntity
        {
            Id = Guid.NewGuid(),
            Name = companyName,
            SchemaPrefix = prefix,
            Status = "TRIAL",
            StripeCustomerId = customerId,
            StripeSubscriptionId = subscriptionId,
            PlanName = plan,
            MaxBatches = maxBatches,
            MaxUsers = maxUsers,
            TrialEndsAt = DateTime.UtcNow.AddDays(60),
            CreatedAt = DateTime.UtcNow,
        };

        var adminUser = new UserEntity
        {
            Id = Guid.NewGuid(),
            Auth0Sub = $"pending|{Guid.NewGuid()}",
            Email = adminEmail,
            DisplayName = adminName,
            Role = "TENANT_ADMIN",
            TenantId = tenant.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.Tenants.Add(tenant);
        db.Users.Add(adminUser);
        await db.SaveChangesAsync();

        logger.LogInformation("Tenant '{Name}' provisioned via Stripe checkout for {Email}", companyName, adminEmail);

        var baseUrl = config["BaseUrl"] ?? "https://accutrac-web.onrender.com";
        var (subject, htmlBody, textBody) = EmailTemplates.Welcome(adminName, companyName, $"{baseUrl}/login");
        try { await emailService.SendAsync(adminEmail, subject, htmlBody, textBody, CancellationToken.None); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to send welcome email to {Email}", adminEmail); }
    }

    public async Task HandleInvoicePaid(string subscriptionId)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.StripeSubscriptionId == subscriptionId);
        if (tenant is null)
        {
            logger.LogWarning("invoice.paid: no tenant found for subscription {SubscriptionId}", subscriptionId);
            return;
        }
        if (tenant.Status is "TRIAL" or "SUSPENDED")
        {
            tenant.Status = "ACTIVE";
            await db.SaveChangesAsync();
            logger.LogInformation("Tenant '{Name}' activated via invoice.paid", tenant.Name);
        }
    }

    public async Task HandlePaymentFailed(string subscriptionId)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.StripeSubscriptionId == subscriptionId);
        if (tenant is null)
        {
            logger.LogWarning("invoice.payment_failed: no tenant found for subscription {SubscriptionId}", subscriptionId);
            return;
        }
        tenant.Status = "SUSPENDED";
        await db.SaveChangesAsync();
        logger.LogWarning("Tenant '{Name}' suspended due to payment failure", tenant.Name);

        var admin = await db.Users.FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Role == "TENANT_ADMIN");
        if (admin is not null)
        {
            var (subject, htmlBody, textBody) = EmailTemplates.PaymentFailed(admin.DisplayName, tenant.Name);
            try { await emailService.SendAsync(admin.Email, subject, htmlBody, textBody, CancellationToken.None); }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to send payment failed email"); }
        }
    }

    public async Task HandleSubscriptionDeleted(string subscriptionId)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.StripeSubscriptionId == subscriptionId);
        if (tenant is null)
        {
            logger.LogWarning("customer.subscription.deleted: no tenant found for subscription {SubscriptionId}", subscriptionId);
            return;
        }
        tenant.Status = "CANCELLED";
        await db.SaveChangesAsync();
        logger.LogWarning("Tenant '{Name}' cancelled via subscription deletion", tenant.Name);
    }

    private static string GenerateSchemaPrefix(string name)
    {
        var prefix = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
        return prefix.Length > 50 ? prefix[..50] : prefix;
    }
}
