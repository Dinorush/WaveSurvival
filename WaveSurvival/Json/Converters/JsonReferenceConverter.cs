using System.Text.Json;
using System.Text.Json.Serialization;

namespace WaveSurvival.Json.Converters
{
    public sealed class JsonReferenceConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType) return false;

            return typeToConvert.GetGenericTypeDefinition().IsAssignableTo(typeof(JsonReference<>));
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var genericType = typeToConvert.GetGenericArguments()[0];
            var converterType = typeof(JsonReferenceConverter<>).MakeGenericType(genericType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }

    public sealed class JsonReferenceConverter<T> : JsonConverter<JsonReference<T>> where T : new()
    {
        public override bool HandleNull => true;

        public override JsonReference<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Null => new(),
                JsonTokenType.String => new(JsonSerializer.Deserialize<string>(ref reader, options)!),
                _ => new(JsonSerializer.Deserialize<T>(ref reader, options))
            };
        }

        public override void Write(Utf8JsonWriter writer, JsonReference<T>? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value.Value != null)
                JsonSerializer.Serialize(writer, value.Value, options);
            else if (!string.IsNullOrEmpty(value.ID))
                JsonSerializer.Serialize(writer, value.ID, options);
            else
                JsonSerializer.Serialize(writer, new T(), options);
        }
    }
}
