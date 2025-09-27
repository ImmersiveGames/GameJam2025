using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Configs
{
    [System.Serializable]
    public class ResourceInstanceConfig
    {
        [Tooltip("O SO base que define o recurso")]
        public ResourceDefinition resourceDefinition;
        
        [Tooltip("Canvas específico para esta instância do recurso")]
        public string targetCanvasId = "MainUI";
        
        [Tooltip("Configuração de auto-flow específica para esta instância")]
        public ResourceAutoFlowConfig autoFlowConfig;
        
        [Tooltip("Se esta instância tem auto-flow habilitado")]
        public bool hasAutoFlow = false;
        
        [Tooltip("Configuração de animação para este recurso")]
        public ResourceUIStyle animationStyle;
        
        [Tooltip("Se a animação está habilitada para este recurso")]
        public bool enableAnimation = true;
    }
}