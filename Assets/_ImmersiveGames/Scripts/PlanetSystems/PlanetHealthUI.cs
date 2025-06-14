using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetHealthUI : ResourceHealthUI
    {
        private EventBinding<PlanetConsumedEvent> _planetDestroyBinding; // Binding para eventos de recurso
        private PlanetsMaster _planetMaster;

        protected override void Initialization()
        {
            base.Initialization();
            _planetMaster = GetComponentInParent<PlanetsMaster>();
            _planetDestroyBinding = new EventBinding<PlanetConsumedEvent>(OnPlanetDestroyed);
            EventBus<PlanetConsumedEvent>.Register(_planetDestroyBinding);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_planetDestroyBinding != null)
            {
                EventBus<PlanetConsumedEvent>.Unregister(_planetDestroyBinding);
            }
        }
        private void OnPlanetDestroyed(PlanetConsumedEvent evt)
        {
            if(evt.Detectable.GetPlanetsMaster() != _planetMaster) return;
            if (healthBar) healthBar.gameObject.SetActive(false);
            if (backgroundImage) backgroundImage.gameObject.SetActive(false);
        }
    }
}