using System.Text.Json;
using System.Text.Json.Serialization;
using WaveSurvival.CustomWaveData.WaveObjective;

namespace WaveSurvival.Json.Converters
{
    public sealed class WeightedWaveConverter : JsonConverter<WeightedWaveData>
    {
        public override WeightedWaveData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            WeightedWaveData data = new();

            if (reader.TokenType == JsonTokenType.String)
            {
                data.ID = reader.GetString()!;
                return data;
            }

            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected enemy data to be either a number or list");

            reader.Read();
            if (reader.TokenType == JsonTokenType.String)
                data.ID = reader.GetString()!;
            else
                throw new JsonException("Expected ID as first element in weighted wave data");

            reader.Read();
            if (reader.TokenType == JsonTokenType.EndArray)
                return data;

            if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected Weight as second element in weighted wave data");

            data.Weight = reader.GetSingle();
            reader.Read();
            if (reader.TokenType == JsonTokenType.EndArray)
                return data;

            if (reader.TokenType == JsonTokenType.EndArray)
                return data;

            throw new JsonException("Expected EndArray after parsing 2 values for weighted wave data");
        }

        public override void Write(Utf8JsonWriter writer, WeightedWaveData? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value.Weight == 1f)
            {
                writer.WriteStringValue(value.ID);
                return;
            }

            writer.WriteStartArray();
            writer.WriteStringValue(value.ID);
            writer.WriteNumberValue(value.Weight);
            writer.WriteEndArray();
        }
    }
}
