using _ImmersiveGames.NewScripts.Infrastructure.Debug;
using _ImmersiveGames.NewScripts.Infrastructure.DI;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Bootstrap estático para registrar e iniciar o serviço de GameLoop em NewScripts.
    /// </summary>
    public static class GameLoopBootstrap
    {
        private static bool _initialized;

        /// <summary>
        /// Garante o registro e inicialização do IGameLoopService no escopo global.
        /// Idempotente: múltiplas chamadas não recriam o serviço.
        /// </summary>
        public static void EnsureRegistered()
        {
            if (_initialized)
                return;

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var existing))
            {
                var service = new GameLoopService();
                DependencyManager.Provider.RegisterGlobal<IGameLoopService>(service);
                service.Initialize();
                DebugUtility.LogVerbose(typeof(GameLoopBootstrap),"IGameLoopService registrado e inicializado.");
            }
            else
            {
                DebugUtility.LogVerbose(typeof(GameLoopBootstrap),"IGameLoopService já estava registrado.");
            }

            RegisterEventBridge();

            _initialized = true;
        }

        private static void RegisterEventBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<GameLoopEventInputBridge>(out var bridge) && bridge != null)
            {
                DebugUtility.LogVerbose(typeof(GameLoopBootstrap), "[GameLoop] Bridge de entrada já estava registrado.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var service) || service == null)
            {
                DebugUtility.LogWarning(typeof(GameLoopBootstrap),
                    "[GameLoop] IGameLoopService indisponível; Bridge de entrada não será registrado.");
                return;
            }

            bridge = new GameLoopEventInputBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);
            DebugUtility.LogVerbose(typeof(GameLoopBootstrap), "[GameLoop] Bridge de entrada registrado após IGameLoopService.");
        }
    }
}
