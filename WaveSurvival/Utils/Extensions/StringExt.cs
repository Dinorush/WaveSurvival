namespace WaveSurvival.Utils.Extensions
{
    internal static class StringExt
    {
        public static T ToEnum<T>(this string? value, T defaultValue) where T : struct
        {
            if (string.IsNullOrEmpty(value)) return defaultValue;

            return Enum.TryParse(value.Replace(" ", null), true, out T result) ? result : defaultValue;
        }
    }
}
