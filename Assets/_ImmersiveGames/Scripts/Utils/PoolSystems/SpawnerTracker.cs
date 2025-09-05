using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public class SpawnerTracker : MonoBehaviour
    {
        public IActor Spawner { get; private set; }

        public void SetSpawner(IActor spawner)
        {
            Spawner = spawner;
        }
    }
}