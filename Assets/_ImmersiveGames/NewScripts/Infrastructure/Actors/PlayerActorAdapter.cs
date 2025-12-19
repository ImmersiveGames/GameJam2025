using UnityEngine;
using LegacyActor = _ImmersiveGames.Scripts.ActorSystems.IActor;

namespace _ImmersiveGames.NewScripts.Infrastructure.Actors
{
    /// <summary>
    /// Adaptador para expor atores legados do Player como IActor do pipeline de WorldLifecycle.
    /// Mantém o ActorId sincronizado com o ator legado quando disponível.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerActorAdapter : MonoBehaviour, IActor
    {
        [SerializeField]
        private string _actorId = string.Empty;

        [SerializeField]
        [Tooltip("Opcional: exibir um nome amigável para diagnósticos do pipeline de baseline.")]
        private string _displayName;

        private LegacyActor _legacyActor;

        public string ActorId => _actorId;

        public string DisplayName => !string.IsNullOrWhiteSpace(_displayName)
            ? _displayName
            : gameObject != null ? gameObject.name : nameof(PlayerActorAdapter);

        public Transform Transform => transform;

        public bool IsActive => isActiveAndEnabled;

        private void Awake()
        {
            _legacyActor = GetComponent<LegacyActor>();
            if (_legacyActor != null && string.IsNullOrWhiteSpace(_actorId))
            {
                _actorId = _legacyActor.ActorId ?? string.Empty;
            }
        }

        internal void Initialize(string actorId)
        {
            if (!string.IsNullOrWhiteSpace(actorId))
            {
                _actorId = actorId;
            }
            else if (_legacyActor != null)
            {
                _actorId = _legacyActor.ActorId ?? string.Empty;
            }
        }
    }
}
