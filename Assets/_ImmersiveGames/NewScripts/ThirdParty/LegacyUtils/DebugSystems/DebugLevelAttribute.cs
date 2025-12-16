using System;
namespace _ImmersiveGames.Scripts.Utils.DebugSystems
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DebugLevelAttribute : Attribute
    {
        public DebugLevel Level { get; }
        public DebugLevelAttribute(DebugLevel level) => Level = level;
    }
    
}