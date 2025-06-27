using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetExplosionController : DeathExplosionEffect
    {
        private EventBinding<PlanetConsumedEvent> _eventBinding;
        private PlanetsMaster _planetMaster;

        protected override void Awake()
        {
            base.Awake();
            _planetMaster = GetComponent<PlanetsMaster>();
        }

        private void OnEnable()
        {
            _eventBinding = new EventBinding<PlanetConsumedEvent>(OnPlanetConsumed);
            EventBus<PlanetConsumedEvent>.Register(_eventBinding);
        }
        private void OnDisable()
        {
            DisableParticles();
            _eventBinding = new EventBinding<PlanetConsumedEvent>(OnPlanetConsumed);
            EventBus<PlanetConsumedEvent>.Register(_eventBinding);
        }
        private void OnPlanetConsumed(PlanetConsumedEvent evt)
        {
            if (evt.Detectable.Name != gameObject.name) return;
            DebugUtility.Log<PlanetExplosionController>("Explodindo planeta: " + evt.Detectable.Name, "yellow", this);
            EnableParticles();
        }
    }
}