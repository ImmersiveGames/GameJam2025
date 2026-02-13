using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Loading.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {

// --------------------------------------------------------------------
        // Fade / Loading
        // --------------------------------------------------------------------

        private static void PreloadRequiredFadeScene()
        {
            if (AbortBootstrapIfFatalLatched("PreloadRequiredFadeScene.begin"))
            {
                return;
            }

            var bootstrapConfig = GetRequiredBootstrapConfig();
            var fadeSceneReference = bootstrapConfig.EssentialScenes.FadeScene;
            var requiredFadeScenePath = fadeSceneReference.GetPathOrNameForLoad();
            var requiredFadeSceneName = fadeSceneReference.SceneName;
            var fadeBuildIndex = SceneUtility.GetBuildIndexByScenePath(requiredFadeScenePath);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][Fade] Preloading FadeSceneName='{requiredFadeSceneName}', FadeScenePath='{requiredFadeScenePath}', BuildIndex={fadeBuildIndex} ...",
                DebugUtility.Colors.Info);

            if (fadeBuildIndex < 0)
            {
                throw RuntimeFailFastUtility.FailFastAndCreateException(
                    "Fade",
                    $"Required fade scene invalid. FadeSceneName='{requiredFadeSceneName}', FadeScenePath='{requiredFadeScenePath}', BuildIndex={fadeBuildIndex}.");
            }

            var fadeScene = SceneManager.GetSceneByName(requiredFadeSceneName);
            if (!fadeScene.IsValid() || !fadeScene.isLoaded)
            {
                SceneManager.LoadScene(fadeBuildIndex, LoadSceneMode.Additive);
                fadeScene = SceneManager.GetSceneByName(requiredFadeSceneName);
            }

            if (!fadeScene.IsValid() || !fadeScene.isLoaded)
            {
                throw RuntimeFailFastUtility.FailFastAndCreateException(
                    "Fade",
                    $"Failed to preload fade scene. FadeSceneName='{requiredFadeSceneName}', FadeScenePath='{requiredFadeScenePath}', BuildIndex={fadeBuildIndex}.");
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][Fade] FadeScene loaded OK. FadeSceneName='{requiredFadeSceneName}', FadeScenePath='{requiredFadeScenePath}', BuildIndex={fadeBuildIndex}.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterSceneFlowFadeModule()
        {
            if (AbortBootstrapIfFatalLatched("RegisterSceneFlowFadeModule.begin"))
            {
                return;
            }

            var bootstrapConfig = GetRequiredBootstrapConfig();
            var requiredFadeSceneName = bootstrapConfig.EssentialScenes.FadeScene.SceneName;

            // Registra o serviço de fade NewScripts no DI global.
            RegisterIfMissing<IFadeService>(() => new FadeService(requiredFadeSceneName));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[Fade] IFadeService registrado no DI global (requiredFadeScene='{requiredFadeSceneName}').",
                DebugUtility.Colors.Info);
        }

        private static void RegisterSceneFlowLoadingIfAvailable()
        {
            if (AbortBootstrapIfFatalLatched("RegisterSceneFlowLoadingIfAvailable.begin"))
            {
                return;
            }

            // ADR-0010: LoadingHudService depende da policy Strict/Release + reporter de degraded.
            // Mantemos best-effort: se por algum motivo os serviços não estiverem disponíveis,
            // ainda assim injetamos nulls e deixamos o próprio serviço decidir como degradar.
            DependencyManager.Provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeMode);
            DependencyManager.Provider.TryGetGlobal<IDegradedModeReporter>(out var degradedReporter);

            RegisterIfMissing<ILoadingHudService>(() => new LoadingHudService(runtimeMode, degradedReporter));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Loading] ILoadingHudService registrado no DI global.",
                DebugUtility.Colors.Info);

            if (DependencyManager.Provider.TryGetGlobal<LoadingHudOrchestrator>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Loading] LoadingHudOrchestrator já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            RegisterIfMissing(() => new LoadingHudOrchestrator());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Loading] LoadingHudOrchestrator registrado no DI global.",
                DebugUtility.Colors.Info);
        }

    }
}
