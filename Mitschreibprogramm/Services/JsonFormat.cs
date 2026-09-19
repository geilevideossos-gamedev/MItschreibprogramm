using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mitschreibprogramm.Services;

public static class JsonFormat
{
    public static readonly JsonSerializerOptions Compact = Create(indented: false);

    public static readonly JsonSerializerOptions Indented = Create(indented: true);

    private static JsonSerializerOptions Create(bool indented) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = indented,
        Converters = { new PageStyleJsonConverter(), new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };
}
