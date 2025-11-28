using WaveSurvival.CustomWaveData.Wave;
using System.Text.Json;
using System.Text.Json.Serialization;
using WaveSurvival.Utils;

namespace WaveSurvival.Json.Converters
{
    public sealed class WaveDataConverter : JsonConverter<WaveData>
    {
        public override WaveData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            WaveData target = new();

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected object when reading a WaveData object");

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return target;

                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected PropertyName when reading WaveData object");

                var name = reader.GetString()!.ToLower();
                reader.Read();
                switch (name)
                {
                    case "spawns":
                        if (reader.TokenType == JsonTokenType.Number)
                        {
                            if (JSON.TryDeserialize<JsonReference<SpawnData>>(ref reader, out var shorthand))
                                target.Spawns = new() { shorthand };
                        }
                        else if (JSON.TryDeserialize<List<JsonReference<SpawnData>>>(ref reader, out var spawns))
                            target.Spawns = spawns;
                        break;
                    case "waveheader":
                        target.WaveHeader = JsonSerializer.Deserialize<LocaleText>(ref reader, options);
                        break;
                    case "screamsize":
                        target.ScreamSize = JsonSerializer.Deserialize<ScreamSize>(ref reader, options);
                        break;
                    case "screamtype":
                        target.ScreamType = JsonSerializer.Deserialize<ScreamType>(ref reader, options);
                        break;
                }
            }

            throw new JsonException("Expected EndObject when reading SpawnData object");
        }

        public override void Write(Utf8JsonWriter writer, WaveData? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            JSON.Serialize(writer, nameof(value.Spawns), value.Spawns);
            writer.WriteString(nameof(value.WaveHeader), value.WaveHeader);
            writer.WriteString(nameof(value.ScreamSize), value.ScreamSize.ToString());
            writer.WriteString(nameof(value.ScreamType), value.ScreamType.ToString());
            writer.WriteEndObject();
        }
    }
}
