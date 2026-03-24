namespace Tungsten.Api.Common.Services;

public static class EmailTemplates
{
    public static (string subject, string htmlBody, string textBody) Welcome(string adminName, string companyName, string loginUrl)
    {
        var subject = $"Welcome to auditraks, {adminName}!";
        var htmlBody = $"""
            <div style="font-family: system-ui, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px;">
                <h1 style="color: #4f46e5; font-size: 24px; margin-bottom: 8px;">Welcome to auditraks</h1>
                <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                    Hi {adminName},
                </p>
                <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                    Your organization <strong>{companyName}</strong> has been set up on auditraks.
                    You have a <strong>60-day free trial</strong> to explore the platform.
                </p>
                <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                    Sign in with Google to get started:
                </p>
                <a href="{loginUrl}" style="display: inline-block; background: #4f46e5; color: white; padding: 12px 28px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 15px; margin: 16px 0;">
                    Sign In to auditraks
                </a>
                <p style="color: #64748b; font-size: 14px; margin-top: 32px;">
                    As your organization's admin, you can invite team members from the Admin Dashboard.
                </p>
                <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 32px 0;" />
                <p style="color: #94a3b8; font-size: 12px;">
                    &copy; 2026 auditraks. Tungsten supply chain compliance, automated.
                </p>
            </div>
            """;
        var textBody = $"Welcome to auditraks, {adminName}!\n\nYour organization {companyName} has been set up on auditraks. You have a 60-day free trial to explore the platform.\n\nSign in to get started: {loginUrl}\n\nAs your organization's admin, you can invite team members from the Admin Dashboard.\n\n© 2026 auditraks.";
        return (subject, htmlBody, textBody);
    }

    public static (string subject, string htmlBody, string textBody) TrialEndingSoon(string adminName, string companyName, int daysRemaining)
    {
        var subject = $"Your auditraks trial ends in {daysRemaining} days";
        var htmlBody = $"""
            <div style="font-family: system-ui, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px;">
                <h1 style="color: #4f46e5; font-size: 24px; margin-bottom: 8px;">Trial ending soon</h1>
                <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                    Hi {adminName},
                </p>
                <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                    Your free trial for <strong>{companyName}</strong> ends in <strong>{daysRemaining} days</strong>.
                    After that, your subscription will begin at <strong>$249/month</strong>.
                </p>
                <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                    No action needed — your card on file will be charged automatically.
                    If you'd like to cancel, you can do so from the Admin Dashboard.
                </p>
                <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 32px 0;" />
                <p style="color: #94a3b8; font-size: 12px;">
                    &copy; 2026 auditraks. Tungsten supply chain compliance, automated.
                </p>
            </div>
            """;
        var textBody = $"Hi {adminName},\n\nYour free trial for {companyName} ends in {daysRemaining} days. After that, your subscription will begin at $249/month.\n\nNo action needed — your card on file will be charged automatically. If you'd like to cancel, you can do so from the Admin Dashboard.\n\n© 2026 auditraks.";
        return (subject, htmlBody, textBody);
    }

    public static (string subject, string htmlBody, string textBody) PaymentFailed(string adminName, string companyName)
    {
        var subject = "Action required: Payment failed for auditraks";
        var htmlBody = $"""
            <div style="font-family: system-ui, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px;">
                <h1 style="color: #dc2626; font-size: 24px; margin-bottom: 8px;">Payment failed</h1>
                <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                    Hi {adminName},
                </p>
                <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                    We were unable to process the payment for <strong>{companyName}</strong>'s auditraks subscription.
                    Your account has been temporarily suspended.
                </p>
                <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                    Please update your payment method from the Admin Dashboard to restore access.
                </p>
                <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 32px 0;" />
                <p style="color: #94a3b8; font-size: 12px;">
                    &copy; 2026 auditraks. Tungsten supply chain compliance, automated.
                </p>
            </div>
            """;
        var textBody = $"Hi {adminName},\n\nWe were unable to process the payment for {companyName}'s auditraks subscription. Your account has been temporarily suspended.\n\nPlease update your payment method from the Admin Dashboard to restore access.\n\n© 2026 auditraks.";
        return (subject, htmlBody, textBody);
    }
}
