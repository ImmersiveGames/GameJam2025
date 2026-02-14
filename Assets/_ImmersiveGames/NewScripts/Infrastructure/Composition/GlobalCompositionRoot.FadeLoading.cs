using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Loading.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {

// --------------------------------------------------------------------
        // Fade / Loading
        // --------------------------------------------------------------------

        private static void RegisterSceneFlowFadeModule()
        {
            // Config canônica obrigatória para FadeScene.
            var bootstrap = GetRequiredBootstrapConfig(out _);
            var fadeSceneKey = bootstrap.FadeSceneKey;
            var fadeSceneName = ResolveRequiredFadeSceneName(bootstrap, fadeSceneKey);

            RegisterIfMissing<IFadeService>(() => new FadeService(fadeSceneName));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[Fade] IFadeService registered in global DI (scene='{fadeSceneName}').",
                DebugUtility.Colors.Info);

            if (DependencyManager.Provider.TryGetGlobal<IFadeService>(out var fadeService) && fadeService is FadeService concrete)
            {
                _ = PreloadFadeSceneAsync(concrete, fadeSceneName);
            }
            else
            {
                FailFast("IFadeService could not be resolved after registration.");
            }
        }

        private static string ResolveRequiredFadeSceneName(NewScriptsBootstrapConfigAsset bootstrap, SceneKeyAsset fadeSceneKey)
        {
            if (fadeSceneKey == null)
            {
                FailFast($"[FATAL][Fade] Missing required SceneKeyAsset. asset='{bootstrap.name}', field='fadeSceneKey'.");
            }

            var fadeSceneName = (fadeSceneKey.SceneName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fadeSceneName))
            {
                FailFast($"[FATAL][Fade] Invalid SceneKeyAsset. asset='{bootstrap.name}', field='fadeSceneKey', keyAsset='{fadeSceneKey.name}', reason='SceneName is empty'.");
            }

            if (!Application.CanStreamedLevelBeLoaded(fadeSceneName))
            {
                FailFast($"[FATAL][Fade] Scene is not available in Build Settings. asset='{bootstrap.name}', field='fadeSceneKey', keyAsset='{fadeSceneKey.name}', scene='{fadeSceneName}'.");
            }

            return fadeSceneName;
        }

        private static async System.Threading.Tasks.Task PreloadFadeSceneAsync(FadeService fadeService, string fadeSceneName)
        {
            try
            {
                await fadeService.EnsureReadyAsync();
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Fade] FadeScene ready (source=GlobalCompositionRoot/Preload, scene='{fadeSceneName}').",
                    DebugUtility.Colors.Success);
            }
            catch (System.Exception ex)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    $"[FATAL][Fade] Failed to preload FadeScene '{fadeSceneName}'. ex='{ex.GetType().Name}: {ex.Message}'");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                if (!Application.isEditor)
                {
                    Application.Quit();
                }

                return;
            }
        }

        private static void RegisterSceneFlowLoadingIfAvailable()
        {
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
