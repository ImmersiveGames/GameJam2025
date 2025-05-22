using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ScriptableObjects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetResourceManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Lista de configurações de planetas disponíveis para sorteio")]
        private List<PlanetData> planetDataPrefabs;

        private void OnValidate()
        {
            // Verifica se a lista de PlanetData está configurada corretamente
            if (planetDataPrefabs == null || planetDataPrefabs.Count == 0)
            {
                Debug.LogWarning("A lista de PlanetData está vazia no PlanetResourceManager.");
            }
            else
            {
                foreach (var data in planetDataPrefabs)
                {
                    if (data == null)
                    {
                        Debug.LogWarning("Um item na lista de PlanetData é nulo.");
                    }
                    else if (data.enemyModel == null)
                    {
                        Debug.LogWarning($"PlanetData não tem um enemyModel definido.");
                    }
                }
            }
        }

        public List<PlanetResources> GenerateResourceList(int numPlanets)
        {
            var resourcesList = new List<PlanetResources>(Enum.GetValues(typeof(PlanetResources)) as PlanetResources[] ?? Array.Empty<PlanetResources>());
            var resourceDistribution = new List<PlanetResources>(resourcesList); // Garante um de cada recurso

            // Preenche o restante com recursos aleatórios
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

            int index = Random.Range(0, planetDataPrefabs.Count);
            return planetDataPrefabs[index];
        }
    }
}