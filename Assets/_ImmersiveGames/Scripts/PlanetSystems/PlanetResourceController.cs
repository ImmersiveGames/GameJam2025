using System;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    /// <summary>
    /// Componente responsável por armazenar o recurso do planeta e notificar interessados quando houver mudanças.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlanetResourceController : MonoBehaviour
    {
        [SerializeField] private PlanetResourcesSo defaultResource;

        private PlanetsMaster _master;
        private PlanetResourcesSo _currentResource;

        public event Action<PlanetResourceController, PlanetResourcesSo> ResourceAssigned;
        public event Action<PlanetResourceController, PlanetResourcesSo> ResourceCleared;

        public PlanetResourcesSo CurrentResource => _currentResource;

        private void Awake()
        {
            if (!TryGetComponent(out _master))
            {
                DebugUtility.LogError<PlanetResourceController>($"{name} precisa de um {nameof(PlanetsMaster)} no mesmo objeto.", this);
            }
        }

        private void OnEnable()
        {
            if (defaultResource != null && _currentResource == null)
            {
                AssignResource(defaultResource);
            }
            else
            {
                PublishCurrentState();
            }
        }

        public void AssignResource(PlanetResourcesSo resource)
        {
            if (resource == null)
            {
                ClearResource();
                return;
            }

            if (_currentResource == resource)
            {
                return;
            }

            _currentResource = resource;

            ResourceAssigned?.Invoke(this, _currentResource);

            if (_master != null)
            {
                EventBus<PlanetResourceAssignedEvent>.Raise(new PlanetResourceAssignedEvent(_master, _currentResource));
            }
        }

        public void ClearResource()
        {
            if (_currentResource == null)
            {
                return;
            }

            var previous = _currentResource;
            _currentResource = null;

            ResourceCleared?.Invoke(this, previous);

            if (_master != null)
            {
                EventBus<PlanetResourceClearedEvent>.Raise(new PlanetResourceClearedEvent(_master, previous));
            }
        }

        private void PublishCurrentState()
        {
            if (_currentResource != null)
            {
                ResourceAssigned?.Invoke(this, _currentResource);
            }
            else
            {
                ResourceCleared?.Invoke(this, null);
            }
        }
    }
}
