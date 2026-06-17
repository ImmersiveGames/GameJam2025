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

        [Header("Damage Target")]
        [FormerlySerializedAs("actorId")]
        [Tooltip("Use o ActorId ativo. Ex.: 'actor.player.primary'. Nao use PlayerSlotId, PlayerActorId ou ActorInstanceRuntimeId.")]
        [SerializeField] private string targetActorId = "actor.player.primary";

        [Header("Damage Values")]
        [SerializeField] private float damageAmount = 25f;

        [ContextMenu("QA/Damage/Apply Damage")]
        public void QaApplyDamage()
        {
            RequireHost().QaDamageActor(targetActorId, damageAmount);
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
        }

        private void Reset()
        {
            DebugUtility.LogVerbose(
                typeof(ActorDamageRuntimeCommandQaProbe),
                $"event='ActorDamageQaProbeReset' targetActorId='{Normalize(targetActorId)}' damageAmount='{damageAmount:0.###}' reason='component_reset_defaults'.");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
