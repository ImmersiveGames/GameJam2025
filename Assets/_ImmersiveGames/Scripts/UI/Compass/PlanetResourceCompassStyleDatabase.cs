using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.PlanetSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// Define a cor aplicada a cada tipo de recurso de planeta exibido na bússola.
    /// O tamanho permanece definido pelo CompassTargetVisualConfig do tipo de alvo.
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
        }

        [Tooltip("Lista de estilos aplicáveis por tipo de recurso.")]
        public List<PlanetResourceCompassStyleEntry> entries = new();

        /// <summary>
        /// Retorna a cor configurada para o tipo de recurso informado ou a cor padrão, quando não há entrada.
        /// </summary>
        public Color GetColorForResource(PlanetResources resourceType, Color defaultColor)
        {
            if (entries == null || entries.Count == 0)
            {
                return defaultColor;
            }

            foreach (var entry in entries.Where(entry => entry != null && entry.resourceType == resourceType))
            {
                return entry.iconColor;
            }

            return defaultColor;
        }
    }
}
