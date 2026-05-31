using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerActor : Actor
    {
        [SerializeField, HideInInspector] private string actorId = string.Empty;

        public override string ActorId => ActorIdValue.ToString();

        public ActorId ActorIdValue => new(Normalize(actorId));

        public override ActorRole ActorRoleMetadata => ActorRole.PrimaryPlayer;
        public override ActorScope ActorScopeMetadata => ActorScope.RouteScoped;

        public void SetActorId(ActorId newActorId)
        {
            if (!newActorId.IsValid)
            {
                throw new InvalidOperationException($"PlayerActor cannot bind an invalid ActorId. actor='{name}'.");
            }

            actorId = newActorId.Value;
        }

        public override void ValidateLocalConfigurationOrThrow(string source)
        {
            string origin = ResolveOrigin(source, nameof(PlayerActor), name);
            if (!ActorIdValue.IsValid)
            {
                throw new InvalidOperationException($"{origin} requires runtime ActorId binding.");
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
