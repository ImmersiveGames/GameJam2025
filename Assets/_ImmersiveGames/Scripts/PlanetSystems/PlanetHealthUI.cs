using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetHealthUI : ResourceHealthUI
    {
        private EventBinding<PlanetDestroyedEvent> _planetDestroyBinding; // Binding para eventos de recurso
        private PlanetsMaster _planetMaster;

        protected override void Initialization()
        {
            base.Initialization();
            _planetMaster = GetComponentInParent<PlanetsMaster>();
            _planetDestroyBinding = new EventBinding<PlanetDestroyedEvent>(OnPlanetDestroyed);
            EventBus<PlanetDestroyedEvent>.Register(_planetDestroyBinding);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_planetDestroyBinding != null)
            {
                EventBus<PlanetDestroyedEvent>.Unregister(_planetDestroyBinding);
            }
        }
        private void OnPlanetDestroyed(PlanetDestroyedEvent evt)
        {
            if(evt.Detectable.GetPlanetsMaster() != _planetMaster) return;
            if (healthBar) healthBar.gameObject.SetActive(false);
            if (backgroundImage) backgroundImage.gameObject.SetActive(false);
        }
    }
}