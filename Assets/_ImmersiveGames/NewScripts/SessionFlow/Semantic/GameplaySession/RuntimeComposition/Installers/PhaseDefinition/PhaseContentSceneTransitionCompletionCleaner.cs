using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Runtime;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Installers.PhaseDefinition
{
    public sealed class PhaseContentSceneTransitionCompletionCleaner : IDisposable
    {
        private readonly EventBinding<SceneTransitionCompletedEvent> _sceneTransitionCompletedBinding;
        private bool _disposed;

        public PhaseContentSceneTransitionCompletionCleaner()
        {
            _sceneTransitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);
            EventBus<SceneTransitionCompletedEvent>.Register(_sceneTransitionCompletedBinding);
        }

        private static bool ShouldClearOnSceneTransitionCompleted(SceneTransitionContext context)
        {
            return context.RouteKind != SceneRouteKind.Gameplay;
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            SceneTransitionContext context = evt.context;
            if (!ShouldClearOnSceneTransitionCompleted(context))
            {
                return;
            }

            if (!PhaseContentSceneRuntimeApplier.HasActiveAppliedPhaseContent)
            {
                DebugUtility.Log(typeof(PhaseContentSceneTransitionCompletionCleaner),
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentClearedOnSceneTransitionCompleted routeId='{context.RouteId}' routeKind='{context.RouteKind}' reason='no_active_phase_content'.",
                    DebugUtility.Colors.Info);
                return;
            }

            IReadOnlyList<string> activeScenes = PhaseContentSceneRuntimeApplier.ActiveAppliedSceneNames;
            if (activeScenes == null || activeScenes.Count == 0)
            {
                DebugUtility.Log(typeof(PhaseContentSceneTransitionCompletionCleaner),
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentClearedOnSceneTransitionCompleted routeId='{context.RouteId}' routeKind='{context.RouteKind}' reason='empty_active_scene_list'.",
                    DebugUtility.Colors.Info);
                return;
            }

            PhaseContentSceneRuntimeApplier.RecordCleared();

            DebugUtility.Log(typeof(PhaseContentSceneTransitionCompletionCleaner),
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentClearedOnSceneTransitionCompleted routeId='{context.RouteId}' routeKind='{context.RouteKind}' clearedScenes=[{string.Join(",", activeScenes)}].",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<SceneTransitionCompletedEvent>.Unregister(_sceneTransitionCompletedBinding);
        }
    }
}
