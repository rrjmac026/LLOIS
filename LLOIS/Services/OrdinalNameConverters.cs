namespace LLOIS.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

// Laravel sends int-backed enums (roles, feedback type/status) as numbers.
// These turn 0/1/2... into the display names the views already expect.
public abstract class OrdinalNameConverter(string[] names) : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var i))
            return i >= 0 && i < names.Length ? names[i] : i.ToString();

        return reader.GetString() ?? string.Empty;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}

public class FeedbackTypeNameConverter()   : OrdinalNameConverter(["Bug", "Concern", "Suggestion"]);
public class FeedbackStatusNameConverter() : OrdinalNameConverter(["Open", "Resolved"]);
public class RoleNameConverter()           : OrdinalNameConverter(["Viewer", "Encoder", "Admin", "SuperAdmin"]);