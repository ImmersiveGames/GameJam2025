using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Configs
{
    [System.Serializable]
    public class ResourceInstanceConfig
    {
        [Tooltip("O SO base que define o recurso")]
        public ResourceDefinition resourceDefinition;

        [Tooltip("Modo de destino do Canvas para este recurso")]
        public CanvasTargetMode canvasTargetMode = CanvasTargetMode.Default;

        [Tooltip("Canvas específico para esta instância do recurso")]
        public string customCanvasId;

        [Tooltip("Configuração de animação/visual para este recurso")]
        public ResourceUIStyle slotStyle;

        [Tooltip("Configuração de thresholds para este recurso")]
        public ResourceThresholdConfig thresholdConfig;
        
        [Tooltip("Se esta instância tem auto-flow habilitado")]
        public bool hasAutoFlow;
        
        [Tooltip("Configuração de auto-flow específica para esta instância")]
        public ResourceAutoFlowConfig autoFlowConfig;

        [Header("Slot Display")]
        [Tooltip("Ordem de exibição no canvas (quanto maior, mais na frente)")]
        public int sortOrder;
    }
}