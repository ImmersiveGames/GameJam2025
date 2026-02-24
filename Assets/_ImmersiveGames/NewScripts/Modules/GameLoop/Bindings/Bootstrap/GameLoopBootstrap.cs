using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Bindings.Bridges;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Bindings.Drivers;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Bridges;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Services;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Bindings.Bootstrap
{
    /// <summary>
    /// Responsável por garantir que o GameLoop esteja registrado e pronto no DI global:
    /// - IGameLoopService (GameLoopService)
    /// - GameLoopCommandEventBridge (entrada de eventos definitivos)
    /// - GameLoopDriver (ticker do serviço)
    ///
    /// Importante:
    /// - O bootstrap NÃO tick o serviço. Quem tick é o GameLoopDriver.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class GameLoopBootstrap : MonoBehaviour
    {
        private const string DriverObjectName = "[NewScripts] GameLoopDriver";
        private const string RunEndBridgeObjectName = "[NewScripts] GameLoopRunEndEventBridge";

        private static bool _initialized;

        public static void Ensure(bool includeGameRunServices = true, bool includeOutcomeEventInputBridge = true)
        {
            if (_initialized)
            {
                return;
            }

            EnsureService();
            EnsureBridge();
            if (includeGameRunServices)
            {
                EnsureGameRunServices();
            }

            if (includeOutcomeEventInputBridge)
            {
                EnsureOutcomeEventInputBridge();
            }
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
            if (DependencyManager.Provider.TryGetGlobal<GameLoopCommandEventBridge>(out _))
            {
                return;
            }

            var bridge = new GameLoopCommandEventBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);
        }

        private static void EnsureDriver()
        {
            // Compatível com versões mais antigas do Unity (evita FindFirstObjectByType).
            if (FindFirstObjectByType<GameLoopDriver>() != null)
            {
                return;
            }

            var go = new GameObject(DriverObjectName);
            go.AddComponent<GameLoopDriver>();
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

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunStateService>(out var status) || status == null)
            {
                status = new GameRunStateService(gameLoopService);
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
                Debug.LogWarning("[NewScripts] GameLoopBootstrap: IGameRunOutcomeService não está disponível; pulando GameRunOutcomeCommandBridge.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<GameRunOutcomeCommandBridge>(out var bridge) || bridge == null)
            {
                bridge = new GameRunOutcomeCommandBridge(outcomeService);
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

