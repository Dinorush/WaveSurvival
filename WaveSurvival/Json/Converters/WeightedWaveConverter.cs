using System.Text.Json;
using System.Text.Json.Serialization;
using WaveSurvival.CustomWaveData.Wave;
using WaveSurvival.CustomWaveData.WaveObjective;

namespace WaveSurvival.Json.Converters
{
    public sealed class WeightedWaveConverter : JsonConverter<WeightedWaveReference>
    {
        public override WeightedWaveReference? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var element = doc.RootElement;

            WeightedWaveReference d = new();

            if (element.ValueKind == JsonValueKind.String || element.ValueKind == JsonValueKind.Object)
            {
                DeserializeReference(element, d, options);
                return d;
            }

            if (element.ValueKind != JsonValueKind.Array)
                throw new JsonException("Expected wave data or list of weighted waves");

            var arr = element.EnumerateArray();

            if (arr.MoveNext()) DeserializeReference(arr.Current, d, options);
            if (arr.MoveNext()) d.Weight = arr.Current.GetSingle();
            if (arr.MoveNext()) throw new JsonException("Expected weighted wave data to be 2 elements long");

            return d;
        }

        private static void DeserializeReference(JsonElement element, WeightedWaveReference data, JsonSerializerOptions options)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    return;
                case JsonValueKind.String:
                    data.ID = element.Deserialize<string>(options)!;
                    return;
                default:
                    data.Value = element.Deserialize< WaveData>(options);
                    return;
            };
        }

        public override void Write(Utf8JsonWriter writer, WeightedWaveReference? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value.Value != null)
            {
                JsonSerializer.Serialize(writer, value.Value, options);
                return;
            }

            if (value.Weight == 1f)
            {
                JsonSerializer.Serialize<JsonReference<WaveData>>(writer, value, options);
                return;
            }

            writer.WriteStartArray();
            JsonSerializer.Serialize<JsonReference<WaveData>>(writer, value, options);
            writer.WriteNumberValue(value.Weight);
            writer.WriteEndArray();
        }
    }
}
