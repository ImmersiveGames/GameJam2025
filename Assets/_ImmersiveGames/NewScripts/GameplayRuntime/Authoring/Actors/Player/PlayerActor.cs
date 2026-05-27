using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player
{
    /// <summary>
    /// Implementacao simples de IActor para o baseline de NewScripts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerActor : Actor, IActor, IActorKindProvider
    {
        [SerializeField] private string actorId = string.Empty;

        [SerializeField]
        [Tooltip("Opcional: nome amigavel exibido em logs do pipeline de baseline.")]
        private string displayName = string.Empty;

        public override string ActorId => Normalize(actorId);

        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? (gameObject != null ? gameObject.name : nameof(PlayerActor))
            : displayName;

        public Transform Transform => transform;

        public bool IsActive => isActiveAndEnabled;

        public ActorKind Kind => ActorKind.Player;

        public override ActorRole ActorRoleMetadata => ActorRole.PrimaryPlayer;

        public override ActorScope ActorScopeMetadata => ActorScope.RouteScoped;

        public void Initialize(string newActorId)
        {
            if (!string.IsNullOrWhiteSpace(newActorId))
            {
                actorId = Normalize(newActorId);
            }
        }

        public override void ValidateLocalConfigurationOrThrow(string source)
        {
            string origin = ResolveOrigin(source, nameof(PlayerActor), name);
            if (string.IsNullOrWhiteSpace(ActorId))
            {
                throw new InvalidOperationException($"{origin} requires actorId.");
            }

            if (CapabilitySurface == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorCapabilitySurface.");
            }
        }

        private void OnValidate()
        {
            base.OnValidate();
            actorId = Normalize(actorId);
        }
    }
}
