using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class Planet : MonoBehaviour
    {
        private PlanetData _planetData;
        private PlanetOrbitController _orbitController;
        private PooledObject _pooledObject;
        private bool _isInitialized;

        public void Initialize(PlanetData data, Transform orbitCenter, float orbitSpeed, bool orbitClockwise)
        {
            _planetData = data;
            _pooledObject = GetComponent<PooledObject>();
            _orbitController = GetComponent<PlanetOrbitController>();

            if (!_pooledObject)
            {
                Debug.LogError($"PooledObject não encontrado em {name}.", this);
                gameObject.SetActive(false);
                return;
            }
            if (!_orbitController)
            {
                Debug.LogError($"PlanetOrbitController não encontrado em {name}.", this);
                gameObject.SetActive(false);
                return;
            }

            _orbitController.Initialize(orbitCenter, orbitSpeed, orbitSpeed, orbitClockwise);
            _isInitialized = true;
        }

        public void DestroyPlanet()
        {
            if (!_isInitialized) return;
            _orbitController.StopOrbit();
            EventBus<PlanetDestroyedEvent>.Raise(new PlanetDestroyedEvent(this));
            _pooledObject.ReturnToPool();
            _isInitialized = false;
        }

        public void ResetState()
        {
            if (!_isInitialized) return;
            _orbitController.ResetState();
            _isInitialized = false;
        }
    }
}