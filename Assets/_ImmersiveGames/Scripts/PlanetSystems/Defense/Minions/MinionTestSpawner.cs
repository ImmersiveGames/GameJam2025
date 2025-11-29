using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    public class MinionTestSpawner : MonoBehaviour
    {
        public DefenseMinionController controller;
        public Transform planet;

        private void Start()
        {
            var planetCenter = planet.position;
            var orbitPos = planet.position + new Vector3(0f, 0f, 3f);
            controller.BeginEntryPhase(planetCenter, orbitPos, "PlanetDefenseDetector");
        }
    }
}