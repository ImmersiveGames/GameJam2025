using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public abstract class ResourceFillStrategy : ScriptableObject
    {
        // Implementations MUST be responsible for setting fillImage and pendingFillImage values/colors.
        public abstract void ApplyFill(Image fillImage, Image pendingFillImage, float target, ResourceUIStyle style);
    }
}