using _ImmersiveGames.NewScripts.Core.DI;
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
        private const string RunEndBridgeObjectName = "[NewScripts] GameLoopRunEndEventBridge";

        private static bool _initialized;

        public static void Ensure()
        {
            if (_initialized)
            {
                return;
            }

            EnsureService();
            EnsureBridge();
            EnsureGameRunServices();
            EnsureOutcomeEventInputBridge();
            EnsureRunEndEventBridge();
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
            // Compatível com versões mais antigas do Unity (evita FindFirstObjectByType).
            if (FindFirstObjectByType<GameLoopRuntimeDriver>() != null)
            {
                return;
            }

            var go = new GameObject(DriverObjectName);
            go.AddComponent<GameLoopRuntimeDriver>();
            DontDestroyOnLoad(go);
        }

        private static void EnsureGameRunServices()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
            {
                Debug.LogWarning("[NewScripts] GameLoopBootstrap: IGameLoopService não está disponível; pulando serviços de run.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var endRequest) || endRequest == null)
            {
                endRequest = new GameRunEndRequestService();
                DependencyManager.Provider.RegisterGlobal(endRequest);
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunStatusService>(out var status) || status == null)
            {
                status = new GameRunStatusService(gameLoopService);
                DependencyManager.Provider.RegisterGlobal(status);
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var outcome) || outcome == null)
            {
                outcome = new GameRunOutcomeService(gameLoopService);
                DependencyManager.Provider.RegisterGlobal(outcome);
            }
        }

        private static void EnsureOutcomeEventInputBridge()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var outcomeService) || outcomeService == null)
            {
                Debug.LogWarning("[NewScripts] GameLoopBootstrap: IGameRunOutcomeService não está disponível; pulando GameRunOutcomeEventInputBridge.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<GameRunOutcomeEventInputBridge>(out var bridge) || bridge == null)
            {
                bridge = new GameRunOutcomeEventInputBridge(outcomeService);
                DependencyManager.Provider.RegisterGlobal(bridge);
            }
        }

        private static void EnsureRunEndEventBridge()
        {
            // Compatível com versões mais antigas do Unity (evita FindFirstObjectByType).
            if (FindFirstObjectByType<GameLoopRunEndEventBridge>() != null)
            {
                return;
            }

            var go = new GameObject(RunEndBridgeObjectName);
            go.AddComponent<GameLoopRunEndEventBridge>();
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
