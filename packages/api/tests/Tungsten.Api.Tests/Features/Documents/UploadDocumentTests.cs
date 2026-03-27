using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.Documents;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Documents;

public class UploadDocumentTests
{
    private static (AppDbContext db, UserEntity user, CustodyEventEntity evt) SetupDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "auth0|s", Email = "s@t.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);
        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B1",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "M",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user.Id
        };
        db.Batches.Add(batch);
        var evt = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = "k1",
            EventDate = DateTime.UtcNow, Location = "L", ActorName = "A",
            Description = "D", Sha256Hash = new string('a', 64),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(evt);
        db.SaveChanges();

        return (db, user, evt);
    }

    [Fact]
    public async Task Handle_ValidUpload_CreatesDocumentWithHash()
    {
        var (db, user, evt) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns(user.IdentityUserId);
        var storage = Substitute.For<IFileStorageService>();
        storage.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("documents/test.pdf");
        storage.GetDownloadUrl(Arg.Any<string>()).Returns("/api/documents/file/test.pdf");

        var handler = new UploadDocument.Handler(db, currentUser, storage);

        using var stream = new MemoryStream("test content"u8.ToArray());
        var command = new UploadDocument.Command(
            evt.Id, "cert.pdf", "application/pdf",
            stream, stream.Length, "CERTIFICATE_OF_ORIGIN");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("cert.pdf");
        result.Value.Sha256Hash.Should().HaveLength(64);
        result.Value.DocumentType.Should().Be("CERTIFICATE_OF_ORIGIN");

        var doc = await db.Documents.FirstAsync();
        doc.Sha256Hash.Should().HaveLength(64);
    }

    [Fact]
    public async Task Handle_FileTooLarge_ReturnsFailure()
    {
        var (db, user, evt) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns(user.IdentityUserId);
        var storage = Substitute.For<IFileStorageService>();

        var handler = new UploadDocument.Handler(db, currentUser, storage);

        using var stream = new MemoryStream();
        var command = new UploadDocument.Command(
            evt.Id, "huge.pdf", "application/pdf",
            stream, 26 * 1024 * 1024, // 26MB > 25MB limit
            "CERTIFICATE_OF_ORIGIN");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("25MB");
    }

    [Fact]
    public async Task Handle_InvalidContentType_ReturnsFailure()
    {
        var (db, user, evt) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns(user.IdentityUserId);
        var storage = Substitute.For<IFileStorageService>();

        var handler = new UploadDocument.Handler(db, currentUser, storage);

        using var stream = new MemoryStream();
        var command = new UploadDocument.Command(
            evt.Id, "script.exe", "application/x-msdownload",
            stream, 1000, "OTHER");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("type");
    }
}
