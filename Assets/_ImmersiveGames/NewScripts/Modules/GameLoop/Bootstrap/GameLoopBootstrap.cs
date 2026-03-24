using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Input;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Run;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Bootstrap
{
    /// <summary>
    /// Responsável por garantir que o GameLoop esteja registrado e pronto no DI global:
    /// - IGameLoopService (GameLoopService)
    /// - GameLoopInputCommandBridge (entrada de eventos definitivos)
    /// - GameLoopInputDriver (ticker do serviço)
    ///
    /// Importante:
    /// - O bootstrap NÃO tick o serviço. Quem tick é o GameLoopInputDriver.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class GameLoopBootstrap : MonoBehaviour
    {
        private const string DriverObjectName = "[NewScripts] GameLoopInputDriver";
        private const string RunEndBridgeObjectName = "[NewScripts] GameRunEndedEventBridge";

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
            if (DependencyManager.Provider.TryGetGlobal<GameLoopInputCommandBridge>(out _))
            {
                return;
            }

            var bridge = new GameLoopInputCommandBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);
        }

        private static void EnsureDriver()
        {
            // Compatível com versões mais antigas do Unity (evita FindFirstObjectByType).
            if (FindFirstObjectByType<GameLoopInputDriver>() != null)
            {
                return;
            }

            var go = new GameObject(DriverObjectName);
            go.AddComponent<GameLoopInputDriver>();
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

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunPlayingStateGuard>(out var gameplayStateGuard) || gameplayStateGuard == null)
            {
                gameplayStateGuard = new GameRunPlayingStateGuard(gameLoopService);
                DependencyManager.Provider.RegisterGlobal(gameplayStateGuard);
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunResultSnapshotService>(out var status) || status == null)
            {
                status = new GameRunResultSnapshotService();
                DependencyManager.Provider.RegisterGlobal(status);
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var outcome) || outcome == null)
            {
                outcome = new GameRunOutcomeService(gameplayStateGuard);
                DependencyManager.Provider.RegisterGlobal(outcome);
            }
        }

        private static void EnsureOutcomeEventInputBridge()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var outcomeService) || outcomeService == null)
            {
                Debug.LogWarning("[NewScripts] GameLoopBootstrap: IGameRunOutcomeService não está disponível; pulando GameRunOutcomeRequestBridge.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<GameRunOutcomeRequestBridge>(out var bridge) || bridge == null)
            {
                bridge = new GameRunOutcomeRequestBridge(outcomeService);
                DependencyManager.Provider.RegisterGlobal(bridge);
            }
        }

        private static void EnsureRunEndEventBridge()
        {
            // Compatível com versões mais antigas do Unity (evita FindFirstObjectByType).
            if (FindFirstObjectByType<GameRunEndedEventBridge>() != null)
            {
                return;
            }

            var go = new GameObject(RunEndBridgeObjectName);
            go.AddComponent<GameRunEndedEventBridge>();
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

