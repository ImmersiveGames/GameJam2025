using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.GameLoop;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Bootstrap estático para registrar e iniciar o serviço de GameLoop em NewScripts.
    /// </summary>
    public static class GameLoopBootstrap
    {
        private const string RuntimeDriverObjectName = "[NewScripts] GameLoopRuntimeDriver";

        private static bool _initialized;

        /// <summary>
        /// Garante o registro e inicialização do IGameLoopService no escopo global.
        /// Idempotente: múltiplas chamadas não recriam o serviço.
        /// </summary>
        public static void EnsureRegistered()
        {
            if (_initialized)
                return;

            EnsureServiceRegisteredAndInitialized();
            RegisterEventBridge();
            EnsureRuntimeDriver();

            _initialized = true;
        }

        private static void EnsureServiceRegisteredAndInitialized()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var existing) || existing == null)
            {
                var service = new GameLoopService();
                DependencyManager.Provider.RegisterGlobal<IGameLoopService>(service);

                service.Initialize();

                DebugUtility.LogVerbose(typeof(GameLoopBootstrap),
                    "IGameLoopService registrado e inicializado.");
            }
            else
            {
                // Garantia defensiva: mesmo se já existir, assegura que está inicializado.
                existing.Initialize();

                DebugUtility.LogVerbose(typeof(GameLoopBootstrap),
                    "IGameLoopService já estava registrado (Initialize() garantido).");
            }
        }

        private static void RegisterEventBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<GameLoopEventInputBridge>(out var bridge) && bridge != null)
            {
                DebugUtility.LogVerbose(typeof(GameLoopBootstrap),
                    "[GameLoop] Bridge de entrada já estava registrado.");
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

            DebugUtility.LogVerbose(typeof(GameLoopBootstrap),
                "[GameLoop] Bridge de entrada registrado após IGameLoopService.");
        }

        private static void EnsureRuntimeDriver()
        {
            // Se já existir na cena/hierarquia (incluindo DontDestroyOnLoad), não cria outro.
            var existing = Object.FindFirstObjectByType<GameLoopRuntimeDriver>();
            if (existing != null)
            {
                DebugUtility.LogVerbose(typeof(GameLoopBootstrap),
                    "[GameLoop] GameLoopRuntimeDriver já existe; não será recriado.");
                return;
            }

            var go = new GameObject(RuntimeDriverObjectName);
            go.AddComponent<GameLoopRuntimeDriver>();
            Object.DontDestroyOnLoad(go);

            DebugUtility.LogVerbose(typeof(GameLoopBootstrap),
                "[GameLoop] GameLoopRuntimeDriver criado (DontDestroyOnLoad) para tickar o GameLoop.");
        }
    }
}
