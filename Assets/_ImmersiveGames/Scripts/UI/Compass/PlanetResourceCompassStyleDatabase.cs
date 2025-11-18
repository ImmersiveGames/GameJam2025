using System.Collections.Generic;
using _ImmersiveGames.Scripts.PlanetSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// Define cor e tamanho multiplicador para cada tipo de recurso de planeta exibido na bússola.
    /// </summary>
    [CreateAssetMenu(fileName = "PlanetResourceCompassStyleDatabase", menuName = "ImmersiveGames/UI/Compass/Planet Resource Style Database")]
    public class PlanetResourceCompassStyleDatabase : ScriptableObject
    {
        [System.Serializable]
        public class PlanetResourceCompassStyleEntry
        {
            [Tooltip("Tipo de recurso ao qual o estilo será aplicado.")]
            public PlanetResources resourceType;

            [Tooltip("Cor final aplicada ao ícone do recurso.")]
            public Color iconColor = Color.white;

            [Tooltip("Multiplicador de tamanho aplicado ao tamanho base configurado na bússola.")]
            public float sizeMultiplier = 1f;
        }

        [Tooltip("Lista de estilos aplicáveis por tipo de recurso.")]
        public List<PlanetResourceCompassStyleEntry> entries = new List<PlanetResourceCompassStyleEntry>();

        /// <summary>
        /// Retorna cor e tamanho calculados a partir do recurso informado, ou os valores base caso não exista estilo específico.
        /// </summary>
        public void GetStyleForResource(PlanetResources resourceType, Color baseColor, float baseSize, out Color finalColor, out float finalSize)
        {
            finalColor = baseColor;
            finalSize = baseSize;

            if (entries == null || entries.Count == 0)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                PlanetResourceCompassStyleEntry entry = entries[i];
                if (entry != null && entry.resourceType == resourceType)
                {
                    finalColor = entry.iconColor;
                    finalSize = baseSize * entry.sizeMultiplier;
                    return;
                }
            }
        }
    }
}
