using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trainer.App.Api;

/// <summary>
/// .NET 7+ ships a TimeOnly converter for System.Text.Json, but trimming/AOT in MAUI
/// can drop it. Registering this explicitly keeps "HH:mm:ss" wire format stable.
/// </summary>
public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private const string Format = "HH:mm:ss";

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString() ?? throw new JsonException("TimeOnly value cannot be null.");
        // Accept "HH:mm" and "HH:mm:ss" and "HH:mm:ss.fff" — server uses the long form, mobile picker emits short.
        return TimeOnly.Parse(s, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}
