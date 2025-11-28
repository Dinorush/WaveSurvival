using System.Text.Json.Serialization;
using WaveSurvival.Json.Converters;

namespace WaveSurvival.Json
{
    [JsonConverter(typeof(JsonReferenceConverterFactory))]
    public class JsonReference<T> where T : new()
    {
        public virtual T? Value { get; set; } = default;
        public string ID { get; set; } = string.Empty;

        public JsonReference() { }
        public JsonReference(T? obj) => Value = obj;
        public JsonReference(string id) => ID = id;

        public static implicit operator T(JsonReference<T> r)
        {
            if (r.Value != null)
                return r.Value;
            throw new InvalidOperationException($"{typeof(T).Name} reference has not been resolved ({r.ID})");
        }
    }
}
