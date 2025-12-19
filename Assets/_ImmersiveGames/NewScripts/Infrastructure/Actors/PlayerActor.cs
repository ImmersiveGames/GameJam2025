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
        private string actorId = string.Empty;

        [SerializeField]
        [Tooltip("Opcional: nome amigável exibido em logs do pipeline de baseline.")]
        private string displayName = string.Empty;

        public string ActorId => actorId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? (gameObject != null ? gameObject.name : nameof(PlayerActor))
            : displayName;

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

            DependencyManager.Provider.TryGetGlobal<IUniqueIdFactory>(out var factory);
            if (factory == null)
            {
                DebugUtility.LogError(typeof(PlayerActor),
                    "IUniqueIdFactory não encontrado; gerando ActorId local para PlayerActor.");
                actorId = $"A_{Guid.NewGuid():N}";
                return;
            }

            actorId = factory.GenerateId(gameObject);

            if (string.IsNullOrWhiteSpace(actorId))
            {
                DebugUtility.LogError(typeof(PlayerActor),
                    "Falha ao gerar ActorId via IUniqueIdFactory; gerando ActorId local.");
                actorId = $"A_{Guid.NewGuid():N}";
            }
        }
    }
}
