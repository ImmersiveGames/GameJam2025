using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Resources/Resource Link Config")]
    public class ResourceLinkConfig : ScriptableObject
    {
        [Header("Link Settings")]
        [Tooltip("Recurso fonte que será linkado")]
        public ResourceType sourceResource;

        [Tooltip("Recurso alvo que receberá o overflow")]
        public ResourceType targetResource;

        [Header("Transfer Behavior")]
        [Tooltip("Quando transferir: Always, WhenSourceEmpty, WhenSourceBelowThreshold")]
        public TransferCondition transferCondition = TransferCondition.WhenSourceEmpty;

        [Tooltip("Threshold para transferência (0-1)")]
        [Range(0f, 1f)]
        public float transferThreshold;

        [Tooltip("Direção da transferência: SourceToTarget, BothWays")]
        public TransferDirection transferDirection = TransferDirection.SourceToTarget;

        [Header("Auto-flow Integration")]
        [Tooltip("Se o auto-flow do recurso fonte deve afetar o alvo quando linkado")]
        public bool affectTargetWithAutoFlow = true;

        public enum TransferCondition
        {
            Always,
            WhenSourceEmpty,
            WhenSourceBelowThreshold
        }

        public enum TransferDirection
        {
            SourceToTarget,
            BothWays
        }

        public bool ShouldTransfer(float sourceCurrent, float sourceMax)
        {
            float percentage = sourceMax > 0 ? sourceCurrent / sourceMax : 0f;

            return transferCondition switch
            {
                TransferCondition.Always => true,
                TransferCondition.WhenSourceEmpty => sourceCurrent <= 0f,
                TransferCondition.WhenSourceBelowThreshold => percentage <= transferThreshold,
                _ => false
            };
        }
    }
}