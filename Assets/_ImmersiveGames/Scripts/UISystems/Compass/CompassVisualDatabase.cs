using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace _ImmersiveGames.Scripts.UISystems.Compass
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
        /// Quantidade de configurações registradas (facilita validações e testes).
        /// </summary>
        public int ConfigsCount => visualConfigs?.Count ?? 0;

        /// <summary>
        /// Retorna a configuração visual associada ao tipo de alvo informado.
        /// </summary>
        /// <param name="targetType">Tipo de alvo rastreável.</param>
        /// <returns>Config visual correspondente ou null se não encontrada.</returns>
        public CompassTargetVisualConfig GetConfig(CompassTargetType targetType)
        {
            return visualConfigs.FirstOrDefault(config => config != null && config.targetType == targetType);

        }
    }
}
