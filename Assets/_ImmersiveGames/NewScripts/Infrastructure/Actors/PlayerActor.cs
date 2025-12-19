using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Actors
{
    /// <summary>
    /// Implementação simples de IActor para o baseline de NewScripts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerActor : MonoBehaviour, IActor
    {
        [SerializeField]
        private string _actorId = string.Empty;

        [SerializeField]
        [Tooltip("Opcional: nome amigável exibido em logs do pipeline de baseline.")]
        private string _displayName = string.Empty;

        public string ActorId => _actorId;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName)
            ? (gameObject != null ? gameObject.name : nameof(PlayerActor))
            : _displayName;

        public Transform Transform => transform;

        public bool IsActive => isActiveAndEnabled;

        private void Awake()
        {
            EnsureActorId();
        }

        internal void Initialize(string actorId)
        {
            if (!string.IsNullOrWhiteSpace(actorId))
            {
                _actorId = actorId;
            }
        }

        private void EnsureActorId()
        {
            if (!string.IsNullOrWhiteSpace(_actorId))
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal(out IUniqueIdFactory factory) || factory == null)
            {
                DebugUtility.LogWarning(typeof(PlayerActor),
                    "IUniqueIdFactory não encontrado; criando instância local para gerar ActorId.");
                factory = new UniqueIdFactory();
                DependencyManager.Provider.RegisterGlobal(factory);
            }

            _actorId = factory.GenerateId(gameObject);
        }
    }
}
