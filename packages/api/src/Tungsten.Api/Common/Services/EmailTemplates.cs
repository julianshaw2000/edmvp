namespace Tungsten.Api.Common.Services;

public static class EmailTemplates
{
    public static (string subject, string htmlBody, string textBody) AccountSetup(string adminName, string companyName, string setupUrl)
    {
        var subject = $"Complete your auditraks account setup";
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
                    Set up your password to get started:
                </p>
                <a href="{setupUrl}" style="display: inline-block; background: #4f46e5; color: white; padding: 12px 28px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 15px; margin: 16px 0;">
                    Set up your password
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
        var textBody = $"Welcome to auditraks, {adminName}!\n\nYour organization {companyName} has been set up on auditraks. You have a 60-day free trial to explore the platform.\n\nSet up your password to get started: {setupUrl}\n\nAs your organization's admin, you can invite team members from the Admin Dashboard.\n\n© 2026 auditraks.";
        return (subject, htmlBody, textBody);
    }

    public static (string subject, string htmlBody, string textBody) AccountReady(string adminName, string companyName, string loginUrl)
    {
        var subject = "Your auditraks account is ready";
        var htmlBody = $"""
            <div style="font-family: system-ui, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px;">
                <h1 style="color: #4f46e5; font-size: 24px; margin-bottom: 8px;">You're all set!</h1>
                <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                    Hi {adminName},
                </p>
                <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                    Your auditraks account for <strong>{companyName}</strong> is now fully set up.
                    Your 60-day free trial has started.
                </p>
                <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                    Here's what you can do next:
                </p>
                <ul style="color: #334155; font-size: 15px; line-height: 1.8; padding-left: 20px;">
                    <li>Invite your team from the Admin Dashboard</li>
                    <li>Create your first mineral batch</li>
                    <li>Set up your supply chain tracking</li>
                </ul>
                <a href="{loginUrl}" style="display: inline-block; background: #4f46e5; color: white; padding: 12px 28px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 15px; margin: 16px 0;">
                    Go to your dashboard
                </a>
                <p style="color: #64748b; font-size: 14px; margin-top: 32px;">
                    If you have any questions, reply to this email or contact support@auditraks.com.
                </p>
                <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 32px 0;" />
                <p style="color: #94a3b8; font-size: 12px;">
                    &copy; 2026 auditraks. Tungsten supply chain compliance, automated.
                </p>
            </div>
            """;
        var textBody = $"Hi {adminName},\n\nYour auditraks account for {companyName} is now fully set up. Your 60-day free trial has started.\n\nHere's what you can do next:\n- Invite your team from the Admin Dashboard\n- Create your first mineral batch\n- Set up your supply chain tracking\n\nGo to your dashboard: {loginUrl}\n\nIf you have any questions, reply to this email or contact support@auditraks.com.\n\n© 2026 auditraks.";
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

    public static (string subject, string htmlBody, string textBody) PassportShared(
        string batchNumber, string senderName, string shareUrl, string? message)
    {
        var subject = $"Material Passport shared with you — {batchNumber}";
        var messageBlock = string.IsNullOrWhiteSpace(message)
            ? ""
            : $"""<div style="background: #f8fafc; border-left: 3px solid #4f46e5; padding: 12px 16px; margin: 16px 0; border-radius: 0 8px 8px 0;"><p style="margin: 0; font-size: 14px; color: #334155;">{message}</p></div>""";
        var htmlBody = $"""
            <div style="font-family: system-ui, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px;">
                <h1 style="color: #4f46e5; font-size: 24px; margin-bottom: 8px;">Material Passport</h1>
                <p style="color: #64748b; font-size: 14px; margin-bottom: 24px;">{senderName} has shared a Material Passport with you for batch <strong>{batchNumber}</strong>.</p>
                {messageBlock}
                <a href="{shareUrl}" style="display: inline-block; background: #4f46e5; color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 8px; font-size: 14px; font-weight: 600; margin: 16px 0;">View Material Passport</a>
                <p style="color: #94a3b8; font-size: 12px; margin-top: 24px;">This link expires in 30 days.</p>
                <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;" />
                <p style="color: #94a3b8; font-size: 12px;">&copy; 2026 auditraks. Tungsten supply chain compliance, automated.</p>
            </div>
            """;
        var textBody = $"""
            Material Passport — {batchNumber}

            {senderName} has shared a Material Passport with you for batch {batchNumber}.

            {(string.IsNullOrWhiteSpace(message) ? "" : $"Message: {message}\n")}
            View it here: {shareUrl}

            This link expires in 30 days.
            """;
        return (subject, htmlBody, textBody);
    }

    public static (string subject, string htmlBody, string textBody) BatchInactivityReminder(
        string supplierName, string batchNumber, int daysSinceLastEvent)
    {
        var subject = $"Your batch {batchNumber} needs attention";
        var htmlBody = $"""
            <div style="font-family: system-ui, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px;">
                <h1 style="color: #4f46e5; font-size: 24px; margin-bottom: 8px;">Batch Update Needed</h1>
                <p style="color: #64748b; font-size: 14px; margin-bottom: 24px;">Hi {supplierName}, your batch <strong>{batchNumber}</strong> has had no custody events for <strong>{daysSinceLastEvent} days</strong>.</p>
                <p style="color: #334155; font-size: 14px; margin-bottom: 24px;">To maintain compliance and keep your supply chain data current, please log in and submit your next custody event.</p>
                <a href="https://auditraks.com/supplier" style="display: inline-block; background: #4f46e5; color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 8px; font-size: 14px; font-weight: 600;">Go to Supplier Portal</a>
                <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;" />
                <p style="color: #94a3b8; font-size: 12px;">&copy; 2026 auditraks. Tungsten supply chain compliance, automated.</p>
            </div>
            """;
        var textBody = $"""
            Batch Update Needed

            Hi {supplierName}, your batch {batchNumber} has had no custody events for {daysSinceLastEvent} days.

            To maintain compliance, please log in and submit your next custody event.

            Go to Supplier Portal: https://auditraks.com/supplier
            """;
        return (subject, htmlBody, textBody);
    }

    public static (string subject, string htmlBody, string textBody) BuyerNudge(
        string supplierName, string buyerCompanyName)
    {
        var subject = $"{buyerCompanyName} is requesting an update on your supply chain data";
        var htmlBody = $"""
            <div style="font-family: system-ui, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px;">
                <h1 style="color: #4f46e5; font-size: 24px; margin-bottom: 8px;">Update Requested</h1>
                <p style="color: #64748b; font-size: 14px; margin-bottom: 24px;">Hi {supplierName}, <strong>{buyerCompanyName}</strong> is requesting an update on your supply chain compliance data.</p>
                <p style="color: #334155; font-size: 14px; margin-bottom: 24px;">Please log in to review your batches, submit any pending custody events, and ensure your compliance status is current.</p>
                <a href="https://auditraks.com/supplier" style="display: inline-block; background: #4f46e5; color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 8px; font-size: 14px; font-weight: 600;">Go to Supplier Portal</a>
                <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;" />
                <p style="color: #94a3b8; font-size: 12px;">&copy; 2026 auditraks. Tungsten supply chain compliance, automated.</p>
            </div>
            """;
        var textBody = $"""
            Update Requested

            Hi {supplierName}, {buyerCompanyName} is requesting an update on your supply chain compliance data.

            Please log in to review your batches and submit any pending custody events.

            Go to Supplier Portal: https://auditraks.com/supplier
            """;
        return (subject, htmlBody, textBody);
    }
}
