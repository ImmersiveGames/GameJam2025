using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// Banco de dados simples para buscar configurações visuais por tipo de alvo.
    /// </summary>
    [CreateAssetMenu(fileName = "CompassVisualDatabase", menuName = "ImmersiveGames/UI/Compass/Visual Database")]
    public class CompassVisualDatabase : ScriptableObject
    {
        [Tooltip("Lista de configurações visuais disponíveis para a bússola.")]
        [SerializeField]
        private List<CompassTargetVisualConfig> visualConfigs = new();

        /// <summary>
        /// Retorna a configuração visual associada ao tipo de alvo informado.
        /// </summary>
        /// <param name="targetType">Tipo de alvo rastreável.</param>
        /// <returns>Config visual correspondente ou null se não encontrada.</returns>
        public CompassTargetVisualConfig GetConfig(CompassTargetType targetType)
        {
            foreach (CompassTargetVisualConfig config in visualConfigs)
            {
                if (config != null && config.targetType == targetType)
                {
                    return config;
                }
            }

            return null;
        }
    }
}
