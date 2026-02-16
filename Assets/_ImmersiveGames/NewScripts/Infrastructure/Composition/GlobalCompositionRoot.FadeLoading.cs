using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Loading.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
            var fadeSceneName = TryResolveFadeSceneName(bootstrap, out var failureReason);

            if (string.IsNullOrWhiteSpace(fadeSceneName))
            {
                HandleFadeBootstrapFailure(failureReason);
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
                HandleFadeBootstrapFailure("IFadeService could not be resolved after registration.");
            }
        }

        private static string TryResolveFadeSceneName(NewScriptsBootstrapConfigAsset bootstrap, out string failureReason)
        {
            var fadeSceneKey = bootstrap.FadeSceneKey;
            if (fadeSceneKey == null)
            {
                failureReason = $"Missing fadeSceneKey. asset='{bootstrap.name}', field='fadeSceneKey'.";
                return string.Empty;
            }

            var fadeSceneName = (fadeSceneKey.SceneName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fadeSceneName))
            {
                failureReason =
                    $"Invalid fadeSceneKey SceneName. asset='{bootstrap.name}', field='fadeSceneKey', keyAsset='{fadeSceneKey.name}'.";
                return string.Empty;
            }

            if (!TryValidateFadeSceneInBuildSettings(fadeSceneName, bootstrap.name, fadeSceneKey.name, out failureReason))
            {
                return string.Empty;
            }

            failureReason = string.Empty;
            return fadeSceneName;
        }

        private static bool TryValidateFadeSceneInBuildSettings(
            string fadeSceneName,
            string bootstrapAssetName,
            string fadeSceneKeyAssetName,
            out string failureReason)
        {
            failureReason = string.Empty;
            string buildScenePath = TryResolveBuildSettingsScenePath(fadeSceneName);
            if (!string.IsNullOrWhiteSpace(buildScenePath))
            {
                int buildIndex = SceneUtility.GetBuildIndexByScenePath(buildScenePath);
                if (buildIndex < 0)
                {
                    failureReason =
                        $"FadeScene inválida no Build Settings: path='{buildScenePath}' retornou buildIndex={buildIndex}. " +
                        $"Corrija em File > Build Settings e garanta a cena habilitada. asset='{bootstrapAssetName}', field='fadeSceneKey', keyAsset='{fadeSceneKeyAssetName}', scene='{fadeSceneName}'.";
                    return false;
                }

                return true;
            }

            if (!Application.CanStreamedLevelBeLoaded(fadeSceneName))
            {
                failureReason =
                    $"FadeScene ausente no Build Settings. Adicione/ative a cena em File > Build Settings (scene/path) para fade funcionar. " +
                    $"asset='{bootstrapAssetName}', field='fadeSceneKey', keyAsset='{fadeSceneKeyAssetName}', scene='{fadeSceneName}'.";
                return false;
            }

            return true;
        }

        private static string TryResolveBuildSettingsScenePath(string fadeSceneName)
        {
#if UNITY_EDITOR
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            if (buildScenes == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < buildScenes.Length; i++)
            {
                EditorBuildSettingsScene scene = buildScenes[i];
                if (scene == null || !scene.enabled || string.IsNullOrWhiteSpace(scene.path))
                {
                    continue;
                }

                string scenePath = scene.path.Trim();
                string sceneFileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (string.Equals(sceneFileName, fadeSceneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return scenePath;
                }
            }
#endif
            return string.Empty;
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
                HandleFadeRuntimeFailure(
                    $"Failed to preload FadeScene '{fadeSceneName}'. ex='{ex.GetType().Name}: {ex.Message}'",
                    "fade_preload_failed");
            }
        }

        private static void HandleFadeBootstrapFailure(string reason)
        {
            if (ShouldDegradeFadeInRuntime())
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    $"[ERROR][DEGRADED][Fade] {reason}");

                DependencyManager.Provider.RegisterGlobal<IFadeService>(
                    new DegradedFadeService(reason),
                    allowOverride: true);
                return;
            }

            HandleFadeRuntimeFailure(reason, "fade_bootstrap_invalid", isConfigFatal: true);
        }

        private static void HandleFadeRuntimeFailure(string reason, string degradedReason, bool isConfigFatal = false)
        {
            if (ShouldDegradeFadeInRuntime())
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    $"[ERROR][DEGRADED][Fade] {reason}");

                DependencyManager.Provider.RegisterGlobal<IFadeService>(
                    new DegradedFadeService(degradedReason),
                    allowOverride: true);
                return;
            }

            var tag = isConfigFatal ? "[FATAL][Config][Fade]" : "[FATAL][Fade]";
            DebugUtility.LogError(typeof(GlobalCompositionRoot), $"{tag} {reason}");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            if (!Application.isEditor)
            {
                Application.Quit();
            }

            throw new System.InvalidOperationException($"{tag} {reason}");
        }

        private static bool ShouldDegradeFadeInRuntime()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
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
