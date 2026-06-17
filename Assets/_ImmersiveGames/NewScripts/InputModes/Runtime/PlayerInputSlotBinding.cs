using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
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

        public PlayerSlotId PlayerSlotId => new(Normalize(playerSlotId));
        public bool IsValid => PlayerSlotId.IsValid;
        public bool IsInitialized => initialized;
        public string InitializedBySource => Normalize(initializedBySource);
        public string InitializedByReason => Normalize(initializedByReason);

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
            initializedBySource = Normalize(source);
            initializedByReason = Normalize(reason);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
