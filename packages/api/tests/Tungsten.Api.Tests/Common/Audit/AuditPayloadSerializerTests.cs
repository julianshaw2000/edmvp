using System.Text.Json;
using Tungsten.Api.Common.Audit;

namespace Tungsten.Api.Tests.Common.Audit;

public class AuditPayloadSerializerTests
{
    private record SimpleCommand(string Name, int Value);
    private record CommandWithRedact(string Name, [property: AuditRedact] string Secret);
    private record CommandWithStream(string Name, Stream Data);

    [Fact]
    public void Serialize_SimpleCommand_ReturnsJson()
    {
        var cmd = new SimpleCommand("test", 42);
        var result = AuditPayloadSerializer.Serialize(cmd);
        var doc = JsonDocument.Parse(result.GetRawText());
        Assert.Equal("test", doc.RootElement.GetProperty("Name").GetString());
        Assert.Equal(42, doc.RootElement.GetProperty("Value").GetInt32());
    }

    [Fact]
    public void Serialize_RedactedField_ReplacesWithMarker()
    {
        var cmd = new CommandWithRedact("visible", "my-secret");
        var result = AuditPayloadSerializer.Serialize(cmd);
        var doc = JsonDocument.Parse(result.GetRawText());
        Assert.Equal("visible", doc.RootElement.GetProperty("Name").GetString());
        Assert.Equal("[REDACTED]", doc.RootElement.GetProperty("Secret").GetString());
    }

    [Fact]
    public void Serialize_StreamField_ReplacesWithMarker()
    {
        using var stream = new MemoryStream();
        var cmd = new CommandWithStream("file", stream);
        var result = AuditPayloadSerializer.Serialize(cmd);
        var doc = JsonDocument.Parse(result.GetRawText());
        Assert.Equal("[STREAM]", doc.RootElement.GetProperty("Data").GetString());
    }
}
