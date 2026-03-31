using FluentAssertions;
using Tungsten.Api.Common.Services;

namespace Tungsten.Api.Tests.Common.Services;

public class EmailTemplateTests
{
    [Fact]
    public void PassportShared_ReturnsCorrectSubject()
    {
        var (subject, _, _) = EmailTemplates.PassportShared("W-2026-001", "John Doe", "https://example.com/share/token", null);
        subject.Should().Contain("W-2026-001");
        subject.Should().Contain("Material Passport");
    }

    [Fact]
    public void PassportShared_IncludesMessageWhenProvided()
    {
        var (_, htmlBody, textBody) = EmailTemplates.PassportShared("W-2026-001", "John", "https://example.com", "Please review");
        htmlBody.Should().Contain("Please review");
        textBody.Should().Contain("Please review");
    }

    [Fact]
    public void PassportShared_OmitsMessageBlockWhenNull()
    {
        var (_, htmlBody, _) = EmailTemplates.PassportShared("W-2026-001", "John", "https://example.com", null);
        htmlBody.Should().NotContain("border-left: 3px solid");
    }

    [Fact]
    public void PassportShared_IncludesShareUrl()
    {
        var (_, htmlBody, textBody) = EmailTemplates.PassportShared("W-2026-001", "John", "https://example.com/share/abc", null);
        htmlBody.Should().Contain("https://example.com/share/abc");
        textBody.Should().Contain("https://example.com/share/abc");
    }

    [Fact]
    public void BatchInactivityReminder_ReturnsCorrectSubject()
    {
        var (subject, _, _) = EmailTemplates.BatchInactivityReminder("Maria", "W-2026-005", 35);
        subject.Should().Contain("W-2026-005");
        subject.Should().Contain("needs attention");
    }

    [Fact]
    public void BatchInactivityReminder_IncludesDayCount()
    {
        var (_, htmlBody, textBody) = EmailTemplates.BatchInactivityReminder("Maria", "W-2026-005", 45);
        htmlBody.Should().Contain("45 days");
        textBody.Should().Contain("45 days");
    }

    [Fact]
    public void BuyerNudge_IncludesCompanyName()
    {
        var (subject, htmlBody, textBody) = EmailTemplates.BuyerNudge("Maria", "Acme Corp");
        subject.Should().Contain("Acme Corp");
        htmlBody.Should().Contain("Acme Corp");
        textBody.Should().Contain("Acme Corp");
    }

    [Fact]
    public void BuyerNudge_IncludesSupplierName()
    {
        var (_, htmlBody, textBody) = EmailTemplates.BuyerNudge("Maria", "Acme Corp");
        htmlBody.Should().Contain("Maria");
        textBody.Should().Contain("Maria");
    }
}
