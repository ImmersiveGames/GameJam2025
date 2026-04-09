using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime
{
    // Active seam for manifest resolution; the surrounding LevelFlow folder remains historical.
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

        public PhaseDefinitionAsset ResolveNextPhaseOrFail(GameplayStartSnapshot snapshot, string reason)
        {
            if (!TryValidateSnapshot(snapshot, out string detail))
            {
                FailFastConfig(snapshot.MacroRouteId, SceneRouteKind.Gameplay, BuildContinuitySignature(snapshot, reason), reason, detail);
            }

            PhaseDefinitionAsset phaseDefinitionRef = snapshot.PhaseDefinitionRef;
            if (phaseDefinitionRef == null)
            {
                FailFastConfig(snapshot.MacroRouteId, snapshot.MacroRouteRef.RouteKind, BuildContinuitySignature(snapshot, reason), reason, "PhaseDefinitionRef is null.");
            }

            PhaseDefinitionAsset.PhaseContinuityBlock continuity = phaseDefinitionRef.Continuity;
            if (continuity == null)
            {
                FailFastConfig(snapshot.MacroRouteId, snapshot.MacroRouteRef.RouteKind, BuildContinuitySignature(snapshot, reason), reason, $"Phase continuity block is null for phaseRef='{phaseDefinitionRef.name}'.");
            }

            if (!continuity.hasNextPhase)
            {
                FailFastConfig(snapshot.MacroRouteId, snapshot.MacroRouteRef.RouteKind, BuildContinuitySignature(snapshot, reason), reason, $"Phase '{phaseDefinitionRef.name}' has no next phase configured.");
            }

            PhaseDefinitionAsset nextPhaseRef = continuity.nextPhaseRef;
            if (nextPhaseRef == null)
            {
                FailFastConfig(snapshot.MacroRouteId, snapshot.MacroRouteRef.RouteKind, BuildContinuitySignature(snapshot, reason), reason, $"Phase '{phaseDefinitionRef.name}' continuity nextPhaseRef is null.");
            }

            nextPhaseRef.ValidateOrFail($"NextPhase continuity phaseId='{phaseDefinitionRef.PhaseId}' routeId='{snapshot.MacroRouteId}' reason='{reason}'");

            return nextPhaseRef;
        }

        public string BuildPhaseSwapSignature(PhaseDefinitionAsset phaseDefinitionRef, SceneRouteId routeId, string reason)
        {
            string phaseName = phaseDefinitionRef != null ? phaseDefinitionRef.name : "<null>";
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "LevelFlow/Unspecified" : reason.Trim();
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

        private static bool TryValidateSnapshot(GameplayStartSnapshot snapshot, out string detail)
        {
            detail = string.Empty;

            if (!snapshot.IsValid)
            {
                detail = "Missing runtime gameplay snapshot.";
                return false;
            }

            if (!snapshot.HasPhaseDefinitionRef || snapshot.PhaseDefinitionRef == null)
            {
                detail = "Current gameplay snapshot has no phaseDefinitionRef.";
                return false;
            }

            if (!snapshot.MacroRouteId.IsValid)
            {
                detail = "Current gameplay snapshot has invalid macroRouteId.";
                return false;
            }

            if (snapshot.MacroRouteRef == null)
            {
                detail = "Current gameplay snapshot has null macroRouteRef.";
                return false;
            }

            if (snapshot.MacroRouteRef.RouteId != snapshot.MacroRouteId)
            {
                detail = $"RouteRef mismatch routeId='{snapshot.MacroRouteId}' routeRefRouteId='{snapshot.MacroRouteRef.RouteId}'.";
                return false;
            }

            if (snapshot.MacroRouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                detail = $"Snapshot routeKind invalid for LevelFlow. routeKind='{snapshot.MacroRouteRef.RouteKind}'.";
                return false;
            }

            return true;
        }

        private static string BuildContinuitySignature(GameplayStartSnapshot snapshot, string reason)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "LevelFlow/Unspecified" : reason.Trim();

            if (snapshot.HasPhaseDefinitionRef)
            {
                return $"phase:{snapshot.PhaseDefinitionRef.name}|route:{snapshot.MacroRouteId}|reason:{normalizedReason}";
            }

            return $"phase:<none>|route:{snapshot.MacroRouteId}|reason:{normalizedReason}";
        }

        private static void FailFastConfig(SceneRouteId routeId, SceneRouteKind routeKind, string signature, string reason, string configReason)
        {
            HardFailFastH1.Trigger(typeof(LevelFlowContentService),
                $"[FATAL][H1][LevelFlow] Content contract error. routeId='{routeId}' routeKind='{routeKind}' signature='{signature}' reason='{reason}' detail='{configReason}'");
        }

    }
}
