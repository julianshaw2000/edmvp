using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.Documents;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Documents;

public class ListBatchDocumentsTests
{
    [Fact]
    public async Task Handle_BatchWithDocuments_ReturnsAll()
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

        db.Documents.Add(new DocumentEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchId = batch.Id,
            FileName = "cert.pdf", StorageKey = "k1", FileSizeBytes = 1000,
            ContentType = "application/pdf", Sha256Hash = new string('a', 64),
            DocumentType = "CERTIFICATE_OF_ORIGIN", UploadedBy = user.Id
        });
        db.Documents.Add(new DocumentEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchId = batch.Id,
            FileName = "assay.pdf", StorageKey = "k2", FileSizeBytes = 2000,
            ContentType = "application/pdf", Sha256Hash = new string('b', 64),
            DocumentType = "ASSAY_REPORT", UploadedBy = user.Id
        });
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns(user.IdentityUserId);
        var storage = Substitute.For<IFileStorageService>();
        storage.GetDownloadUrl(Arg.Any<string>()).Returns(x => $"/download/{x.Arg<string>()}");

        var handler = new ListBatchDocuments.Handler(db, currentUser, storage);
        var result = await handler.Handle(new ListBatchDocuments.Query(batch.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Documents.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_CrossTenant_ReturnsEmpty()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantA = new TenantEntity { Id = Guid.NewGuid(), Name = "A", SchemaPrefix = "a", Status = "ACTIVE" };
        var tenantB = new TenantEntity { Id = Guid.NewGuid(), Name = "B", SchemaPrefix = "b", Status = "ACTIVE" };
        db.Tenants.AddRange(tenantA, tenantB);

        var userA = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "auth0|a", Email = "a@t.com",
            DisplayName = "A", Role = "SUPPLIER", TenantId = tenantA.Id, IsActive = true
        };
        var userB = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "auth0|b", Email = "b@t.com",
            DisplayName = "B", Role = "SUPPLIER", TenantId = tenantB.Id, IsActive = true
        };
        db.Users.AddRange(userA, userB);

        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantA.Id, BatchNumber = "B1",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "M",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = userA.Id
        };
        db.Batches.Add(batch);
        db.Documents.Add(new DocumentEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantA.Id, BatchId = batch.Id,
            FileName = "cert.pdf", StorageKey = "k1", FileSizeBytes = 1000,
            ContentType = "application/pdf", Sha256Hash = new string('a', 64),
            DocumentType = "CERTIFICATE_OF_ORIGIN", UploadedBy = userA.Id
        });
        db.SaveChanges();

        // User B tries to access tenant A's documents
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns(userB.IdentityUserId);
        var storage = Substitute.For<IFileStorageService>();

        var handler = new ListBatchDocuments.Handler(db, currentUser, storage);
        var result = await handler.Handle(new ListBatchDocuments.Query(batch.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Documents.Should().BeEmpty();
    }
}
