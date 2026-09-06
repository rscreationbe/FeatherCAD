using System;
using System.Windows.Media;
using Newtonsoft.Json;

namespace FeatherCAD.Logic;

// On renomme pour éviter le conflit avec System.Windows.Media.ColorConverter
public class ColorJsonConverter : JsonConverter<Color>
{
    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        // Sauvegarde au format hexadécimal string : "#FF000000"
        writer.WriteValue(value.ToString());
    }

    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        string? s = reader.Value as string;
        if (string.IsNullOrEmpty(s)) return Colors.Black;

        try
        {
            // On utilise explicitement le convertisseur de WPF
            object? color = System.Windows.Media.ColorConverter.ConvertFromString(s);
            return color != null ? (Color)color : Colors.Black;
        }
        catch
        {
            return Colors.Black;
        }
    }
}