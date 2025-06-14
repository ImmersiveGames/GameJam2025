using System.Collections.Generic;
using _ImmersiveGames.Scripts.SpawnSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [CreateAssetMenu(fileName = "PlanetSpawnData", menuName = "SpawnSystem/PlanetSpawnData")]
    public class PlanetSpawnData : SpawnData
    {
        [Header("Planet Configuration")]
        [Tooltip("Lista de configurações possíveis para os planetas (ex.: tamanho via planetScale, material). Deve conter pelo menos uma opção para suportar aleatoriedade.")]
        [SerializeField] private List<PlanetData> planetOptions = new List<PlanetData>();

        [Tooltip("Lista de recursos disponíveis para os planetas (ex.: minerais, água). Deve conter pelo menos um recurso para atribuição aleatória.")]
        [SerializeField] private List<PlanetResourcesSo> planetResources = new List<PlanetResourcesSo>();

        [Header("Orbit Settings")]
        [Tooltip("Raio inicial da órbita do primeiro planeta (em unidades do Unity). Ex.: 10 para começar a 10 unidades do centro. Use valores maiores para sistemas amplos.")]
        [Min(0f)]
        [SerializeField] private float initialOrbitRadius = 10f;

        [Tooltip("Centro da órbita no espaço (mantenha Y = 0 para órbitas planas). Ex.: (0, 0, 0) para a origem.")]
        [SerializeField] private Vector3 orbitCenter = Vector3.zero;

        [Tooltip("Espaço mínimo entre planetas (em unidades do Unity). Ex.: 2-5 para planetas pequenos, 5-10 para planetas com anéis ou meshes grandes.")]
        [Min(0f)]
        [SerializeField] private float spaceBetweenPlanets = 2f;

        // Getters para acesso seguro
        public List<PlanetData> PlanetOptions => planetOptions;
        public List<PlanetResourcesSo> PlanetResources => planetResources;
        public float InitialOrbitRadius => initialOrbitRadius;
        public Vector3 OrbitCenter => orbitCenter;
        public float SpaceBetweenPlanets => spaceBetweenPlanets;

        // Validação no Unity Editor
        private void OnValidate()
        {
            if (initialOrbitRadius < 0f)
            {
                Debug.LogWarning($"[{nameof(PlanetSpawnData)}] InitialOrbitRadius deve ser maior ou igual a 0. Ajustado para 0.");
                initialOrbitRadius = 0f;
            }

            if (spaceBetweenPlanets < 0f)
            {
                Debug.LogWarning($"[{nameof(PlanetSpawnData)}] SpaceBetweenPlanets deve ser maior ou igual a 0. Ajustado para 0.");
                spaceBetweenPlanets = 0f;
            }

            if (planetOptions == null || planetOptions.Count == 0)
            {
                Debug.LogWarning($"[{nameof(PlanetSpawnData)}] PlanetOptions está vazio! Adicione pelo menos uma configuração de planeta para suportar aleatoriedade.");
            }

            if (planetResources == null || planetResources.Count == 0)
            {
                Debug.LogWarning($"[{nameof(PlanetSpawnData)}] PlanetResources está vazio! Adicione pelo menos um recurso para atribuição aleatória.");
            }
        }
    }
}