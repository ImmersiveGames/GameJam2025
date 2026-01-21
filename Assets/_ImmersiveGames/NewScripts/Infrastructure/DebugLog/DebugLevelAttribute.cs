using System;
namespace _ImmersiveGames.NewScripts.Infrastructure.DebugLog
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DebugLevelAttribute : Attribute
    {
        public DebugLevel Level { get; }
        public DebugLevelAttribute(DebugLevel level) => Level = level;
    }
}
