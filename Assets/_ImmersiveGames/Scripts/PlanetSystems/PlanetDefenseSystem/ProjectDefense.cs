using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.PlanetDefenseSystem
{
    public class ProjectDefense : MonoBehaviour
    {
        private PooledObject _pooledObject;
        private void Awake()
        {
            _pooledObject = GetComponent<PooledObject>();
        }
        private void OnTriggerEnter(Collider other)
        {
            var destructible = other.GetComponentInParent<IDestructible>();
            
            Debug.Log("Trigger Entered with: " + destructible);
            if (destructible is null or PlanetHealth) return;
            var data = _pooledObject.GetData<ProjectilesData>();
            destructible?.TakeDamage(data.damage);
            _pooledObject.Deactivate();
        }
    }
}