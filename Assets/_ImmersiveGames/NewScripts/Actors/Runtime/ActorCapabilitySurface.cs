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
        private IActorCommandSourceHub actorCommandSourceHub;
        private IActorProjectileEmitterEndpoint actorProjectileEmitterEndpoint;

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

        public IActorCommandSourceHub ActorCommandSourceHub
        {
            get
            {
                if (actorCommandSourceHub == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return actorCommandSourceHub;
            }
        }

        public IActorProjectileEmitterEndpoint ActorProjectileEmitterEndpoint
        {
            get
            {
                if (actorProjectileEmitterEndpoint == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return actorProjectileEmitterEndpoint;
            }
        }

        public void RefreshFromLocalActorRoot()
        {
            Transform actorRoot = ResolveActorRootTransform();
            presentationEndpoint = ResolveSingleInActorRoot<ActorPresentationEndpoint>(actorRoot);
            attributeEndpoint = ResolveSingleInActorRoot<ActorAttributeEndpoint>(actorRoot);
            actorCameraTargetEndpoint = ResolveSingleInterfaceInActorRoot<IActorCameraTargetEndpoint>(actorRoot);
            actorMovementEndpoint = ResolveSingleInterfaceInActorRoot<IActorMovementEndpoint>(actorRoot);
            actorPermissionReceiver = ResolveSingleInterfaceInActorRoot<IActorPermissionReceiver>(actorRoot);
            actorCommandSourceHub = ResolveSingleInterfaceInActorRoot<IActorCommandSourceHub>(actorRoot);
            actorProjectileEmitterEndpoint = ResolveSingleInterfaceInActorRoot<IActorProjectileEmitterEndpoint>(actorRoot);
        }

        public bool TryGetEndpoint<TEndpoint>(out TEndpoint endpoint)
            where TEndpoint : Component
        {
            endpoint = ResolveSingleInActorRoot<TEndpoint>(ResolveActorRootTransform());
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

        private Transform ResolveActorRootTransform()
        {
            Actor actor = GetComponentInParent<Actor>(includeInactive: true);
            if (actor != null && actor.transform != null)
            {
                return actor.transform;
            }

            return transform;
        }

        private TEndpoint ResolveSingleInActorRoot<TEndpoint>(Transform actorRoot)
            where TEndpoint : Component
        {
            if (actorRoot == null)
            {
                return null;
            }

            TEndpoint[] endpoints = actorRoot.GetComponentsInChildren<TEndpoint>(includeInactive: true);
            if (endpoints == null || endpoints.Length == 0)
            {
                return null;
            }

            if (endpoints.Length > 1)
            {
                throw new InvalidOperationException($"{nameof(ActorCapabilitySurface)} found multiple {typeof(TEndpoint).Name} components in actor root '{actorRoot.name}'.");
            }

            return endpoints[0];
        }

        private TEndpoint ResolveSingleInterfaceInActorRoot<TEndpoint>(Transform actorRoot)
            where TEndpoint : class
        {
            if (actorRoot == null)
            {
                return null;
            }

            MonoBehaviour[] components = actorRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            TEndpoint foundEndpoint = null;
            for (int index = 0; index < components.Length; index++)
            {
                if (components[index] is TEndpoint candidate)
                {
                    if (foundEndpoint != null)
                    {
                        throw new InvalidOperationException($"{nameof(ActorCapabilitySurface)} found multiple {typeof(TEndpoint).Name} components in actor root '{actorRoot.name}'.");
                    }

                    foundEndpoint = candidate;
                }
            }

            return foundEndpoint;
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
