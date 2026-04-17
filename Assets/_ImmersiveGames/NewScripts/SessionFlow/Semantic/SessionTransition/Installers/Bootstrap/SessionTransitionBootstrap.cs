using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Installers.Bootstrap
{
    public static class SessionTransitionBootstrap
    {
        private static bool _runtimeComposed;

        public static void ComposeRuntime()
        {
            if (_runtimeComposed)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplaySessionFlowContinuityService>(out var continuityService) || continuityService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionTransition] IGameplaySessionFlowContinuityService missing from global DI before session transition composition.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<SessionTransitionPlanResolver>(out var existingResolver) || existingResolver == null)
            {
                DependencyManager.Provider.RegisterGlobal(new SessionTransitionPlanResolver());
                DebugUtility.LogVerbose(typeof(SessionTransitionBootstrap),
                    "[OBS][GameplaySessionFlow][SessionTransition] SessionTransitionPlanResolver registered in global DI.",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<SessionTransitionOrchestrator>(out var existingOrchestrator) || existingOrchestrator == null)
            {
                DependencyManager.Provider.RegisterGlobal(new SessionTransitionOrchestrator(continuityService));
                DebugUtility.LogVerbose(typeof(SessionTransitionBootstrap),
                    "[OBS][GameplaySessionFlow][SessionTransition] SessionTransitionOrchestrator registered in global DI.",
                    DebugUtility.Colors.Info);
            }

            _runtimeComposed = true;
        }
    }
}

