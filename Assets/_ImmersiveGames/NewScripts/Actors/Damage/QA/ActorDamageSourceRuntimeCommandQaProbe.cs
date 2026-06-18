using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Damage.QA
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Actors/Damage/Actor Damage Source Runtime Command QA Probe")]
    public sealed class ActorDamageSourceRuntimeCommandQaProbe : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private SessionActivityHost host;

        [Header("Damage Source")]
        [Tooltip("ActorId da fonte de dano. Ex.: 'actor.player.primary'.")]
        [SerializeField] private string sourceActorId = "actor.player.primary";

        [Header("Damage Target")]
        [Tooltip("ActorId do alvo damageable. Para E4A pode ser o mesmo actor da fonte.")]
        [SerializeField] private string targetActorId = "actor.player.primary";

        [Header("Damage Values")]
        [SerializeField] private float damageAmount = 25f;

        [ContextMenu("QA/Damage Source/Apply Damage")]
        public void QaApplyDamageFromSource()
        {
            RequireHost().QaDamageSourceActor(sourceActorId, targetActorId, damageAmount);
        }

        private SessionActivityHost RequireHost()
        {
            if (host != null)
            {
                return host;
            }

            throw new InvalidOperationException("[FATAL][ActorDamageSourceRuntimeCommandQaProbe] SessionActivityHost reference is required.");
        }

        private void OnValidate()
        {
            if (damageAmount < 0f)
            {
                damageAmount = 0f;
            }
        }

        private void Reset()
        {
            DebugUtility.LogVerbose(
                typeof(ActorDamageSourceRuntimeCommandQaProbe),
                $"event='ActorDamageSourceQaProbeReset' sourceActorId='{sourceActorId.TrimToEmpty()}' targetActorId='{targetActorId.TrimToEmpty()}' damageAmount='{damageAmount:0.###}' reason='component_reset_defaults'.");
        }
    }
}
