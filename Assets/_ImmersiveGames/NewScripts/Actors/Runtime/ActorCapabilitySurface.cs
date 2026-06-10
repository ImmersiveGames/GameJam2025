using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Contracts;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ActorCapabilitySurface : MonoBehaviour
    {
        private ActorPresentationEndpoint _presentationEndpoint;
        private ActorAttributeEndpoint _attributeEndpoint;
        private IActorCameraTargetEndpoint _actorCameraTargetEndpoint;
        private IActorMovementEndpoint _actorMovementEndpoint;
        private IActorPermissionReceiver _actorPermissionReceiver;
        private IActorCommandSourceHub _actorCommandSourceHub;
        private IActorProjectileFireEndpoint _actorProjectileFireEndpoint;

        private IActorSetupContributionProvider[] _setupContributionProviders = Array.Empty<IActorSetupContributionProvider>();
        private IActorBindingContributionProvider[] _bindingContributionProviders = Array.Empty<IActorBindingContributionProvider>();
        private IActorPermissionReceiverContributionProvider[] _permissionReceiverContributionProviders = Array.Empty<IActorPermissionReceiverContributionProvider>();
        private IActorResetContributionProvider[] _resetContributionProviders = Array.Empty<IActorResetContributionProvider>();
        private IActorSnapshotContributionProvider[] _snapshotContributionProviders = Array.Empty<IActorSnapshotContributionProvider>();
        private IActorRestoreContributionProvider[] _restoreContributionProviders = Array.Empty<IActorRestoreContributionProvider>();
        private IActorReleaseContributionProvider[] _releaseContributionProviders = Array.Empty<IActorReleaseContributionProvider>();

        public ActorPresentationEndpoint PresentationEndpoint
        {
            get
            {
                if (_presentationEndpoint == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return _presentationEndpoint;
            }
        }

        public ActorAttributeEndpoint AttributeEndpoint
        {
            get
            {
                if (_attributeEndpoint == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return _attributeEndpoint;
            }
        }

        public IActorCameraTargetEndpoint ActorCameraTargetEndpoint
        {
            get
            {
                if (_actorCameraTargetEndpoint == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return _actorCameraTargetEndpoint;
            }
        }

        public IActorMovementEndpoint ActorMovementEndpoint
        {
            get
            {
                if (_actorMovementEndpoint == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return _actorMovementEndpoint;
            }
        }

        public IActorPermissionReceiver ActorPermissionReceiver
        {
            get
            {
                if (_actorPermissionReceiver == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return _actorPermissionReceiver;
            }
        }

        public IActorCommandSourceHub ActorCommandSourceHub
        {
            get
            {
                if (_actorCommandSourceHub == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return _actorCommandSourceHub;
            }
        }

        public IActorProjectileFireEndpoint ActorProjectileFireEndpoint
        {
            get
            {
                if (_actorProjectileFireEndpoint == null)
                {
                    RefreshFromLocalActorRoot();
                }

                return _actorProjectileFireEndpoint;
            }
        }

        public IReadOnlyList<IActorSetupContributionProvider> SetupContributionProviders => _setupContributionProviders;
        public IReadOnlyList<IActorBindingContributionProvider> BindingContributionProviders => _bindingContributionProviders;
        public IReadOnlyList<IActorPermissionReceiverContributionProvider> PermissionReceiverContributionProviders => _permissionReceiverContributionProviders;
        public IReadOnlyList<IActorResetContributionProvider> ResetContributionProviders => _resetContributionProviders;
        public IReadOnlyList<IActorSnapshotContributionProvider> SnapshotContributionProviders => _snapshotContributionProviders;
        public IReadOnlyList<IActorRestoreContributionProvider> RestoreContributionProviders => _restoreContributionProviders;
        public IReadOnlyList<IActorReleaseContributionProvider> ReleaseContributionProviders => _releaseContributionProviders;

        public void RefreshFromLocalActorRoot()
        {
            Transform actorRoot = ResolveActorRootTransform();

            // Trilho concreto ainda ativo. Deve ser removido quando scanners migrarem para contributions homogêneas.
            _presentationEndpoint = ResolveSingleInActorRoot<ActorPresentationEndpoint>(actorRoot);
            _attributeEndpoint = ResolveSingleInActorRoot<ActorAttributeEndpoint>(actorRoot);
            _actorCameraTargetEndpoint = ResolveSingleInterfaceInActorRoot<IActorCameraTargetEndpoint>(actorRoot);
            _actorMovementEndpoint = ResolveSingleInterfaceInActorRoot<IActorMovementEndpoint>(actorRoot);
            _actorPermissionReceiver = ResolveSingleInterfaceInActorRoot<IActorPermissionReceiver>(actorRoot);
            _actorCommandSourceHub = ResolveSingleInterfaceInActorRoot<IActorCommandSourceHub>(actorRoot);
            _actorProjectileFireEndpoint = ResolveSingleInterfaceInActorRoot<IActorProjectileFireEndpoint>(actorRoot);

            // Novo índice passivo. Providers podem ser zero, um ou vários por fase.
            _setupContributionProviders = ResolveInterfacesInActorRoot<IActorSetupContributionProvider>(actorRoot);
            _bindingContributionProviders = ResolveInterfacesInActorRoot<IActorBindingContributionProvider>(actorRoot);
            _permissionReceiverContributionProviders = ResolveInterfacesInActorRoot<IActorPermissionReceiverContributionProvider>(actorRoot);
            _resetContributionProviders = ResolveInterfacesInActorRoot<IActorResetContributionProvider>(actorRoot);
            _snapshotContributionProviders = ResolveInterfacesInActorRoot<IActorSnapshotContributionProvider>(actorRoot);
            _restoreContributionProviders = ResolveInterfacesInActorRoot<IActorRestoreContributionProvider>(actorRoot);
            _releaseContributionProviders = ResolveInterfacesInActorRoot<IActorReleaseContributionProvider>(actorRoot);
        }

        public bool TryGetEndpoint<TEndpoint>(out TEndpoint endpoint)
            where TEndpoint : Component
        {
            endpoint = ResolveSingleInActorRoot<TEndpoint>(ResolveActorRootTransform());
            return endpoint != null;
        }

        public bool TryGetInterfaceEndpoint<TEndpoint>(out TEndpoint endpoint)
            where TEndpoint : class
        {
            endpoint = ResolveSingleInterfaceInActorRoot<TEndpoint>(ResolveActorRootTransform());
            return endpoint != null;
        }

        public IReadOnlyList<TProvider> GetContributionProviders<TProvider>()
            where TProvider : class
        {
            return ResolveInterfacesInActorRoot<TProvider>(ResolveActorRootTransform());
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

        private TInterface[] ResolveInterfacesInActorRoot<TInterface>(Transform actorRoot)
            where TInterface : class
        {
            if (actorRoot == null)
            {
                return Array.Empty<TInterface>();
            }

            MonoBehaviour[] components = actorRoot.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            if (components == null || components.Length == 0)
            {
                return Array.Empty<TInterface>();
            }

            List<TInterface> results = new List<TInterface>();
            for (int index = 0; index < components.Length; index++)
            {
                if (components[index] is TInterface candidate)
                {
                    results.Add(candidate);
                }
            }

            return results.Count == 0 ? Array.Empty<TInterface>() : results.ToArray();
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
