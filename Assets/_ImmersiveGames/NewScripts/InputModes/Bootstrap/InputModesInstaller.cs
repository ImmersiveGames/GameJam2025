using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.InputModes.Contracts;
using _ImmersiveGames.NewScripts.InputModes.Runtime;
namespace _ImmersiveGames.NewScripts.InputModes.Bootstrap
{
    public static class InputModesInstaller
    {
        private static bool _installed;

        public static void Install(RuntimeModeConfig runtimeModeConfig)
        {
            if (_installed)
            {
                return;
            }

            _ = runtimeModeConfig;

            RuntimeModeConfig runtimeConfig = ResolveRuntimeModeConfigOrFail();
            RuntimeModeConfig.InputModesSettings settings = runtimeConfig.inputModes;

            if (settings != null && !settings.enableInputModes)
            {
                throw new InvalidOperationException(
                    "[FATAL][Config][InputModes] InputModes disabled by RuntimeModeConfig. Canonical InputModes rail is mandatory in Base 1.1.");
            }

            (string playerMapName, string menuMapName) = InputModesDefaults.ResolveRequiredFrom(runtimeConfig);
            ValidateRequiredActionMapNamesOrFail(playerMapName, menuMapName);

            EnsureCanonicalInputModeService(playerMapName, menuMapName);

            _installed = true;

            DebugUtility.Log(typeof(InputModesInstaller),
                "[InputModes] Module installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static RuntimeModeConfig ResolveRuntimeModeConfigOrFail()
        {
            if (DependencyManager.Provider == null)
            {
                throw new InvalidOperationException("[FATAL][Config][InputModes] DependencyManager.Provider indisponivel no installer.");
            }

            if (DependencyManager.Provider.TryGetGlobal<RuntimeModeConfig>(out var runtimeConfig) && runtimeConfig != null)
            {
                return runtimeConfig;
            }

            throw new InvalidOperationException(
                "[FATAL][Config][InputModes] RuntimeModeConfig obrigatorio ausente no DI global antes de instalar InputModes.");
        }

        private static void EnsureCanonicalInputModeService(string playerMapName, string menuMapName)
        {
            if (DependencyManager.Provider.TryGetGlobal<IInputModeService>(out var existingService) && existingService != null)
            {
                if (existingService is not InputModeService)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][InputModes] IInputModeService existente incompatvel. expected='{nameof(InputModeService)}' actual='{existingService.GetType().Name}'.");
                }

                if (!DependencyManager.Provider.TryGetGlobal<IInputModeStateService>(out var existingState) || existingState == null)
                {
                    DependencyManager.Provider.RegisterGlobal<IInputModeStateService>((InputModeService)existingService);
                }

                DebugUtility.LogVerbose(typeof(InputModesInstaller),
                    "[OBS][InputModes][Installer] Canonical IInputModeService already present.",
                    DebugUtility.Colors.Info);
                return;
            }

            var inputModeService = new InputModeService(playerMapName, menuMapName);
            DependencyManager.Provider.RegisterGlobal<IInputModeService>(inputModeService);
            DependencyManager.Provider.RegisterGlobal<IInputModeStateService>(inputModeService);
            DependencyManager.Provider.RegisterGlobal(inputModeService);

            DebugUtility.LogVerbose(typeof(InputModesInstaller),
                $"[OBS][InputModes][Installer] Canonical IInputModeService registered playerMap='{playerMapName}' menuMap='{menuMapName}'.",
                DebugUtility.Colors.Info);
        }

        private static void ValidateRequiredActionMapNamesOrFail(string playerMapName, string menuMapName)
        {
            if (string.IsNullOrWhiteSpace(playerMapName))
            {
                throw new InvalidOperationException("[FATAL][Config][InputModes] playerActionMapName obrigatorio ausente no RuntimeModeConfig.");
            }

            if (string.IsNullOrWhiteSpace(menuMapName))
            {
                throw new InvalidOperationException("[FATAL][Config][InputModes] menuActionMapName obrigatorio ausente no RuntimeModeConfig.");
            }
        }
    }
}
