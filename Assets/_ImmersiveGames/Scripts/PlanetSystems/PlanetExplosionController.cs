using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetExplosionController : DeathExplosionEffect
    {
        private EventBinding<PlanetDestroyedEvent> _eventBinding;

        private IDetectable _detectable;

        protected override void Awake()
        {
            base.Awake();
            _detectable = GetComponent<IDetectable>();
        }

        private void OnEnable()
        {
            _eventBinding = new EventBinding<PlanetDestroyedEvent>(OnPlanetConsumed);
            EventBus<PlanetDestroyedEvent>.Register(_eventBinding);
        }
        private void OnDisable()
        {
            DisableParticles();
            _eventBinding = new EventBinding<PlanetDestroyedEvent>(OnPlanetConsumed);
            EventBus<PlanetDestroyedEvent>.Register(_eventBinding);
        }
        private void OnPlanetConsumed(PlanetDestroyedEvent evt)
        {
            if (evt.Detected.Owner != _detectable) return;
            EnableParticles();
        }
    }
}