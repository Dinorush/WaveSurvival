using WaveSurvival.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WaveSurvival.Json.Converters.Utils
{
    public sealed class WeightedListConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType) return false;

            return typeToConvert.GetGenericTypeDefinition() == typeof(WeightedList<>);
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var genericType = typeToConvert.GetGenericArguments()[0];
            var converterType = typeof(WeightedListConverter<>).MakeGenericType(genericType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }

    public sealed class WeightedListConverter<T> : JsonConverter<WeightedList<T>> where T : IWeightable
    {
        public override WeightedList<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<List<T>>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, WeightedList<T>? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value.Values, options);
        }
    }
}
