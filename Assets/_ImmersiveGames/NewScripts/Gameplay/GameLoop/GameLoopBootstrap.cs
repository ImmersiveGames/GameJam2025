using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

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

            _initialized = true;
        }
    }
}
