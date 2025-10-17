using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlanetsMaster))]
    public class PlanetResourceController : MonoBehaviour
    {
        [SerializeField] private PlanetResourcesSo defaultResource;

        public PlanetResourcesSo CurrentResource { get; private set; }

        private PlanetsMaster _planetsMaster;
        private IActor _actor;

        private void Awake()
        {
            _planetsMaster = GetComponent<PlanetsMaster>();
            _actor = _planetsMaster;
            CurrentResource = defaultResource;
        }

        private void Start()
        {
            if (CurrentResource != null)
            {
                NotifyResourceChanged(CurrentResource);
            }
        }

        public void AssignResource(PlanetResourcesSo resource)
        {
            if (resource == null)
            {
                DebugUtility.LogWarning<PlanetResourceController>($"Recurso nulo atribuído ao planeta {_planetsMaster?.ActorName ?? name}.", this);
            }

            CurrentResource = resource;
            NotifyResourceChanged(resource);
        }

        public void ClearResource()
        {
            if (CurrentResource == null)
            {
                return;
            }

            CurrentResource = null;
            NotifyResourceChanged(null);
        }

        private void NotifyResourceChanged(PlanetResourcesSo resource)
        {
            if (_planetsMaster == null)
            {
                DebugUtility.LogWarning<PlanetResourceController>("PlanetsMaster ausente ao notificar recurso.", this);
                return;
            }

            var actorId = _actor?.ActorId;
            var evt = new PlanetResourceChangedEvent(_planetsMaster, resource, actorId);

            EventBus<PlanetResourceChangedEvent>.Raise(evt);

            if (!string.IsNullOrEmpty(actorId))
            {
                FilteredEventBus<PlanetResourceChangedEvent>.RaiseFiltered(evt, actorId);
            }
        }
    }
}
