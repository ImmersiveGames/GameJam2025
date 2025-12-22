using UnityEngine;
using LegacyActor = _ImmersiveGames.Scripts.ActorSystems.IActor;

namespace _ImmersiveGames.NewScripts.Infrastructure.Actors
{
    /// <summary>
    /// Adaptador para expor atores legados do Player como IActor do pipeline de WorldLifecycle.
    /// Mantém o ActorId sincronizado com o ator legado quando disponível.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerActorAdapter : MonoBehaviour, IActor, IPlayerActorMarker
    {
        [SerializeField]
        private string actorId = string.Empty;

        [SerializeField]
        [Tooltip("Opcional: exibir um nome amigável para diagnósticos do pipeline de baseline.")]
        private string displayName;

        private LegacyActor _legacyActor;

        public string ActorId => actorId;

        public string DisplayName => !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : gameObject != null ? gameObject.name : nameof(PlayerActorAdapter);

        public Transform Transform => transform;

        public bool IsActive => isActiveAndEnabled;

        private void Awake()
        {
            _legacyActor = GetComponent<LegacyActor>();
            if (_legacyActor != null && string.IsNullOrWhiteSpace(actorId))
            {
                actorId = _legacyActor.ActorId ?? string.Empty;
            }
        }

        internal void Initialize(string newActorId)
        {
            if (!string.IsNullOrWhiteSpace(newActorId))
            {
                actorId = newActorId;
            }
            else if (_legacyActor != null)
            {
                actorId = _legacyActor.ActorId ?? string.Empty;
            }
        }
    }
}
