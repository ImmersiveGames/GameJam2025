using System;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Core
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlanetsMaster))]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PlanetResourceModule : MonoBehaviour
    {
        [SerializeField] private PlanetResourcesSo defaultResource;

        private PlanetsMaster _planetMaster;
        private PlanetResourcesSo _currentResource;

        public event Action<PlanetResourcesSo> ResourceAssigned;
        public event Action ResourceCleared;

        public PlanetResourcesSo CurrentResource => _currentResource;
        public PlanetsMaster PlanetMaster => _planetMaster;
        public bool HasResource => _currentResource != null;

        private void Awake()
        {
            if (!TryGetComponent(out _planetMaster))
            {
                DebugUtility.LogError<PlanetResourceModule>(
                    $"PlanetResourceModule exige o componente PlanetsMaster em {gameObject.name}.",
                    gameObject);
            }
        }

        private void Start()
        {
            if (defaultResource != null && _currentResource == null && _planetMaster != null)
            {
                AssignResource(defaultResource);
            }
        }

        public bool AssignResource(PlanetResourcesSo resource)
        {
            if (resource == null)
            {
                DebugUtility.LogWarning<PlanetResourceModule>(
                    $"Tentativa de atribuir um recurso nulo ao planeta {_planetMaster?.name ?? gameObject.name}.",
                    gameObject);
                return false;
            }

            bool isSameResource = _currentResource == resource;
            _currentResource = resource;

            DebugUtility.LogVerbose<PlanetResourceModule>(
                $"Recurso {resource.ResourceType} atribuído ao planeta {_planetMaster?.name ?? gameObject.name}.",
                "cyan",
                this);

            ResourceAssigned?.Invoke(_currentResource);
            if (_planetMaster != null)
            {
                EventBus<PlanetResourceAssignedEvent>.Raise(
                    new PlanetResourceAssignedEvent(_planetMaster, _currentResource));
            }

            return !isSameResource;
        }

        public void ClearResource()
        {
            if (_currentResource == null)
            {
                return;
            }

            var previousResource = _currentResource;
            _currentResource = null;

            DebugUtility.LogVerbose<PlanetResourceModule>(
                $"Recurso removido do planeta {_planetMaster?.name ?? gameObject.name}.",
                "yellow",
                this);

            ResourceCleared?.Invoke();
            if (_planetMaster != null)
            {
                EventBus<PlanetResourceClearedEvent>.Raise(
                    new PlanetResourceClearedEvent(_planetMaster, previousResource));
            }
        }

        private void OnDestroy()
        {
            if (_planetMaster != null)
            {
                ClearResource();
            }
        }
    }
}
