namespace WaveSurvival.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class InvokeOnBuildStartAttribute : Attribute
    {
        public InvokeOnBuildStartAttribute()
        {
        }
    }
}
