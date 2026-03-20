using FluentAssertions;
using Tungsten.Api.Common.Services;

namespace Tungsten.Api.Tests.Features.CustodyEvents;

public class IdempotencyKeyServiceTests
{
    [Fact]
    public void GenerateKey_SameInput_SameKey()
    {
        var batchId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var key1 = IdempotencyKeyService.GenerateKey(batchId, "MINE_EXTRACTION", "2026-01-15T10:00:00Z", "Bisie Mine", "Mining Corp");
        var key2 = IdempotencyKeyService.GenerateKey(batchId, "MINE_EXTRACTION", "2026-01-15T10:00:00Z", "Bisie Mine", "Mining Corp");

        key1.Should().Be(key2);
    }

    [Fact]
    public void GenerateKey_DifferentInput_DifferentKey()
    {
        var batchId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var key1 = IdempotencyKeyService.GenerateKey(batchId, "MINE_EXTRACTION", "2026-01-15T10:00:00Z", "Bisie Mine", "Mining Corp");
        var key2 = IdempotencyKeyService.GenerateKey(batchId, "CONCENTRATION", "2026-01-15T10:00:00Z", "Bisie Mine", "Mining Corp");

        key1.Should().NotBe(key2);
    }
}
