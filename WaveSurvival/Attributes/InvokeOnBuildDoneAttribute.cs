using System;

namespace WaveSurvival.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class InvokeOnBuildDoneAttribute : Attribute
    {
        public InvokeOnBuildDoneAttribute()
        {
        }
    }
}
