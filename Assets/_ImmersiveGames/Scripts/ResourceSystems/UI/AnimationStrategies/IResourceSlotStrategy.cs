using _ImmersiveGames.Scripts.ResourceSystems.Configs;
namespace _ImmersiveGames.Scripts.ResourceSystems.AnimationStrategies
{
    public interface IResourceSlotStrategy
    {
        void ApplyFill(ResourceUISlot slot, float currentPct, float pendingPct, ResourceUIStyle style);
        void ApplyFill(ResourceUISlot slot, float currentPct, ResourceUIStyle style);
        void ApplyText(ResourceUISlot slot, string target, ResourceUIStyle style);
        void ClearVisuals(ResourceUISlot slot);
    }
}