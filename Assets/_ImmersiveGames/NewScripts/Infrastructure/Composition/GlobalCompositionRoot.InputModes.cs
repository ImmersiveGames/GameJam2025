using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.InputModes;
using _ImmersiveGames.NewScripts.Modules.InputModes.Interop;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Adapters;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void RegisterInputModesFromRuntimeConfig()
        {
            if (!DependencyManager.HasInstance)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    "[InputMode] DependencyManager indisponível. Registro do IInputModeService ignorado.");
                ReportInputModesDegraded("missing_dependency_manager",
                    "DependencyManager not available during global composition.");
                return;
            }

            var provider = DependencyManager.Provider;

            provider.TryGetGlobal<RuntimeModeConfig>(out var config);
            var settings = config != null ? config.inputModes : null;

            bool enableInputModes = settings?.enableInputModes ?? true;
            bool logVerbose = settings?.logVerbose ?? true;

            if (!enableInputModes)
            {
                if (logVerbose)
                {
                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        "[InputMode] InputModes desabilitado via RuntimeModeConfig; IInputModeService não será registrado.",
                        DebugUtility.Colors.Info);
                }

                ReportInputModesDegraded("disabled_by_config",
                    "InputModes disabled by RuntimeModeConfig.");
                return;
            }

            if (provider.TryGetGlobal<IInputModeService>(out var existing) && existing != null)
            {
                if (logVerbose)
                {
                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        "[InputMode] IInputModeService já registrado no DI global.",
                        DebugUtility.Colors.Info);
                }

                return;
            }

            string playerMapName = settings?.playerActionMapName;
            string menuMapName = settings?.menuActionMapName;

            if (string.IsNullOrWhiteSpace(playerMapName))
            {
                playerMapName = "Player";
            }

            if (string.IsNullOrWhiteSpace(menuMapName))
            {
                menuMapName = "UI";
            }

            try
            {
                provider.RegisterGlobal<IInputModeService>(new InputModeService(playerMapName, menuMapName));

                if (logVerbose)
                {
                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        $"[InputMode] IInputModeService registrado no DI global (playerMap='{playerMapName}', menuMap='{menuMapName}').",
                        DebugUtility.Colors.Info);
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    $"[InputMode] Falha ao registrar IInputModeService. ex='{ex.GetType().Name}: {ex.Message}'.");
                ReportInputModesDegraded("register_failed", ex.Message);
            }
        }

        private static void ReportInputModesDegraded(string reason, string detail)
        {
            if (!DependencyManager.HasInstance)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IDegradedModeReporter>(out var reporter) || reporter == null)
            {
                return;
            }

            reporter.Report(DegradedKeys.Feature.InputModes, reason, detail);
        }

        private static void RegisterInputModeSceneFlowBridge()
        {
            RegisterIfMissing(
                () => new SceneFlowInputModeBridge(),
                "[InputMode] SceneFlowInputModeBridge ja registrado no DI global.",
                "[InputMode] SceneFlowInputModeBridge registrado no DI global.");
        }
    }
}
