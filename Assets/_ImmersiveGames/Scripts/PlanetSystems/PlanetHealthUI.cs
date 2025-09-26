namespace _ImmersiveGames.Scripts.PlanetSystems
{
    /*public class PlanetHealthUI : ResourceHealthUI
    {
        private EventBinding<PlanetDestroyedEvent> _planetDestroyBinding; // Binding para eventos de recurso
        private PlanetsMaster _planetMaster;

        protected void Initialization()
        {
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
            if(evt.Detected.GetPlanetsMaster() != _planetMaster) return;
            //if (healthBar) healthBar.gameObject.SetActive(false);
            if (backgroundImage) backgroundImage.gameObject.SetActive(false);
        }
    }*/
}