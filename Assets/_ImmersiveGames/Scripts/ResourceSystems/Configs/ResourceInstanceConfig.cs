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

        [Tooltip("Se esta instância tem auto-flow habilitado")]
        public bool hasAutoFlow = false;

        [Tooltip("Configuração de animação/visual para este recurso")]
        public ResourceUIStyle animationStyle;

        [Tooltip("Estratégia de preenchimento (ScriptableObject). Se null, usa InstantFillStrategy padrão.")]
        public ResourceFillStrategy animationStrategy;

        [Tooltip("Se a animação está habilitada para este recurso")]
        public bool enableAnimation = true;

        [Tooltip("Configuração de thresholds para este recurso")]
        public ResourceThresholdConfig thresholdConfig;

        [Tooltip("Configuração de auto-flow específica para esta instância")]
        public ResourceAutoFlowConfig autoFlowConfig;
    }

    public enum CanvasTargetMode
    {
        Default,       // "MainUI"
        ActorSpecific, // "{actorId}_Canvas"
        Custom         // customCanvasId
    }
}