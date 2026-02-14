using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Bridges;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void RegisterGameLoopSceneFlowCoordinatorIfAvailable()
        {
            if (_sceneFlowCoordinator != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoopSceneFlow] Coordinator já está registrado (static reference).",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoopSceneFlow] ISceneTransitionService indisponível. Coordinator não será registrado.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bootstrap = GetRequiredBootstrapConfig(out _);
            SceneTransitionProfile profile = ResolveRequiredStartupProfile(bootstrap);

            var startPlan = new SceneTransitionRequest(
                scenesToLoad: new[] { SceneMenu, SceneUIGlobal },
                scenesToUnload: new[] { SceneNewBootstrap },
                targetActiveScene: SceneMenu,
                transitionProfile: profile,
                useFade: true,
                transitionProfileId: StartProfileId);

            _sceneFlowCoordinator = new GameLoopSceneFlowCoordinator(sceneFlow, startPlan);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[GameLoopSceneFlow] Coordinator registrado (startPlan production, profile='{StartProfileId}', profileAsset='{profile.name}').",
                DebugUtility.Colors.Info);
        }

        private static SceneTransitionProfile ResolveRequiredStartupProfile(Infrastructure.Config.NewScriptsBootstrapConfigAsset bootstrap)
        {
            var transitionCatalog = bootstrap.TransitionProfileCatalog;
            if (transitionCatalog == null)
            {
                FailFast("[FATAL][Config] Missing required NewScriptsBootstrapConfigAsset.transitionProfileCatalog for startup transition plan.");
            }

            if (!transitionCatalog.TryGetProfile(StartProfileId, out var profile) || profile == null)
            {
                FailFast($"[FATAL][Config] Startup profile missing in transitionProfileCatalog. profileId='{StartProfileId}'.");
            }

            return profile;
        }
    }
}
