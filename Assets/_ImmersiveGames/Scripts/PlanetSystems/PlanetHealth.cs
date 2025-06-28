using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.EaterSystem;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Verbose)]
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

        protected override void OnDeath(IActor byActor = null)
        {
            base.OnDeath(byActor);
            if (!TryGetComponent<PlanetsMaster>(out var planet)) return;
            if (byActor is not EaterMaster) return;
            EventBus<PlanetConsumedEvent>.Raise(new PlanetConsumedEvent(planet));
            PlanetsManager.Instance.RemovePlanet(planet);
            DebugUtility.Log<PlanetHealth>($"Planeta {planet.name} destruído e removido de PlanetsManager.", "yellow", this);
        }

        private void OnPlanetCreated(PlanetCreatedEvent obj)
        {
            Reset();
        }
    }
}