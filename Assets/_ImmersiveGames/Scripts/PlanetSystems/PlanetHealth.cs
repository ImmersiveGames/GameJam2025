using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    /*[DebugLevel(DebugLevel.Verbose)]
    public class PlanetHealth : HealthResource
    {
        private EventBinding<PlanetCreatedEvent> _planetCreateBinding;

        protected override void OnEnable()
        {
            base.OnEnable();
            _planetCreateBinding = new EventBinding<PlanetCreatedEvent>(OnPlanetCreated);
            EventBus<PlanetCreatedEvent>.Register(_planetCreateBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetCreatedEvent>.Unregister(_planetCreateBinding);
        }

        public override void OnDeath()
        {
            if (!TryGetComponent<PlanetsMaster>(out var planet)) return;
            base.OnDeath();
            EventBus<PlanetDestroyedEvent>.Raise(new PlanetDestroyedEvent(planet,lastChanger));
            PlanetsManager.Instance.RemovePlanet(planet);
            DebugUtility.LogVerbose<PlanetHealth>($"Planeta {planet.name} destruído por {lastChanger?.ActorName ?? "desconhecido"} e removido de PlanetsManager.", "yellow", this);
        }

        private void OnPlanetCreated(PlanetCreatedEvent obj)
        {
            Reset();
        }
    }*/
}