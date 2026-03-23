using System.Reflection;
using System.Text.Json;

namespace Tungsten.Api.Common.Audit;

public static class AuditPayloadSerializer
{
    public static JsonElement Serialize<T>(T command)
    {
        var dict = new Dictionary<string, object?>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (typeof(Stream).IsAssignableFrom(prop.PropertyType))
            {
                dict[prop.Name] = "[STREAM]";
            }
            else if (prop.GetCustomAttribute<AuditRedactAttribute>() is not null)
            {
                dict[prop.Name] = "[REDACTED]";
            }
            else
            {
                dict[prop.Name] = prop.GetValue(command);
            }
        }

        var json = JsonSerializer.Serialize(dict);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
