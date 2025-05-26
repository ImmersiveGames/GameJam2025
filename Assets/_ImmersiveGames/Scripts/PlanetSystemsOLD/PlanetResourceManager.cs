using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _ImmersiveGames.Scripts.PlanetSystemsOLD
{
    public class PlanetResourceManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Lista de configurações de planetas disponíveis para sorteio")]
        public List<PlanetData> planetDataPrefabs;

        private void OnValidate()
        {
            if (planetDataPrefabs == null || planetDataPrefabs.Count == 0)
            {
                Debug.LogWarning("A lista de PlanetData está vazia no PlanetResourceManager.");
            }
        }

        public List<PlanetResources> GenerateResourceList(int numPlanets)
        {
            var resourcesList = new List<PlanetResources>(Enum.GetValues(typeof(PlanetResources)) as PlanetResources[] ?? Array.Empty<PlanetResources>());
            if (resourcesList.Count == 0)
            {
                Debug.LogError("Nenhum PlanetResources definido.");
                return new List<PlanetResources>();
            }

            var resourceDistribution = new List<PlanetResources>(resourcesList); // Garante um de cada recurso
            while (resourceDistribution.Count < numPlanets)
            {
                resourceDistribution.Add(resourcesList[Random.Range(0, resourcesList.Count)]);
            }

            // Embaralhamento Fisher-Yates
            for (int i = resourceDistribution.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (resourceDistribution[i], resourceDistribution[j]) = (resourceDistribution[j], resourceDistribution[i]);
            }

            return resourceDistribution;
        }

        public PlanetData GetRandomPlanetData()
        {
            if (planetDataPrefabs == null || planetDataPrefabs.Count == 0)
            {
                Debug.LogError("Nenhum PlanetData configurado no PlanetResourceManager.");
                return null;
            }

            return planetDataPrefabs[Random.Range(0, planetDataPrefabs.Count)];
        }
    }
}