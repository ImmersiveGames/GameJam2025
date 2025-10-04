using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Managers
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PlanetMarkingManager
    {
        private static PlanetMarkingManager _instance;
        public static PlanetMarkingManager Instance => _instance ??= new PlanetMarkingManager();

        private MarkPlanet _currentlyMarkedPlanet;
        private EventBinding<PlanetMarkedEvent> _markedBinding;
        private EventBinding<PlanetUnmarkedEvent> _unmarkedBinding;

        public MarkPlanet CurrentlyMarkedPlanet => _currentlyMarkedPlanet;
        public IActor CurrentlyMarkedPlanetActor => _currentlyMarkedPlanet?.PlanetActor;
        public bool HasMarkedPlanet => _currentlyMarkedPlanet != null;

        private PlanetMarkingManager()
        {
            Initialize();
        }

        private void Initialize()
        {
            _markedBinding = new EventBinding<PlanetMarkedEvent>(OnPlanetMarked);
            _unmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnPlanetUnmarked);

            EventBus<PlanetMarkedEvent>.Register(_markedBinding);
            EventBus<PlanetUnmarkedEvent>.Register(_unmarkedBinding);
        }

        private void OnPlanetMarked(PlanetMarkedEvent markedEvent)
        {
            var previousMarked = _currentlyMarkedPlanet;
            
            // JÁ FAZ O QUE PRECISA: desmarca anterior se for diferente do novo
            if (_currentlyMarkedPlanet != null && _currentlyMarkedPlanet != markedEvent.MarkPlanet)
            {
                _currentlyMarkedPlanet.Unmark(); // ← FUNCIONA!
            }

            _currentlyMarkedPlanet = markedEvent.MarkPlanet;

            // Notifica outros sistemas via EventBus
            EventBus<PlanetMarkingChangedEvent>.Raise(new PlanetMarkingChangedEvent(
                markedEvent.PlanetActor,
                previousMarked?.PlanetActor
            ));
        }

        private void OnPlanetUnmarked(PlanetUnmarkedEvent unmarkedEvent)
        {
            if (_currentlyMarkedPlanet == unmarkedEvent.MarkPlanet)
            {
                _currentlyMarkedPlanet = null;
            }
        }

        public bool TryMarkPlanet(GameObject planetObject)
        {
            if (planetObject == null) return false;

            var markPlanet = planetObject.GetComponentInParent<MarkPlanet>();
            if (markPlanet == null) return false;

            markPlanet.ToggleMark();
            return true;
        }

        public void ClearAllMarks()
        {
            if (_currentlyMarkedPlanet != null)
            {
                _currentlyMarkedPlanet.Unmark();
            }
        }

        public void Dispose()
        {
            EventBus<PlanetMarkedEvent>.Unregister(_markedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_unmarkedBinding);
            _currentlyMarkedPlanet = null;
        }
    }
}