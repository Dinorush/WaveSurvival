using System;

namespace WaveSurvival.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class InvokeOnCleanupAttribute : Attribute
    {
        public InvokeOnCleanupAttribute()
        {

        }
    }
}
