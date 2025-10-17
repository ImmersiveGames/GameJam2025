using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlanetsMaster))]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PlanetResourceSystemBridge : ResourceBridgeBase
    {
        [Header("Resource System Binding")]
        [SerializeField] private PlanetResourcesSo defaultResource;
        [SerializeField] private ResourceInstanceConfig resourceConfig;

        private PlanetsMaster _planetMaster;
        private PlanetResourcesSo _currentResource;
        private VisualResourceValue _visualValue;
        private ResourceType _resourceType = ResourceType.None;
        private bool _hasPendingUpdate;

        public PlanetResourcesSo CurrentResource => _currentResource;
        public bool HasResource => _currentResource != null;
        public PlanetsMaster PlanetMaster => _planetMaster;

        protected override void Awake()
        {
            base.Awake();

            if (!TryGetComponent(out _planetMaster))
            {
                DebugUtility.LogError<PlanetResourceSystemBridge>(
                    $"PlanetsMaster não encontrado em {gameObject.name}.",
                    this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!enabled)
            {
                return;
            }

            if (defaultResource != null && _currentResource == null)
            {
                AssignResource(defaultResource, false);
            }
            else if (_currentResource != null)
            {
                ScheduleStateSync();
            }
        }

        protected override void OnServiceInitialized()
        {
            ScheduleStateSync();
        }

        protected override void OnServiceDispose()
        {
            _visualValue = null;
            _resourceType = ResourceType.None;
            _hasPendingUpdate = false;
        }

        public bool AssignResource(PlanetResourcesSo resource, bool raiseEvents = true)
        {
            if (!enabled)
            {
                return false;
            }

            if (resource == null)
            {
                DebugUtility.LogWarning<PlanetResourceSystemBridge>(
                    $"Tentativa de atribuir recurso nulo ao planeta {_planetMaster?.name ?? name}.",
                    this);
                return false;
            }

            var previous = _currentResource;

            _currentResource = resource;
            _resourceType = ResolveResourceType(resource);
            ScheduleStateSync();

            if (raiseEvents && _planetMaster != null)
            {
                EventBus<PlanetResourceAssignedEvent>.Raise(new PlanetResourceAssignedEvent(_planetMaster, resource));
            }

            return previous != resource;
        }

        public void ClearResource(bool raiseEvents = true)
        {
            if (!enabled)
            {
                return;
            }

            if (_currentResource == null && !_hasPendingUpdate && _visualValue == null)
            {
                return;
            }

            var previous = _currentResource;

            _currentResource = null;
            ScheduleStateSync();

            if (raiseEvents && previous != null && _planetMaster != null)
            {
                EventBus<PlanetResourceClearedEvent>.Raise(new PlanetResourceClearedEvent(_planetMaster, previous));
            }
        }

        private void ScheduleStateSync()
        {
            _hasPendingUpdate = true;
            TryApplyPendingResource();
        }

        private void TryApplyPendingResource()
        {
            if (!IsInitialized || resourceSystem == null || !_hasPendingUpdate)
            {
                return;
            }

            ApplyResourceState();
        }

        private void ApplyResourceState()
        {
            if (!_hasPendingUpdate || resourceSystem == null)
            {
                return;
            }

            var targetType = _resourceType != ResourceType.None
                ? _resourceType
                : ResolveResourceType(defaultResource);

            if (targetType == ResourceType.None)
            {
                targetType = ResourceType.PlanetResource;
            }

            if (_currentResource != null)
            {
                _visualValue = CreateVisualValue(_currentResource);

                bool updated = resourceSystem.RegisterOrUpdateResource(targetType, _visualValue, resourceConfig);
                if (updated)
                {
                    DebugUtility.LogVerbose<PlanetResourceSystemBridge>(
                        $"Recurso visual {_currentResource.ResourceType} sincronizado para {_planetMaster?.name ?? name}.",
                        "cyan",
                        this);
                }
            }
            else
            {
                if (_visualValue == null)
                {
                    _visualValue = new VisualResourceValue();
                }

                _visualValue.SetCurrentValue(0f);
                _visualValue.SetIcon(null);

                bool updated = resourceSystem.RegisterOrUpdateResource(targetType, _visualValue, resourceConfig);
                if (updated)
                {
                    DebugUtility.LogVerbose<PlanetResourceSystemBridge>(
                        $"Recurso visual limpo para {_planetMaster?.name ?? name}.",
                        "yellow",
                        this);
                }
            }

            _hasPendingUpdate = false;
        }

        private VisualResourceValue CreateVisualValue(PlanetResourcesSo resource)
        {
            var created = resource?.CreateInitialValue();
            resource?.ApplyTo(created);

            if (created is VisualResourceValue visualValue)
            {
                return visualValue;
            }

            return new VisualResourceValue(created, resource?.GetIcon());
        }

        private ResourceType ResolveResourceType(PlanetResourcesSo resource)
        {
            if (resource != null && resource.ResourceCategory != ResourceType.None)
            {
                return resource.ResourceCategory;
            }

            if (_resourceType != ResourceType.None)
            {
                return _resourceType;
            }

            if (defaultResource != null && defaultResource.ResourceCategory != ResourceType.None)
            {
                return defaultResource.ResourceCategory;
            }

            return ResourceType.PlanetResource;
        }
    }
}
