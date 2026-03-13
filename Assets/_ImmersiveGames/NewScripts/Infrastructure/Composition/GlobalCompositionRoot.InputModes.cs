using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.InputModes;
using _ImmersiveGames.NewScripts.Modules.InputModes.Interop;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static bool _inputModeDefaultsAppliedLogged;
        private static bool _inputModeRuntimeRailSkippedLogged;

        private static void RegisterInputModesFromRuntimeConfig()
        {
            if (!DependencyManager.HasInstance)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    "[InputMode] DependencyManager indisponivel. Registro do IInputModeService ignorado.");
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
                        "[InputMode] InputModes desabilitado via RuntimeModeConfig; IInputModeService nao sera registrado.",
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
                        "[InputMode] IInputModeService ja registrado no DI global.",
                        DebugUtility.Colors.Info);
                }

                return;
            }

            (string playerMapName, string menuMapName) = InputModesDefaults.ResolveFrom(config);

            if (logVerbose
                && !_inputModeDefaultsAppliedLogged
                && (settings == null
                    || string.IsNullOrWhiteSpace(settings.playerActionMapName)
                    || string.IsNullOrWhiteSpace(settings.menuActionMapName)))
            {
                _inputModeDefaultsAppliedLogged = true;
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][InputMode] ActionMapDefaultsApplied reason='blank_config' player='{playerMapName}' menu='{menuMapName}'.",
                    DebugUtility.Colors.Info);
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

        private static void RegisterInputModeCoordinator()
        {
            RegisterIfMissing(
                () => new InputModeCoordinator(),
                "[InputMode] InputModeCoordinator ja registrado no DI global.",
                "[InputMode] InputModeCoordinator registrado no DI global.");
        }

        private static void RegisterInputModeSceneFlowBridge()
        {
            // O trilho runtime de request/coordinator so pode existir quando o servico canonico ja estiver registrado.
            if (!ShouldRegisterInputModeRuntimeRail())
            {
                return;
            }

            RegisterInputModeCoordinator();
            RegisterIfMissing(
                () => new SceneFlowInputModeBridge(),
                "[InputMode] SceneFlowInputModeBridge ja registrado no DI global.",
                "[InputMode] SceneFlowInputModeBridge registrado no DI global.");
        }

        private static bool ShouldRegisterInputModeRuntimeRail()
        {
            if (!DependencyManager.HasInstance)
            {
                LogInputModeRuntimeRailSkippedOnce();
                return false;
            }

            var provider = DependencyManager.Provider;
            if (provider == null)
            {
                LogInputModeRuntimeRailSkippedOnce();
                return false;
            }

            if (!provider.TryGetGlobal<IInputModeService>(out var service) || service == null)
            {
                LogInputModeRuntimeRailSkippedOnce();
                return false;
            }

            return true;
        }

        private static void LogInputModeRuntimeRailSkippedOnce()
        {
            if (_inputModeRuntimeRailSkippedLogged)
            {
                return;
            }

            _inputModeRuntimeRailSkippedLogged = true;
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[OBS][InputMode] InputModeCoordinator/Bridge skipped reason='input_modes_disabled_or_not_registered'.",
                DebugUtility.Colors.Info);
        }
    }
}
