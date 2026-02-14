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
            var bootstrap = GetRequiredBootstrapConfig(out _);
            var fadeSceneName = TryResolveFadeSceneName(bootstrap, out var degradeReason);

            if (string.IsNullOrWhiteSpace(fadeSceneName))
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    $"[ERROR][DEGRADED][Fade] {degradeReason}");

                RegisterIfMissing<IFadeService>(() => new DegradedFadeService(degradeReason));
                return;
            }

            RegisterIfMissing<IFadeService>(() => new FadeService(fadeSceneName));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[Fade] IFadeService registered in global DI (scene='{fadeSceneName}').",
                DebugUtility.Colors.Info);

            if (DependencyManager.Provider.TryGetGlobal<IFadeService>(out var fadeService) && fadeService != null)
            {
                _ = PreloadFadeSceneAsync(fadeService, fadeSceneName);
            }
            else
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[ERROR][DEGRADED][Fade] IFadeService could not be resolved after registration. Using degraded service.");
                RegisterIfMissing<IFadeService>(() => new DegradedFadeService("fade_service_resolve_failed"));
            }
        }

        private static string TryResolveFadeSceneName(NewScriptsBootstrapConfigAsset bootstrap, out string degradeReason)
        {
            var fadeSceneKey = bootstrap.FadeSceneKey;
            if (fadeSceneKey == null)
            {
                degradeReason = $"Missing fadeSceneKey. asset='{bootstrap.name}', field='fadeSceneKey'.";
                return string.Empty;
            }

            var fadeSceneName = (fadeSceneKey.SceneName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fadeSceneName))
            {
                degradeReason =
                    $"Invalid fadeSceneKey SceneName. asset='{bootstrap.name}', field='fadeSceneKey', keyAsset='{fadeSceneKey.name}'.";
                return string.Empty;
            }

            if (!Application.CanStreamedLevelBeLoaded(fadeSceneName))
            {
                degradeReason =
                    $"Fade scene is not available in Build Settings. asset='{bootstrap.name}', field='fadeSceneKey', keyAsset='{fadeSceneKey.name}', scene='{fadeSceneName}'.";
                return string.Empty;
            }

            degradeReason = string.Empty;
            return fadeSceneName;
        }

        private static async System.Threading.Tasks.Task PreloadFadeSceneAsync(IFadeService fadeService, string fadeSceneName)
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
                    $"[ERROR][DEGRADED][Fade] Failed to preload FadeScene '{fadeSceneName}'. ex='{ex.GetType().Name}: {ex.Message}'");

                RegisterIfMissing<IFadeService>(() => new DegradedFadeService("fade_preload_failed"));
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
