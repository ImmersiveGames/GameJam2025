using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace ImmersiveGames.RuntimeAttributes.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Resources/Resource Link Config")]
    public class RuntimeAttributeLinkConfig : ScriptableObject
    {
        [Header("Link Settings")]
        [Tooltip("Recurso fonte que será linkado")]
        public RuntimeAttributeType sourceRuntimeAttribute;

        [Tooltip("Recurso alvo que receberá o overflow")]
        public RuntimeAttributeType targetRuntimeAttribute;

        [Header("Transfer Behavior")]
        [Tooltip("Quando transferir: Always, WhenSourceEmpty, WhenSourceBelowThreshold")]
        public readonly TransferCondition transferCondition = TransferCondition.WhenSourceEmpty;

        [Tooltip("Threshold para transferência (0-1)")]
        [Range(0f, 1f)]
        public float transferThreshold;

        [Tooltip("Direção da transferência: SourceToTarget, BothWays")]
        public TransferDirection transferDirection = TransferDirection.SourceToTarget;

        [Header("Auto-flow Integration")]
        [Tooltip("Se o auto-flow do recurso fonte deve afetar o alvo quando linkado")]
        public readonly bool affectTargetWithAutoFlow = true;

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
            if (sourceMax <= 0f)
            {
                DebugUtility.LogWarning<RuntimeAttributeLinkConfig>($"sourceMax é zero para {sourceRuntimeAttribute}. Transferência bloqueada.");
                return false;
            }

            float percentage = sourceCurrent / sourceMax;

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