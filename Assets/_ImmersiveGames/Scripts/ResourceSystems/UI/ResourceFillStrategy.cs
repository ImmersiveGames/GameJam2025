using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using TMPro;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public interface IResourceSlotStrategy
    {
        // Implementations MUST be responsible for setting fillImage and pendingFillImage values/colors.
        void ApplyFill(ResourceUISlot slot, float currentPct, float pendingPct, ResourceUIStyle style);
        void ApplyFill(ResourceUISlot slot, float currentPct, ResourceUIStyle style);
        void ApplyText(ResourceUISlot slot, string target, ResourceUIStyle style);
        void ClearVisuals(ResourceUISlot slot);
    }
}