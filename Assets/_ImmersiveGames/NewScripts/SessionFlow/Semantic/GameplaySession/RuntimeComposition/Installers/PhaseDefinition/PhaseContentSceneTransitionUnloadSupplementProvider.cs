using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.SceneComposition;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Runtime;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Installers.PhaseDefinition
{
    public sealed class PhaseContentSceneTransitionUnloadSupplementProvider : ISceneTransitionUnloadSupplementProvider
    {
        public PhaseContentSceneTransitionUnloadSupplementProvider()
        {
            SceneTransitionUnloadSupplementRegistry.Register(this);
        }

        public IReadOnlyList<string> GetSupplementalScenesToUnload(SceneTransitionContext context)
        {
            if (context.RouteKind == SceneRouteKind.Gameplay)
            {
                return Array.Empty<string>();
            }

            if (!PhaseContentSceneRuntimeApplier.HasActiveAppliedPhaseContent)
            {
                DebugUtility.Log(typeof(PhaseContentSceneTransitionUnloadSupplementProvider),
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentSupplementalUnloadSkipped routeId='{context.RouteId}' routeKind='{context.RouteKind}' reason='no_active_phase_content'.",
                    DebugUtility.Colors.Info);
                return Array.Empty<string>();
            }

            IReadOnlyList<string> activeScenes = PhaseContentSceneRuntimeApplier.ActiveAppliedSceneNames;
            if (activeScenes == null || activeScenes.Count == 0)
            {
                DebugUtility.Log(typeof(PhaseContentSceneTransitionUnloadSupplementProvider),
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentSupplementalUnloadSkipped routeId='{context.RouteId}' routeKind='{context.RouteKind}' reason='empty_active_scene_list'.",
                    DebugUtility.Colors.Info);
                return Array.Empty<string>();
            }

            DebugUtility.Log(typeof(PhaseContentSceneTransitionUnloadSupplementProvider),
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentSupplementalUnloadProvided routeId='{context.RouteId}' routeKind='{context.RouteKind}' activeScenes=[{string.Join(",", activeScenes)}].",
                DebugUtility.Colors.Info);

            return activeScenes;
        }
    }
}
