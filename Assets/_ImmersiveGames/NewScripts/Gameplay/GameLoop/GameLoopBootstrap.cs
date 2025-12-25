using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.GameLoop;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Responsável por garantir que o GameLoop esteja registrado e pronto no DI global:
    /// - IGameLoopService (GameLoopService)
    /// - GameLoopEventInputBridge (entrada de eventos definitivos)
    /// - GameLoopRuntimeDriver (ticker do serviço)
    ///
    /// Importante:
    /// - O bootstrap NÃO tick o serviço. Quem tick é o GameLoopRuntimeDriver.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class GameLoopBootstrap : MonoBehaviour
    {
        private const string DriverObjectName = "[NewScripts] GameLoopRuntimeDriver";

        private static bool _initialized;

        public static void Ensure()
        {
            if (_initialized)
            {
                return;
            }

            EnsureService();
            EnsureBridge();
            EnsureDriver();

            _initialized = true;
        }

        private static void EnsureService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var svc) || svc == null)
            {
                svc = new GameLoopService();
                DependencyManager.Provider.RegisterGlobal(svc);
            }

            // Initialize deve ser idempotente.
            svc.Initialize();
        }

        private static void EnsureBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<GameLoopEventInputBridge>(out _))
            {
                return;
            }

            var bridge = new GameLoopEventInputBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);
        }

        private static void EnsureDriver()
        {
            if (FindFirstObjectByType<GameLoopRuntimeDriver>() != null)
            {
                return;
            }

            var go = new GameObject(DriverObjectName);
            go.AddComponent<GameLoopRuntimeDriver>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            // Permite usar o bootstrap em uma cena (por prefab), mas sem criar duplicatas.
            Ensure();
            DontDestroyOnLoad(gameObject);
        }
    }
}
