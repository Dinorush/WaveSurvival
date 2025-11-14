namespace WaveSurvival.Utils.Extensions
{
    internal static class RandomExt
    {
        public static void Shuffle<T>(this Random random, T[] values)
        {
            int n = values.Length;

            for (int i = 0; i < n - 1; i++)
            {
                int j = random.Next(i, n);

                if (j != i)
                    (values[j], values[i]) = (values[i], values[j]);
            }
        }

        public static float NextSingle(this Random random, float maxValue)
        {
            return random.NextSingle() * maxValue;
        }

        public static float NextSingle(this Random random, float minValue, float maxValue)
        {
            return random.NextSingle() * (maxValue - minValue) + minValue;
        }
    }
}
