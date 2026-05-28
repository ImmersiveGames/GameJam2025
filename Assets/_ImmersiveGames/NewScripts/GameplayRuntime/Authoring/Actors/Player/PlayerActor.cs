using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerActor : Actor
    {
        [SerializeField] private string actorId = string.Empty;

        public override string ActorId => Normalize(actorId);

        public override ActorRole ActorRoleMetadata => ActorRole.PrimaryPlayer;
        public override ActorScope ActorScopeMetadata => ActorScope.RouteScoped;

        public void SetActorId(string newActorId)
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
