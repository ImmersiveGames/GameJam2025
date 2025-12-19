using System;
using _ImmersiveGames.NewScripts.Infrastructure.Ids;
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

            DependencyManager.Provider.TryGetGlobal<IUniqueIdFactory>(out var factory);
            if (factory == null)
            {
                DebugUtility.LogError(typeof(PlayerActor),
                    "IUniqueIdFactory não encontrado; gerando ActorId local para PlayerActor.");
                _actorId = $"A_{Guid.NewGuid():N}";
                return;
            }

            _actorId = factory.GenerateId(gameObject);

            if (string.IsNullOrWhiteSpace(_actorId))
            {
                DebugUtility.LogError(typeof(PlayerActor),
                    "Falha ao gerar ActorId via IUniqueIdFactory; gerando ActorId local.");
                _actorId = $"A_{Guid.NewGuid():N}";
            }
        }
    }
}
