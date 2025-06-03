using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.SpawnSystems.Strategies;
using _ImmersiveGames.Scripts.SpawnSystems.Triggers;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PlanetSpawn : SpawnPoint
    {
        protected override void Awake()
        {
            if (!(spawnData is PlanetSpawnData planetSpawnData))
            {
                Debug.LogError($"PlanetSpawn exige PlanetSpawnData, mas recebeu {spawnData?.GetType().Name}.");
                enabled = false;
                return;
            }
            Debug.Log($"PlanetSpawn inicializado com PlanetSpawnData: {planetSpawnData.name}, count: {planetSpawnData.SpawnCount}.");
            base.Awake();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetTriggerActive(true);
            Debug.Log($"PlanetSpawn {name} desativado, trigger reativado.");
        }
    }
}