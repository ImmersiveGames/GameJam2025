using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Core.Interfaces;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.UI.Configs;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs
{
    [System.Serializable]
    public class RuntimeAttributeInstanceConfig
    {
        [Tooltip("O SO base que define o recurso")]
        public RuntimeAttributeDefinition runtimeAttributeDefinition;

        [Tooltip("Modo de destino do Canvas para este recurso")]
        public AttributeCanvasTargetMode attributeCanvasTargetMode = AttributeCanvasTargetMode.Default;

        [Tooltip("Canvas específico para esta instância do recurso")]
        public string customCanvasId;

        [Tooltip("Configuração de animação/visual para este recurso")]
        public RuntimeAttributeUIStyle slotStyle;

        [Tooltip("Configuração de thresholds para este recurso")]
        public RuntimeAttributeThresholdConfig thresholdConfig;
        
        [Tooltip("Se esta instância tem auto-flow habilitado")]
        public bool hasAutoFlow;
        
        [Tooltip("Configuração de auto-flow específica para esta instância")]
        public RuntimeAttributeAutoFlowConfig autoFlowConfig;

        [Header("Slot Display")]
        [Tooltip("Ordem de exibição no attributeCanvas (quanto maior, mais na frente)")]
        public int sortOrder;
    }
}