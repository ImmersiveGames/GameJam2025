using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Infrastructure.Actors.Bindings.Player
{
    /// <summary>
    /// Adaptador para expor atores legados do Player como IActor do pipeline de WorldLifecycle.
    /// Mantém o ActorId sincronizado com o ator legado quando disponível.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerActorAdapter : MonoBehaviour, IActor
    {
        [SerializeField]
        private string actorId = string.Empty;

        [SerializeField]
        [Tooltip("Opcional: exibir um nome amigável para diagnósticos do pipeline de baseline.")]
        private string displayName;

        public string ActorId => actorId;

        public string DisplayName => !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : gameObject != null ? gameObject.name : nameof(PlayerActorAdapter);

        public Transform Transform => transform;

        public bool IsActive => isActiveAndEnabled;

        private void Awake()
        {
            EnsureActorId();
        }

        internal void Initialize(string newActorId)
        {
            if (!string.IsNullOrWhiteSpace(newActorId))
            {
                actorId = newActorId;
            }
        }

        private void EnsureActorId()
        {
            if (!string.IsNullOrWhiteSpace(actorId))
            {
                return;
            }

            actorId = gameObject != null ? gameObject.name : nameof(PlayerActorAdapter);
        }
    }
}
