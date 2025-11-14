using WaveSurvival.CustomWaveData.Wave;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WaveSurvival.Json.Converters
{
    public sealed class WeightedEnemyConverter : JsonConverter<WeightedEnemyData>
    {
        public override WeightedEnemyData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            WeightedEnemyData data = new();

            if (reader.TokenType == JsonTokenType.Number)
            {
                data.ID = reader.GetUInt32();
                return data;
            }

            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected enemy data to be either a number or list");

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected ID as first element in enemy data");

            data.ID = reader.GetUInt32();
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndArray)
                return data;

            if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected Weight as second element in enemy data");

            data.Weight = reader.GetSingle();
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndArray)
                return data;

            if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected Cost as third element in enemy data");

            data.Cost = reader.GetInt32();
            reader.Read();

            if (reader.TokenType == JsonTokenType.EndArray)
                return data;

            throw new JsonException("Expected EndArray after parsing 3 values for enemy data");
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
