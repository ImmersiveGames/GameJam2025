using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public class Projectiles : MonoBehaviour
    {      
        private PooledObject _pooledObject;
        private void Awake()
        {
            _pooledObject = GetComponent<PooledObject>();
        }
        
        private void OnTriggerEnter(Collider other)
        {
            var destructible = other.GetComponentInParent<IDestructible>();
            DebugUtility.Log<Projectiles>($"Attempting to hit destructible: {destructible} with owner actor: {_pooledObject}");
            if (destructible is null) return;
            //var data = _pooledObject.GetData<ProjectilesData>();
            //destructible.TakeDamage(data.damage, _pooledObject.GetComponentInParent<IActor>());
            _pooledObject.Deactivate();
        }
    }
}