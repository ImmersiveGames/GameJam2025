using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Player.Movement;
using _ImmersiveGames.NewScripts.Players.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ActorCapabilitySurface : MonoBehaviour
    {
        private ActorPresentationEndpoint presentationEndpoint;
        private ActorAttributeEndpoint attributeEndpoint;
        private PlayerCameraEndpoint playerCameraEndpoint;
        private PlayerMovementController playerMovementEndpoint;

        public ActorPresentationEndpoint PresentationEndpoint
        {
            get
            {
                if (presentationEndpoint == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return presentationEndpoint;
            }
        }

        public ActorAttributeEndpoint AttributeEndpoint
        {
            get
            {
                if (attributeEndpoint == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return attributeEndpoint;
            }
        }

        public PlayerCameraEndpoint PlayerCameraEndpoint
        {
            get
            {
                if (playerCameraEndpoint == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return playerCameraEndpoint;
            }
        }

        public PlayerMovementController PlayerMovementEndpoint
        {
            get
            {
                if (playerMovementEndpoint == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return playerMovementEndpoint;
            }
        }

        public void RefreshFromLocalActorRoot()
        {
            presentationEndpoint = ResolveSingleInActorRoot<ActorPresentationEndpoint>();
            attributeEndpoint = ResolveSingleInActorRoot<ActorAttributeEndpoint>();
            playerCameraEndpoint = ResolveSingleInActorRoot<PlayerCameraEndpoint>();
            playerMovementEndpoint = ResolveSingleInActorRoot<PlayerMovementController>();
        }

        public bool TryGetEndpoint<TEndpoint>(out TEndpoint endpoint)
            where TEndpoint : Component
        {
            endpoint = ResolveSingleInActorRoot<TEndpoint>();
            return endpoint != null;
        }

        public void ValidateLocalConfigurationOrThrow(string source)
        {
            string origin = string.IsNullOrWhiteSpace(source)
                ? $"{nameof(ActorCapabilitySurface)}:{name}"
                : source.Trim();

            if (transform == null)
            {
                throw new InvalidOperationException($"{origin} requires valid transform.");
            }
        }

        private TEndpoint ResolveSingleInActorRoot<TEndpoint>()
            where TEndpoint : Component
        {
            TEndpoint[] endpoints = GetComponentsInChildren<TEndpoint>(includeInactive: true);
            if (endpoints == null || endpoints.Length == 0)
            {
                return null;
            }

            return endpoints[0];
        }

        private void OnValidate()
        {
            RefreshFromLocalActorRoot();
        }

        private void Awake()
        {
            RefreshFromLocalActorRoot();
        }
    }
}
