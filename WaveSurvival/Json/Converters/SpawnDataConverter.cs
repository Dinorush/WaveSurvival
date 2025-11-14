using GameData;
using WaveSurvival.CustomWaveData.Wave;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WaveSurvival.Json.Converters
{
    public sealed class SpawnDataConverter : JsonConverter<SpawnData>
    {
        public override SpawnData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            SpawnData target = new();

            if (reader.TokenType == JsonTokenType.Number)
            {
                target.Enemies = new() { new WeightedEnemyData() { ID = reader.GetUInt32() } };
                return target;
            }

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected number/list/object when reading a SpawnData object");

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return target;

                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected PropertyName when reading SpawnData object");

                var name = reader.GetString()!.ToLower();
                reader.Read();
                switch (name)
                {
                    case "enemies":
                        if (JSON.TryDeserialize<List<WeightedEnemyData>>(ref reader, out var enemies))
                            target.Enemies = enemies;
                        break;
                    case "count":
                        target.Count = reader.GetInt32();
                        break;
                    case "spawnrate":
                        target.SpawnRate = reader.GetSingle();
                        break;
                    case "subwavemaxcount":
                        target.SubWaveMaxCount = reader.GetInt32();
                        break;
                    case "subwavedelay":
                        target.SubWaveDelay = reader.GetSingle();
                        break;
                    case "randomdirectionchance":
                        target.RandomDirectionChance = reader.GetSingle();
                        break;
                    case "eventsonsubwavestart":
                        if (JSON.TryDeserialize<List<WardenObjectiveEventData>>(ref reader, out var events))
                            target.EventsOnSubWaveStart = events;
                        break;
                }
            }

            throw new JsonException("Expected EndObject when reading SpawnData object");
        }

        private static bool IsNextToken(Utf8JsonReader reader, JsonTokenType token)
        {
            if (!reader.Read()) throw new JsonException("Expected EndArray token when parsing SpawnData");

            return reader.TokenType == token;
        }

        public override void Write(Utf8JsonWriter writer, SpawnData? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            JSON.Serialize(writer, nameof(value.Enemies), value.Enemies);
            writer.WriteNumber(nameof(value.Count), value.Count);
            writer.WriteNumber(nameof(value.SpawnRate), value.SpawnRate);
            writer.WriteNumber(nameof(value.SubWaveMaxCount), value.SubWaveMaxCount);
            writer.WriteNumber(nameof(value.SubWaveDelay), value.SubWaveDelay);
            writer.WriteNumber(nameof(value.RandomDirectionChance), value.RandomDirectionChance);
            JSON.Serialize(writer, nameof(value.EventsOnSubWaveStart), value.EventsOnSubWaveStart);
            writer.WriteEndObject();
        }
    }
}
