using WaveSurvival.CustomWaveData.Wave;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WaveSurvival.Json.Converters
{
    public sealed class WeightedEnemyConverter : JsonConverter<WeightedEnemyData>
    {
        public override WeightedEnemyData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var element = doc.RootElement;

            WeightedEnemyData d = new();

            if (element.ValueKind == JsonValueKind.Number)
            {
                d.ID = element.GetUInt32();
                return d;
            }

            if (element.ValueKind != JsonValueKind.Array)
                throw new JsonException("Expected enemy data to be either a number or list");

            var arr = element.EnumerateArray();

            if (arr.MoveNext()) d.ID = arr.Current.GetUInt32();
            if (arr.MoveNext()) d.Weight = arr.Current.GetSingle();
            if (arr.MoveNext()) d.Cost = arr.Current.GetInt32();
            if (arr.MoveNext()) throw new JsonException("Expected weighted enemy tuple to be at most 3 elements long");

            return d;
        }

        public override void Write(Utf8JsonWriter writer, WeightedEnemyData? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value.Weight == 1f && value.Cost == 1)
            {
                writer.WriteNumberValue(value.ID);
                return;
            }

            writer.WriteStartArray();
            writer.WriteNumberValue(value.ID);
            writer.WriteNumberValue(value.Weight);
            writer.WriteNumberValue(value.Cost);
            writer.WriteEndArray();
        }
    }
}
