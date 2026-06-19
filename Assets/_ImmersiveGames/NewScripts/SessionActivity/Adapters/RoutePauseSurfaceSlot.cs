using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/SessionActivity/Route Pause Surface Slot")]
    public sealed class RoutePauseSurfaceSlot : MonoBehaviour
    {
        [SerializeField] private string slotId = "pause.activity.content.root";

        public string SlotId => slotId.TrimToEmpty();
        public bool IsValid => !string.IsNullOrWhiteSpace(SlotId);
    }
}
