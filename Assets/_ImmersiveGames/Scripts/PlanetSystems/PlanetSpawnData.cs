using System.Collections.Generic;
using _ImmersiveGames.Scripts.SpawnSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [CreateAssetMenu(fileName = "PlanetSpawnData", menuName = "SpawnSystem/PlanetSpawnData")]
    public class PlanetSpawnData : SpawnData
    {
        [SerializeField] private List<PlanetData> planetOptions; // Opções de dados de planetas
        [SerializeField] private List<PlanetResourcesSo> planetResources; // Recursos disponíveis
        [SerializeField] private float spaceBetweenPlanets = 2f; // Espaço mínimo entre planetas
        [SerializeField] private float initialOrbitRadius = 10f; // Raio inicial da órbita
        [SerializeField] private Vector3 orbitCenter = Vector3.zero; // Centro da órbita (Y = 0)

        public List<PlanetData> PlanetOptions => planetOptions;
        public List<PlanetResourcesSo> PlanetResources => planetResources;
        public float SpaceBetweenPlanets => spaceBetweenPlanets;
        public float InitialOrbitRadius => initialOrbitRadius;
        public Vector3 OrbitCenter => orbitCenter;
    }
}