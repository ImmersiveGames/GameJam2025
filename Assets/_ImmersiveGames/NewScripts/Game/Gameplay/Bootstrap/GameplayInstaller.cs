using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Camera;
using _ImmersiveGames.NewScripts.Modules.Gameplay.State;

namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Bootstrap
{
    /// <summary>
    /// Installer do Gameplay.
    ///
    /// Responsabilidade:
    /// - registrar contratos de estado/camera do gameplay no boot;
    /// - nao compor runtime nem ativar bridges.
    /// </summary>
    public static class GameplayInstaller
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            RegisterGameplayStateGate();
            RegisterGameplayCameraResolver();

            _installed = true;

            DebugUtility.Log(typeof(GameplayInstaller),
                "[Gameplay] Module installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameplayStateGate()
        {
            RegisterIfMissing<IGameplayStateGate>(
                () => new GameplayStateGate(),
                "[Gameplay] IGameplayStateGate ja registrado no DI global.",
                "[Gameplay] GameplayStateGate registrado no DI global.");
        }

        private static void RegisterGameplayCameraResolver()
        {
            RegisterIfMissing<IGameplayCameraResolver>(
                () => new GameplayCameraResolver(),
                "[Gameplay] IGameplayCameraResolver ja registrado no DI global.",
                "[Gameplay] GameplayCameraResolver registrado no DI global.");
        }

        private static void RegisterIfMissing<T>(Func<T> factory, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GameplayInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(GameplayInstaller), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}
