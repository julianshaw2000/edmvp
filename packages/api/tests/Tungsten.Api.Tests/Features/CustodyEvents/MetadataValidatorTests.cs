using System.Text.Json;
using FluentAssertions;
using Tungsten.Api.Features.CustodyEvents;

namespace Tungsten.Api.Tests.Features.CustodyEvents;

public class MetadataValidatorTests
{
    [Fact]
    public void Validate_MineExtraction_ValidMetadata_ReturnsSuccess()
    {
        var metadata = JsonSerializer.SerializeToElement(new
        {
            gpsCoordinates = "-1.5,29.0",
            mineOperatorIdentity = "Mining Corp",
            mineralogicalCertificateRef = "CERT-001"
        });

        var result = MetadataValidator.Validate("MINE_EXTRACTION", metadata);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MineExtraction_MissingField_ReturnsErrors()
    {
        var metadata = JsonSerializer.SerializeToElement(new
        {
            gpsCoordinates = "-1.5,29.0"
            // missing mineOperatorIdentity and mineralogicalCertificateRef
        });

        var result = MetadataValidator.Validate("MINE_EXTRACTION", metadata);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_PrimaryProcessing_ValidMetadata_ReturnsSuccess()
    {
        var metadata = JsonSerializer.SerializeToElement(new
        {
            smelterId = "CID001100",
            processType = "Carbothermic reduction",
            inputWeightKg = 500.0,
            outputWeightKg = 450.0
        });

        var result = MetadataValidator.Validate("PRIMARY_PROCESSING", metadata);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UnknownEventType_ReturnsError()
    {
        var metadata = JsonSerializer.SerializeToElement(new { });

        var result = MetadataValidator.Validate("UNKNOWN_TYPE", metadata);
        result.IsValid.Should().BeFalse();
    }
}
