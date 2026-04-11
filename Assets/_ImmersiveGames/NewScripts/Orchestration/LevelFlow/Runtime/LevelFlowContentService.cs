using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime
{
    // Active seam for manifest resolution; the surrounding historical folder remains compatibility-only.
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelFlowContentService : ILevelFlowContentService
    {
        public PhaseDefinitionAsset.PhaseSwapBlock ResolvePhaseSwapOrFail(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            string signature,
            string reason)
        {
            if (phaseDefinitionRef == null)
            {
                FailFastConfig(macroRouteId, SceneRouteKind.Gameplay, signature, reason, "PhaseDefinitionRef is null.");
            }

            phaseDefinitionRef.ValidateOrFail($"PhaseSwap routeId='{macroRouteId}' reason='{reason}'");

            PhaseDefinitionAsset.PhaseSwapBlock swap = phaseDefinitionRef.Swap;
            if (swap == null)
            {
                FailFastConfig(macroRouteId, SceneRouteKind.Gameplay, signature, reason, $"Phase swap block is null for phaseRef='{phaseDefinitionRef.name}'.");
            }

            if (swap.contentManifest == null)
            {
                FailFastConfig(macroRouteId, SceneRouteKind.Gameplay, signature, reason, $"Phase swap contentManifest is null for phaseRef='{phaseDefinitionRef.name}'.");
            }

            if (!swap.contentManifest.TryValidateRuntime(out string manifestError))
            {
                FailFastConfig(macroRouteId, SceneRouteKind.Gameplay, signature, reason, $"Phase swap contentManifest invalid for phaseRef='{phaseDefinitionRef.name}'. detail='{manifestError}'.");
            }

            if (swap.additiveScenes == null || swap.additiveScenes.Count == 0)
            {
                FailFastConfig(macroRouteId, SceneRouteKind.Gameplay, signature, reason, $"Phase swap additive scenes missing for phaseRef='{phaseDefinitionRef.name}'.");
            }

            return swap;
        }

        public string BuildPhaseSwapSignature(PhaseDefinitionAsset phaseDefinitionRef, SceneRouteId routeId, string reason)
        {
            string phaseName = phaseDefinitionRef != null ? phaseDefinitionRef.name : "<null>";
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "GameplaySessionFlow/Unspecified" : reason.Trim();
            return $"phase:{phaseName}|route:{routeId}|reason:{normalizedReason}";
        }

        public string BuildPhaseSwapContentId(PhaseDefinitionAsset phaseDefinitionRef, string contentId = null)
        {
            if (!string.IsNullOrWhiteSpace(contentId))
            {
                return contentId.Trim();
            }

            return phaseDefinitionRef != null
                ? phaseDefinitionRef.BuildCanonicalSwapContentId()
                : "phase-swap:default";
        }

        private static void FailFastConfig(SceneRouteId routeId, SceneRouteKind routeKind, string signature, string reason, string configReason)
        {
            HardFailFastH1.Trigger(typeof(GameplayPhaseFlowService),
                $"[FATAL][H1][GameplaySessionFlow] Content contract error. routeId='{routeId}' routeKind='{routeKind}' signature='{signature}' reason='{reason}' detail='{configReason}'");
        }

    }
}
