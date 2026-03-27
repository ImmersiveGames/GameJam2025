using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace _ImmersiveGames.Scripts.UISystems.Compass
{
    /// <summary>
    /// Banco de dados simples para buscar configuraÃ§Ãµes visuais por tipo de alvo.
    /// </summary>
    [CreateAssetMenu(fileName = "CompassVisualDatabase", menuName = "ImmersiveGames/Legacy/UI/Compass/Visual Database")]
    public class CompassVisualDatabase : ScriptableObject
    {
        [Tooltip("Lista de configuraÃ§Ãµes visuais disponÃ­veis para a bÃºssola.")]
        [SerializeField]
        private List<CompassTargetVisualConfig> visualConfigs = new();

        /// <summary>
        /// Quantidade de configuraÃ§Ãµes registradas (facilita validaÃ§Ãµes e testes).
        /// </summary>
        public int ConfigsCount => visualConfigs?.Count ?? 0;

        /// <summary>
        /// Retorna a configuraÃ§Ã£o visual associada ao tipo de alvo informado.
        /// </summary>
        /// <param name="targetType">Tipo de alvo rastreÃ¡vel.</param>
        /// <returns>Config visual correspondente ou null se nÃ£o encontrada.</returns>
        public CompassTargetVisualConfig GetConfig(CompassTargetType targetType)
        {
            return visualConfigs.FirstOrDefault(config => config != null && config.targetType == targetType);

        }
    }
}
