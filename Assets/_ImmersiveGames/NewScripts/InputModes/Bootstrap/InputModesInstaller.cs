using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.InputModes.Contracts;
using _ImmersiveGames.NewScripts.InputModes.Runtime;
namespace _ImmersiveGames.NewScripts.InputModes.Bootstrap
{
    public static class InputModesInstaller
    {
        private static bool _installed;
        private static bool _defaultsAppliedLogged;

        public static void Install(BootstrapConfigAsset bootstrapConfig)
        {
            if (_installed)
            {
                return;
            }

            _ = bootstrapConfig;

            RuntimeModeConfig runtimeConfig = ResolveRuntimeModeConfigOrFail();
            RuntimeModeConfig.InputModesSettings settings = runtimeConfig.inputModes;

            if (settings != null && !settings.enableInputModes)
            {
                throw new InvalidOperationException(
                    "[FATAL][Config][InputModes] InputModes disabled by RuntimeModeConfig. Canonical InputModes rail is mandatory in Base 1.0.");
            }

            bool logVerbose = settings?.logVerbose ?? true;
            (string playerMapName, string menuMapName) = InputModesDefaults.ResolveFrom(runtimeConfig);

            if (logVerbose
                && !_defaultsAppliedLogged
                && (settings == null
                    || string.IsNullOrWhiteSpace(settings.playerActionMapName)
                    || string.IsNullOrWhiteSpace(settings.menuActionMapName)))
            {
                _defaultsAppliedLogged = true;
                DebugUtility.LogVerbose(typeof(InputModesInstaller),
                    $"[OBS][InputModes][Installer] ActionMapDefaultsApplied reason='blank_config' player='{playerMapName}' menu='{menuMapName}'.",
                    DebugUtility.Colors.Info);
            }

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

                if (!DependencyManager.Provider.TryGetGlobal<IPlayerInputLocator>(out var existingLocator) || existingLocator == null)
                {
                    DependencyManager.Provider.RegisterGlobal<IPlayerInputLocator>(new PlayerInputLocator());
                }

                DebugUtility.LogVerbose(typeof(InputModesInstaller),
                    "[OBS][InputModes][Installer] Canonical IInputModeService already present.",
                    DebugUtility.Colors.Info);
                return;
            }

            var playerInputLocator = new PlayerInputLocator();
            var inputModeService = new InputModeService(playerInputLocator, playerMapName, menuMapName);

            DependencyManager.Provider.RegisterGlobal<IPlayerInputLocator>(playerInputLocator);
            DependencyManager.Provider.RegisterGlobal<IInputModeService>(inputModeService);
            DependencyManager.Provider.RegisterGlobal<IInputModeStateService>(inputModeService);
            DependencyManager.Provider.RegisterGlobal(inputModeService);

            DebugUtility.LogVerbose(typeof(InputModesInstaller),
                $"[OBS][InputModes][Installer] Canonical IInputModeService registered playerMap='{playerMapName}' menuMap='{menuMapName}'.",
                DebugUtility.Colors.Info);
        }
    }
}
