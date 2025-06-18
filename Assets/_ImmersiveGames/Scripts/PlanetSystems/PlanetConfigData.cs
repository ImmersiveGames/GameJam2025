using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [CreateAssetMenu(fileName = "PlanetConfigData", menuName = "SpawnSystem/PlanetConfigData")]
    public class PlanetConfigData : ScriptableObject
    {
        [Header("Planet Configuration")]
        [Tooltip("Lista de configurações possíveis para os planetas (ex.: tamanho, material).")]
        [SerializeField] private List<PlanetData> planetOptions = new List<PlanetData>();

        [Tooltip("Lista de recursos disponíveis para os planetas (ex.: minerais, água).")]
        [SerializeField] private List<PlanetResourcesSo> planetResources = new List<PlanetResourcesSo>();

        public List<PlanetData> PlanetOptions => planetOptions;
        public List<PlanetResourcesSo> PlanetResources => planetResources;

        private void OnValidate()
        {
            if (planetOptions == null || planetOptions.Count == 0)
            {
                Debug.LogWarning($"[{nameof(PlanetConfigData)}] PlanetOptions está vazio! Adicione pelo menos uma configuração de planeta.");
            }

            if (planetResources == null || planetResources.Count == 0)
            {
                Debug.LogWarning($"[{nameof(PlanetConfigData)}] PlanetResources está vazio! Adicione pelo menos um recurso.");
            }
        }
    }
}