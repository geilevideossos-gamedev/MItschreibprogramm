using System.Text.Json;
using System.Text.Json.Serialization;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Services;

// Files and settings written before the dashed style became the squared grid still say "dashed".
public sealed class PageStyleJsonConverter : JsonConverter<PageStyle>
{
    private const string LegacyDashed = "dashed";

    public override PageStyle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        if (string.Equals(text, LegacyDashed, StringComparison.OrdinalIgnoreCase))
        {
            return PageStyle.Squared;
        }

        foreach (var style in Enum.GetValues<PageStyle>())
        {
            if (string.Equals(text, Name(style), StringComparison.OrdinalIgnoreCase))
            {
                return style;
            }
        }

        throw new JsonException($"Unknown page style '{text}'.");
    }

    public override void Write(Utf8JsonWriter writer, PageStyle value, JsonSerializerOptions options) =>
        writer.WriteStringValue(Name(value));

    private static string Name(PageStyle style) => JsonNamingPolicy.CamelCase.ConvertName(style.ToString());
}
