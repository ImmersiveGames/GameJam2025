using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core
{
    internal enum AudioSfxBlockPolicy
    {
        None = 0,
        Cooldown = 1,
        SimultaneousLimit = 2
    }

    internal readonly struct AudioSfxDirectPolicyDecision
    {
        public AudioSfxDirectPolicyDecision(
            bool shouldBlock,
            AudioSfxBlockPolicy blockPolicy,
            bool restartedExisting,
            bool previousHandleStopped)
        {
            ShouldBlock = shouldBlock;
            BlockPolicy = blockPolicy;
            RestartedExisting = restartedExisting;
            PreviousHandleStopped = previousHandleStopped;
        }

        public bool ShouldBlock { get; }
        public AudioSfxBlockPolicy BlockPolicy { get; }
        public bool RestartedExisting { get; }
        public bool PreviousHandleStopped { get; }
    }

    internal static class AudioSfxDirectPolicyEngine
    {
        public static AudioSfxDirectPolicyDecision Evaluate(
            AudioSfxCueAsset cue,
            bool useSpatial,
            float now,
            float? lastPlayTime,
            int activeInstances,
            bool hasActive2dHandle,
            bool previousHandleStopped)
        {
            bool isGlobal2dRequest = !useSpatial;
            bool restartedExisting = isGlobal2dRequest && hasActive2dHandle;

            float cooldown = Mathf.Max(0f, cue.SfxRetriggerCooldownSeconds);
            if (!restartedExisting && cooldown > 0f && lastPlayTime.HasValue)
            {
                float elapsed = now - lastPlayTime.Value;
                if (elapsed < cooldown)
                {
                    return new AudioSfxDirectPolicyDecision(
                        shouldBlock: true,
                        blockPolicy: AudioSfxBlockPolicy.Cooldown,
                        restartedExisting: false,
                        previousHandleStopped: false);
                }
            }

            int maxSimultaneous = Mathf.Max(1, cue.MaxSimultaneousInstances);
            if (!restartedExisting && activeInstances >= maxSimultaneous)
            {
                return new AudioSfxDirectPolicyDecision(
                    shouldBlock: true,
                    blockPolicy: AudioSfxBlockPolicy.SimultaneousLimit,
                    restartedExisting: false,
                    previousHandleStopped: false);
            }

            return new AudioSfxDirectPolicyDecision(
                shouldBlock: false,
                blockPolicy: AudioSfxBlockPolicy.None,
                restartedExisting: restartedExisting,
                previousHandleStopped: restartedExisting && previousHandleStopped);
        }
    }

    internal enum AudioSfxPooledDecisionType
    {
        Proceed = 0,
        FallbackToDirect = 1,
        Blocked = 2
    }

    internal readonly struct AudioSfxPooledPolicyDecision
    {
        public AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType type, string reason)
        {
            Type = type;
            Reason = reason;
        }

        public AudioSfxPooledDecisionType Type { get; }
        public string Reason { get; }
    }

    internal static class AudioSfxPooledPolicyEngine
    {
        public static AudioSfxPooledPolicyDecision EvaluateProfile(AudioSfxVoiceProfileAsset profile)
        {
            if (profile != null)
            {
                return new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.Proceed, "profile_ok");
            }

            return new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.FallbackToDirect, "missing_profile");
        }

        public static AudioSfxPooledPolicyDecision EvaluatePoolDefinition(AudioSfxVoiceProfileAsset profile)
        {
            if (profile == null)
            {
                return new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.FallbackToDirect, "missing_profile");
            }

            if (profile.PooledVoicePoolDefinition != null)
            {
                return new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.Proceed, "pool_ok");
            }

            return profile.AllowDirectFallback
                ? new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.FallbackToDirect, "missing_pool_definition")
                : new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.Blocked, "missing_pool_definition");
        }

        public static AudioSfxPooledPolicyDecision EvaluatePoolService(AudioSfxVoiceProfileAsset profile, bool hasPoolService)
        {
            if (hasPoolService)
            {
                return new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.Proceed, "pool_service_ok");
            }

            return profile != null && profile.AllowDirectFallback
                ? new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.FallbackToDirect, "pool_service_unavailable")
                : new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.Blocked, "pool_service_unavailable");
        }

        public static AudioSfxPooledPolicyDecision EvaluateBudget(AudioSfxVoiceProfileAsset profile, int activeForProfile)
        {
            if (profile == null)
            {
                return new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.FallbackToDirect, "missing_profile");
            }

            int budget = Mathf.Max(0, profile.DefaultVoiceBudget);
            if (budget <= 0 || activeForProfile < budget)
            {
                return new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.Proceed, "budget_ok");
            }

            return profile.AllowDirectFallback
                ? new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.FallbackToDirect, "block_budget")
                : new AudioSfxPooledPolicyDecision(AudioSfxPooledDecisionType.Blocked, "block_budget");
        }
    }
}

