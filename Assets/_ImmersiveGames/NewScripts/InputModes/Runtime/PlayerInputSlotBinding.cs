using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputSlotBinding : MonoBehaviour
    {
        [SerializeField] private string playerSlotId;
        [SerializeField] private bool initialized;
        [SerializeField] private string initializedBySource;
        [SerializeField] private string initializedByReason;

        public PlayerSlotId PlayerSlotId => new(playerSlotId.TrimToEmpty());
        public bool IsValid => PlayerSlotId.IsValid;
        public bool IsInitialized => initialized;
        public string InitializedBySource => initializedBySource.TrimToEmpty();
        public string InitializedByReason => initializedByReason.TrimToEmpty();

        public void Initialize(PlayerSlotId slotId, string source, string reason)
        {
            if (!slotId.IsValid)
            {
                throw new System.InvalidOperationException("PlayerInputSlotBinding.Initialize requires explicit valid PlayerSlotId.");
            }

            var currentSlotId = PlayerSlotId;
            if (currentSlotId.IsValid && currentSlotId != slotId)
            {
                throw new System.InvalidOperationException(
                    $"PlayerInputSlotBinding already configured with slotId='{currentSlotId}' and cannot be reinitialized to slotId='{slotId}'.");
            }

            if (initialized && currentSlotId != slotId)
            {
                throw new System.InvalidOperationException(
                    $"PlayerInputSlotBinding invalid reinitialization: currentSlotId='{currentSlotId}' requestedSlotId='{slotId}'.");
            }

            playerSlotId = slotId.Value;
            initialized = true;
            initializedBySource = source.TrimToEmpty();
            initializedByReason = reason.TrimToEmpty();
        }
}
}
