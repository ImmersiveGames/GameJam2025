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

        public string PlayerSlotId => Normalize(playerSlotId);
        public bool IsValid => !string.IsNullOrWhiteSpace(PlayerSlotId);
        public bool IsInitialized => initialized;
        public string InitializedBySource => Normalize(initializedBySource);
        public string InitializedByReason => Normalize(initializedByReason);

        public void Initialize(string slotId, string source, string reason)
        {
            string normalizedSlotId = Normalize(slotId);
            if (string.IsNullOrWhiteSpace(normalizedSlotId))
            {
                throw new System.InvalidOperationException("PlayerInputSlotBinding.Initialize requer slotId explicito e nao-vazio.");
            }

            string currentSlotId = PlayerSlotId;
            if (!string.IsNullOrWhiteSpace(currentSlotId) &&
                !string.Equals(currentSlotId, normalizedSlotId, System.StringComparison.Ordinal))
            {
                throw new System.InvalidOperationException(
                    $"PlayerInputSlotBinding ja configurado com slotId='{currentSlotId}' e nao pode ser reinicializado para slotId='{normalizedSlotId}'.");
            }

            if (initialized && !string.Equals(currentSlotId, normalizedSlotId, System.StringComparison.Ordinal))
            {
                throw new System.InvalidOperationException(
                    $"PlayerInputSlotBinding reinicializacao invalida: currentSlotId='{currentSlotId}' requestedSlotId='{normalizedSlotId}'.");
            }

            playerSlotId = normalizedSlotId;
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
