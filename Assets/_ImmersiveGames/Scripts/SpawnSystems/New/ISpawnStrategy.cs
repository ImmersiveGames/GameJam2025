using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.New
{
    public enum CircleRotationMode
    {
        InheritFromSpawner, // Herda rotação do spawner
        LookToCenter,       // Olha para o centro do círculo
        LookAwayFromCenter  // Olha para fora do centro (spread)
    }
    public interface ISpawnStrategy
    {
        void Execute(ObjectPool pool, Transform spawnerTransform, bool exhaust, IActor actor = null, SpawnSystem spawnSystem = null);
        void SetCenterTransform(Transform newCenter);
    }
}