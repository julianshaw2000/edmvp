using FluentAssertions;
using Tungsten.Api.Common.Services;

namespace Tungsten.Api.Tests.Features.CustodyEvents;

public class HashServiceTests
{
    [Fact]
    public void ComputeEventHash_SameInput_SameHash()
    {
        var date = HashService.NormalizeDate("2026-01-15T10:00:00Z");
        var hash1 = HashService.ComputeEventHash("MINE_EXTRACTION", date,
            Guid.Parse("11111111-1111-1111-1111-111111111111"), "Bisie Mine",
            "Mining Corp", null, "First extraction", "{}", null);

        var hash2 = HashService.ComputeEventHash("MINE_EXTRACTION", date,
            Guid.Parse("11111111-1111-1111-1111-111111111111"), "Bisie Mine",
            "Mining Corp", null, "First extraction", "{}", null);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeEventHash_DifferentInput_DifferentHash()
    {
        var hash1 = HashService.ComputeEventHash("MINE_EXTRACTION",
            HashService.NormalizeDate("2026-01-15T10:00:00Z"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"), "Bisie Mine",
            "Mining Corp", null, "First extraction", "{}", null);

        var hash2 = HashService.ComputeEventHash("MINE_EXTRACTION",
            HashService.NormalizeDate("2026-01-15T11:00:00Z"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"), "Bisie Mine",
            "Mining Corp", null, "First extraction", "{}", null);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeEventHash_Returns64CharHexString()
    {
        var hash = HashService.ComputeEventHash("MINE_EXTRACTION", "2026-01-15T10:00:00Z",
            Guid.NewGuid(), "loc", "actor", null, "desc", "{}", null);

        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[a-f0-9]{64}$");
    }

    [Fact]
    public void ComputeEventHash_IncludesPreviousHash_ChangesResult()
    {
        var batchId = Guid.NewGuid();
        var hashWithout = HashService.ComputeEventHash("MINE_EXTRACTION", "2026-01-15T10:00:00Z",
            batchId, "loc", "actor", null, "desc", "{}", null);

        var hashWith = HashService.ComputeEventHash("MINE_EXTRACTION", "2026-01-15T10:00:00Z",
            batchId, "loc", "actor", null, "desc", "{}", "abc123");

        hashWithout.Should().NotBe(hashWith);
    }
}
