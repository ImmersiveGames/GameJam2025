using System;
using _ImmersiveGames.NewScripts.ActorSystem.Semantic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.InputModes.Contracts;
using _ImmersiveGames.NewScripts.InputModes.Runtime;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static bool _inputModeDefaultsAppliedLogged;

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
                if (existing is IInputModeStateService existingState
                    && (!provider.TryGetGlobal<IInputModeStateService>(out var registeredState) || registeredState == null))
                {
                    provider.RegisterGlobal(existingState);
                }

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
                provider.TryGetGlobal<IActorSystemReadModelService>(out var actorSystemReadModelService);
                var playerInputLocator = new PlayerInputLocator(actorSystemReadModelService);
                var inputModeService = new InputModeService(playerInputLocator, playerMapName, menuMapName);
                provider.RegisterGlobal<IPlayerInputLocator>(playerInputLocator);
                provider.RegisterGlobal<IInputModeService>(inputModeService);
                provider.RegisterGlobal<IInputModeStateService>(inputModeService);

                if (logVerbose)
                {
                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        $"[InputMode] IInputModeService registrado no DI global (playerMap='{playerMapName}', menuMap='{menuMapName}', actorSystem='{(actorSystemReadModelService != null ? "connected" : "not_connected")}').",
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
    }
}

