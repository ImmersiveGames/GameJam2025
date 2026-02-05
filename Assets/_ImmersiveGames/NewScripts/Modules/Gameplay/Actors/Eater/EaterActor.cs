using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Eater
{
    /// <summary>
    /// Implementação simples de IActor para o baseline de NewScripts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EaterActor : MonoBehaviour, IActor, IActorKindProvider
    {
        [SerializeField]
        private string actorId = string.Empty;

        [SerializeField]
        [Tooltip("Opcional: nome amigável exibido em logs do pipeline de baseline.")]
        private string displayName = string.Empty;

        public string ActorId => actorId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? (gameObject != null ? gameObject.name : nameof(EaterActor))
            : displayName;

        public Transform Transform => transform;

        public bool IsActive => isActiveAndEnabled;

        public ActorKind Kind => ActorKind.Eater;

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
                DebugUtility.LogError(typeof(EaterActor),
                    "IUniqueIdFactory não encontrado; gerando ActorId local para EaterActor.");
                actorId = $"A_{Guid.NewGuid():N}";
                return;
            }

            actorId = factory.GenerateId(gameObject);

            if (string.IsNullOrWhiteSpace(actorId))
            {
                DebugUtility.LogError(typeof(EaterActor),
                    "Falha ao gerar ActorId via IUniqueIdFactory; gerando ActorId local.");
                actorId = $"A_{Guid.NewGuid():N}";
            }
        }
    }
}
