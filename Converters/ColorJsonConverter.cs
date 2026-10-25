using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media;

namespace ATLab.Converters;

/// <summary>Stores colors as text (e.g. #FF2979FF) in the project file.</summary>
public sealed class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return Color.Parse(reader.GetString()!);
    }

    public override void Write(
        Utf8JsonWriter writer,
        Color value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}