using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
{
    namespace _ImmersiveGames.Scripts.Utils.PoolSystems
    {
        public interface ISpawnStrategy
        {
            void Spawn(PoolManager poolManager, SpawnParameters parameters);
        }
    }
}