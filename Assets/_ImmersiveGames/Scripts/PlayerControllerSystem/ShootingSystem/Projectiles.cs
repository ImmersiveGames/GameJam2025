using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public class Projectiles : PooledObject
    {
        
        private void OnTriggerEnter(Collider other)
        {
            var destructible = other.GetComponentInParent<IDestructible>();
            DebugUtility.Log<Projectiles>($"Attempting to hit destructible: {destructible} with owner actor: {Spawner}");
            if (destructible is null) return;
            var data = GetData<ProjectilesData>();
            destructible.TakeDamage(data.damage, GetComponentInParent<IActor>());
            Deactivate();
        }
    }
}