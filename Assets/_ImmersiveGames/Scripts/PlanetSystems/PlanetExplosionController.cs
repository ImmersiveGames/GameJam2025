using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetExplosionController : DeathExplosionEffect
    {
        private EventBinding<PlanetConsumedEvent> _eventBinding;

        private IDetectable _detectable;

        protected override void Awake()
        {
            base.Awake();
            _detectable = GetComponent<IDetectable>();
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
            if (evt.Detected.Detectable != _detectable) return;
            DebugUtility.Log<PlanetExplosionController>("Explodindo planeta: " + evt.Detected.Detectable.Name, "yellow", this);
            EnableParticles();
        }
    }
}