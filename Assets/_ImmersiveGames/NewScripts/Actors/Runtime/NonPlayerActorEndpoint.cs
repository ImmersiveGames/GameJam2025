using System;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    [DisallowMultipleComponent]
    public sealed class NonPlayerActorEndpoint : MonoBehaviour
    {
        [SerializeField] private string nonPlayerActorId;
        [SerializeField] private string actorKind = "NonPlayerActor";
        [SerializeField] private ActorPresentationProfileAsset presentationProfile;
        [SerializeField] private ActorPresentationEndpoint presentationEndpoint;

        public string NonPlayerActorId => Normalize(nonPlayerActorId);
        public string ActorKind => Normalize(actorKind);
        public ActorPresentationProfileAsset PresentationProfile => presentationProfile;
        public ActorPresentationEndpoint PresentationEndpoint => presentationEndpoint;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(NonPlayerActorId) &&
            !string.IsNullOrWhiteSpace(ActorKind) &&
            presentationProfile != null &&
            presentationEndpoint != null;

        public void ValidateOrThrow(string source)
        {
            string origin = string.IsNullOrWhiteSpace(source)
                ? $"{nameof(NonPlayerActorEndpoint)}:{name}"
                : source.Trim();

            if (string.IsNullOrWhiteSpace(NonPlayerActorId))
            {
                throw new InvalidOperationException($"{origin} requires nonPlayerActorId.");
            }

            if (string.IsNullOrWhiteSpace(ActorKind))
            {
                throw new InvalidOperationException($"{origin} requires actorKind.");
            }

            if (presentationProfile == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorPresentationProfileAsset.");
            }

            if (presentationEndpoint == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorPresentationEndpoint.");
            }
        }

        private void OnValidate()
        {
            nonPlayerActorId = Normalize(nonPlayerActorId);
            actorKind = Normalize(actorKind);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
