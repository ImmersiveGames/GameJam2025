using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline;
using UnityEngine;
using UnityEngine.Serialization;

namespace _ImmersiveGames.NewScripts.Actors.Damage.QA
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Actors/Damage/Actor Damage Runtime Command QA Probe")]
    public sealed class ActorDamageRuntimeCommandQaProbe : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private SessionActivityHost host;

        [Header("Damage Source")]
        [Tooltip("ActorId da fonte de dano. Para smoke inicial pode ser o mesmo actor do alvo.")]
        [SerializeField] private string sourceActorId = "actor.player.primary";

        [Header("Damage Target")]
        [FormerlySerializedAs("actorId")]
        [Tooltip("Use o ActorId ativo. Ex.: 'actor.player.primary'. Nao use PlayerSlotId, PlayerActorId ou ActorInstanceRuntimeId.")]
        [SerializeField] private string targetActorId = "actor.player.primary";

        [Header("Damage Values")]
        [SerializeField] private float damageAmount = 25f;
        [SerializeField] private int damageSequenceStepCount = 4;

        [ContextMenu("QA/Damage/Apply Damage")]
        public void QaApplyDamage()
        {
            RequireHost().QaDamageActor(targetActorId, damageAmount);
        }

        [ContextMenu("QA/Damage Source/Apply Damage")]
        public void QaApplyDamageFromSource()
        {
            RequireHost().QaDamageSourceActor(sourceActorId, targetActorId, damageAmount);
        }

        [ContextMenu("QA/Damage/Apply Damage Sequence To Depleted")]
        public void QaApplyDamageSequenceToDepleted()
        {
            SessionActivityHost resolvedHost = RequireHost();
            string normalizedActorId = Normalize(targetActorId);
            int appliedCount = 0;
            int rejectedCount = 0;

            DebugUtility.LogVerbose(
                typeof(ActorDamageRuntimeCommandQaProbe),
                $"event='ActorDamageQaSequenceStarted' actorId='{normalizedActorId}' damageAmount='{damageAmount:0.###}' stepCount='{damageSequenceStepCount}' reason='damageable_threshold_smoke_sequence'.");

            for (int index = 0; index < damageSequenceStepCount; index++)
            {
                bool applied = resolvedHost.QaDamageActor(normalizedActorId, damageAmount);
                if (applied)
                {
                    appliedCount++;
                }
                else
                {
                    rejectedCount++;
                }

                DebugUtility.LogVerbose(
                    typeof(ActorDamageRuntimeCommandQaProbe),
                    $"event='ActorDamageQaSequenceStepCompleted' actorId='{normalizedActorId}' stepIndex='{index + 1}' stepCount='{damageSequenceStepCount}' applied='{applied}' damageAmount='{damageAmount:0.###}' reason='damageable_threshold_smoke_sequence_step'.");
            }

            DebugUtility.Log(
                typeof(ActorDamageRuntimeCommandQaProbe),
                $"event='ActorDamageQaSequenceCompleted' actorId='{normalizedActorId}' damageAmount='{damageAmount:0.###}' stepCount='{damageSequenceStepCount}' appliedCount='{appliedCount}' rejectedCount='{rejectedCount}' reason='damageable_threshold_smoke_sequence_completed'.");
        }

        private SessionActivityHost RequireHost()
        {
            if (host != null)
            {
                return host;
            }

            throw new InvalidOperationException("[FATAL][ActorDamageRuntimeCommandQaProbe] SessionActivityHost reference is required.");
        }

        private void OnValidate()
        {
            if (damageAmount < 0f)
            {
                damageAmount = 0f;
            }

            if (damageSequenceStepCount < 1)
            {
                damageSequenceStepCount = 1;
            }
        }

        private void Reset()
        {
            DebugUtility.LogVerbose(
                typeof(ActorDamageRuntimeCommandQaProbe),
                $"event='ActorDamageQaProbeReset' sourceActorId='{Normalize(sourceActorId)}' targetActorId='{Normalize(targetActorId)}' damageAmount='{damageAmount:0.###}' damageSequenceStepCount='{damageSequenceStepCount}' reason='component_reset_defaults'.");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
