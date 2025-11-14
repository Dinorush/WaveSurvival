using GameData;
using System.Text.Json;
using System.Text.Json.Serialization;
using WaveSurvival.CustomWaveData.WaveObjective;

namespace WaveSurvival.Json.Converters
{
    public sealed class SpawnPathDataConverter : JsonConverter<SpawnPathData>
    {
        public override SpawnPathData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            SpawnPathData target = new();

            if (reader.TokenType == JsonTokenType.Number)
            {
                target.ZoneIndex = (eLocalZoneIndex)reader.GetInt32();
                return target;
            }

            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected number/list when reading a SpawnPath object");

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected local index when reading a SpawnPath object");
            target.ZoneIndex = (eLocalZoneIndex)reader.GetInt32();

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected area index when reading a SpawnPath object");
            target.AreaIndex = reader.GetInt32();

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray) throw new JsonException("Expected EndArray when reading SpawnPath object");
            return target;
        }

        public override void Write(Utf8JsonWriter writer, SpawnPathData? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value.AreaIndex == -1)
                writer.WriteNumberValue((int)value.ZoneIndex);
            else
            {
                writer.WriteStartArray();
                writer.WriteNumberValue((int)value.ZoneIndex);
                writer.WriteNumberValue(value.AreaIndex);
                writer.WriteEndArray();
            }
        }
    }
}
