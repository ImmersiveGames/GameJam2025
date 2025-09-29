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
        public string customCanvasId; // Used only if mode == Custom
        
        [Tooltip("Configuração de auto-flow específica para esta instância")]
        public ResourceAutoFlowConfig autoFlowConfig;
        
        [Tooltip("Se esta instância tem auto-flow habilitado")]
        public bool hasAutoFlow = false;
        
        [Tooltip("Configuração de animação para este recurso")]
        public ResourceUIStyle animationStyle;
        
        [Tooltip("Se a animação está habilitada para este recurso")]
        public bool enableAnimation = true;
        [Tooltip("Configuração de thresholds para este recurso")]
        public ResourceThresholdConfig thresholdConfig;
        
        [Tooltip("Se o monitoramento de thresholds está habilitado para este recurso")]
        public bool enableThresholdMonitoring = true;
    }
    public enum CanvasTargetMode
    {
        Default,      // Uses "MainUI"
        ActorSpecific, // Uses "{actorId}_Canvas"
        Custom        // Uses customCanvasId string
    }
}