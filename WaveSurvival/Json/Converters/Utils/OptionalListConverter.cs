using System.Text.Json;
using System.Text.Json.Serialization;

namespace WaveSurvival.Json.Converters.Utils
{
    public sealed class OptionalListConverter<T> : JsonConverter<List<T>>
    {
        public override List<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            List<T> target = new();

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return target;

                    target.Add(JsonSerializer.Deserialize<T>(ref reader, options)!);
                }
                throw new JsonException("Expected EndArray when reading list");
            }

            target.Add(JsonSerializer.Deserialize<T>(ref reader, options)!);
            return target;
        }

        public override void Write(Utf8JsonWriter writer, List<T>? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value.Count == 1)
            {
                JsonSerializer.Serialize(writer, value[0], options);
                return;
            }

            writer.WriteStartArray();
            foreach (var item in value)
                JsonSerializer.Serialize(writer, item, options);
            writer.WriteEndArray();
        }
    }
}
