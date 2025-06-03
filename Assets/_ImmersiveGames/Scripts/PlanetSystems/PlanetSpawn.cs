using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class PlanetSpawn : SpawnPoint
    {
        protected override void Awake()
        {
            if (spawnData is not PlanetSpawnData planetSpawnData)
            {
                DebugUtility.LogError<PlanetSpawn>($"PlanetSpawn exige PlanetSpawnData, mas recebeu {spawnData?.GetType().Name}.");
                enabled = false;
                return;
            }
            DebugUtility.Log<PlanetSpawn>($"PlanetSpawn inicializado com PlanetSpawnData: {planetSpawnData.name}, count: {planetSpawnData.SpawnCount}.");
            base.Awake();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetTriggerActive(true);
            DebugUtility.Log<PlanetSpawn>($"PlanetSpawn {name} desativado, trigger reativado.");
        }
    }
}