using WaveSurvival.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;
using WaveSurvival.CustomWaveData.WaveObjective;

namespace WaveSurvival.Json.Converters
{
    public sealed class WaveGroupConverter : JsonConverter<WaveGroupData>
    {
        public override WaveGroupData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            WaveGroupData data = new();

            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected StartObject when reading WaveGroup");

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return data;

                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected PropertyName when reading WaveGroup");

                var name = reader.GetString()!.ToLower();
                reader.Read();
                switch (name)
                {
                    case "waves":
                    case "wave":
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            if (!JSON.TryDeserialize<WeightedList<WeightedWaveReference>>(ref reader, out var waveList))
                                throw new JsonException("Expected list of wave data when reading WaveGroup");
                            data.Waves = waveList;
                        }
                        else
                        {
                            if (!JSON.TryDeserialize<WeightedWaveReference>(ref reader, out var waveData))
                                throw new JsonException("Expected wave data when reading WaveGroup");
                            data.Waves = new() { waveData };
                        }
                        break;
                    case "eventdata":
                    case "events":
                    case "event":
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            if (!JSON.TryDeserialize<List<JsonReference<WaveEventData>>>(ref reader, out var eventList))
                                throw new JsonException("Expected list of event data when reading WaveGroup");
                            data.Events = eventList;
                        }
                        else
                        {
                            if (!JSON.TryDeserialize<JsonReference<WaveEventData>>(ref reader, out var eventData))
                                throw new JsonException("Expected wave data when reading WaveGroup");
                            data.Events = new() { eventData };
                        }
                        break;
                }
            }

            throw new JsonException("Expected EndObject when reading WaveGroup");
        }

        public override void Write(Utf8JsonWriter writer, WaveGroupData? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("Wave");
            if (value.Waves.Count == 1)
                JsonSerializer.Serialize(writer, value.Waves[0], options);
            else
                JsonSerializer.Serialize(writer, value.Waves, options);

            writer.WritePropertyName("Event");
            if (value.Events.Count == 1)
                JsonSerializer.Serialize(writer, value.Events[0], options);
            else
                JsonSerializer.Serialize(writer, value.Events, options);
            writer.WriteEndObject();
        }
    }
}
