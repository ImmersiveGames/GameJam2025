using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ActorCapabilitySurface : MonoBehaviour
    {
        private ActorPresentationEndpoint presentationEndpoint;
        private ActorAttributeEndpoint attributeEndpoint;
        private IActorCameraTargetEndpoint actorCameraTargetEndpoint;
        private IActorMovementEndpoint actorMovementEndpoint;
        private IActorPermissionReceiver actorPermissionReceiver;
        private IActorIntentSource actorIntentSource;

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

        public IActorCameraTargetEndpoint ActorCameraTargetEndpoint
        {
            get
            {
                if (actorCameraTargetEndpoint == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return actorCameraTargetEndpoint;
            }
        }

        public IActorMovementEndpoint ActorMovementEndpoint
        {
            get
            {
                if (actorMovementEndpoint == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return actorMovementEndpoint;
            }
        }

        public IActorPermissionReceiver ActorPermissionReceiver
        {
            get
            {
                if (actorPermissionReceiver == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return actorPermissionReceiver;
            }
        }

        public IActorIntentSource ActorIntentSource
        {
            get
            {
                if (actorIntentSource == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return actorIntentSource;
            }
        }

        public void RefreshFromLocalActorRoot()
        {
            presentationEndpoint = ResolveSingleInActorRoot<ActorPresentationEndpoint>();
            attributeEndpoint = ResolveSingleInActorRoot<ActorAttributeEndpoint>();
            actorCameraTargetEndpoint = ResolveSingleInterfaceInActorRoot<IActorCameraTargetEndpoint>();
            actorMovementEndpoint = ResolveSingleInterfaceInActorRoot<IActorMovementEndpoint>();
            actorPermissionReceiver = ResolveSingleInterfaceInActorRoot<IActorPermissionReceiver>();
            actorIntentSource = ResolveSingleInterfaceInActorRoot<IActorIntentSource>();
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

        private TEndpoint ResolveSingleInterfaceInActorRoot<TEndpoint>()
            where TEndpoint : class
        {
            MonoBehaviour[] components = GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            for (int index = 0; index < components.Length; index++)
            {
                if (components[index] is TEndpoint endpoint)
                {
                    return endpoint;
                }
            }

            return null;
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
