using _ImmersiveGames.Scripts.SpawnSystems;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetSpawn : SpawnPoint
    {
        protected override void OnDisable()
        {
            base.OnDisable();
            SetTriggerActive(true);
        }
    }
}