using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Implementação de produção: executa rotas via ISceneTransitionService.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameNavigationService : IGameNavigationService
    {
        private readonly ISceneTransitionService _sceneFlow;
        private readonly IGameNavigationCatalog _catalog;
        private readonly ISceneRouteCatalog _sceneRouteCatalog;
        private readonly ITransitionStyleCatalog _styleCatalog;
        private readonly ILevelFlowService _levelFlowService;

        private int _navigationInProgress;

        public GameNavigationService(ISceneTransitionService sceneFlow)
            : this(sceneFlow, GameNavigationCatalog.CreateDefaultMinimal())
        {
        }

        public GameNavigationService(ISceneTransitionService sceneFlow, GameNavigationCatalog catalog)
            : this(sceneFlow, (IGameNavigationCatalog)catalog)
        {
        }

        public GameNavigationService(ISceneTransitionService sceneFlow, GameNavigationCatalogAsset catalogAsset)
            : this(sceneFlow, (IGameNavigationCatalog)catalogAsset)
        {
        }

        public GameNavigationService(ISceneTransitionService sceneFlow, IGameNavigationCatalog catalog)
            : this(sceneFlow, catalog, null, null, null)
        {
        }

        public GameNavigationService(
            ISceneTransitionService sceneFlow,
            IGameNavigationCatalog catalog,
            ISceneRouteCatalog sceneRouteCatalog,
            ITransitionStyleCatalog styleCatalog,
            ILevelFlowService levelFlowService)
        {
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _sceneRouteCatalog = sceneRouteCatalog;
            _styleCatalog = styleCatalog;
            _levelFlowService = levelFlowService;

            DebugUtility.LogVerbose(typeof(GameNavigationService),
                $"[Navigation] GameNavigationService inicializado. Rotas: {string.Join(", ", _catalog.RouteIds)}",
                DebugUtility.Colors.Info);
        }

        public Task RequestMenuAsync(string reason = null)
            => NavigateAsync(GameNavigationCatalog.Routes.ToMenu, reason);

        public Task RequestGameplayAsync(string reason = null)
            => NavigateAsync(GameNavigationCatalog.Routes.ToGameplay, reason);

        public async Task StartGameplayAsync(LevelId levelId, string reason = null)
        {
            if (!levelId.IsValid)
            {
                DebugUtility.LogWarning(typeof(GameNavigationService),
                    $"[Navigation] StartGameplayAsync chamado com LevelId inválido. levelId='{levelId}'.");
                return;
            }

            if (Interlocked.CompareExchange(ref _navigationInProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning(typeof(GameNavigationService),
                    $"[Navigation] Navegação já em progresso. Ignorando LevelId='{levelId}'.");
                return;
            }

            try
            {
                if (!_catalog.TryGet(GameNavigationCatalog.Routes.ToGameplay, out var entry) || !entry.IsValid)
                {
                    DebugUtility.LogError(typeof(GameNavigationService),
                        $"[Navigation] Intent padrão de gameplay não encontrado. levelId='{levelId}'.");
                    return;
                }

                if (_levelFlowService == null)
                {
                    DebugUtility.LogWarning(typeof(GameNavigationService),
                        $"[Navigation] LevelFlow indisponível. Fallback para rota padrão. levelId='{levelId}'.");
                    await ExecuteEntryAsync(GameNavigationCatalog.Routes.ToGameplay, entry, reason);
                    return;
                }

                if (!_levelFlowService.TryResolve(levelId, out var resolvedRouteId, out var payload))
                {
                    DebugUtility.LogWarning(typeof(GameNavigationService),
                        $"[Navigation] LevelId não resolvido. Fallback para rota padrão. levelId='{levelId}'.");
                    await ExecuteEntryAsync(GameNavigationCatalog.Routes.ToGameplay, entry, reason);
                    return;
                }

                var levelEntry = new GameNavigationEntry(resolvedRouteId, entry.StyleId, payload);
                await ExecuteEntryAsync(GameNavigationCatalog.Routes.ToGameplay, levelEntry, reason);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GameNavigationService),
                    $"[Navigation] Exceção ao iniciar gameplay. levelId='{levelId}', reason='{reason ?? "<null>"}', ex={ex}");
            }
            finally
            {
                Interlocked.Exchange(ref _navigationInProgress, 0);
            }
        }

        public async Task NavigateAsync(string routeId, string reason = null)
        {
            if (string.IsNullOrWhiteSpace(routeId))
            {
                DebugUtility.LogError(typeof(GameNavigationService),
                    "[Navigation] NavigateAsync chamado com routeId vazio. Abortando.");
                return;
            }

            if (Interlocked.CompareExchange(ref _navigationInProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning(typeof(GameNavigationService),
                    $"[Navigation] Navegação já em progresso. Ignorando rota='{routeId}'.");
                return;
            }

            try
            {
                if (!_catalog.TryGet(routeId, out var entry) || !entry.IsValid)
                {
                    DebugUtility.LogError(typeof(GameNavigationService),
                        $"[Navigation] Rota desconhecida ou sem request. routeId='{routeId}'.");
                    return;
                }

                await ExecuteEntryAsync(routeId, entry, reason);
            }
            catch (Exception ex)
            {
                // Comentário: navegação é infraestrutura de fluxo; não deve derrubar o jogo.
                DebugUtility.LogError(typeof(GameNavigationService),
                    $"[Navigation] Exceção ao navegar. routeId='{routeId}', reason='{reason ?? "<null>"}', ex={ex}");
            }
            finally
            {
                Interlocked.Exchange(ref _navigationInProgress, 0);
            }
        }

        private async Task ExecuteEntryAsync(string intentId, GameNavigationEntry entry, string reason)
        {
            var payload = ResolvePayload(entry);
            var (profileId, useFade) = ResolveStyle(entry, payload);

            var request = new SceneTransitionRequest(
                entry.RouteId,
                entry.StyleId,
                payload,
                transitionProfileId: profileId,
                useFade: useFade,
                requestedBy: reason,
                reason: reason);

            DebugUtility.Log(typeof(GameNavigationService),
                $"[Navigation] NavigateAsync -> intentId='{intentId}', sceneRouteId='{entry.RouteId}', " +
                $"styleId='{entry.StyleId}', reason='{reason ?? "<null>"}', " +
                $"Load=[{string.Join(", ", request.ScenesToLoad)}], " +
                $"Unload=[{string.Join(", ", request.ScenesToUnload)}], " +
                $"Active='{request.TargetActiveScene}', UseFade={request.UseFade}, Profile='{request.TransitionProfileName}'.",
                DebugUtility.Colors.Info);

            await _sceneFlow.TransitionAsync(request);
        }

        private SceneTransitionPayload ResolvePayload(GameNavigationEntry entry)
        {
            var payload = entry.Payload ?? SceneTransitionPayload.Empty;

            if (payload.HasSceneData)
            {
                return payload;
            }

            if (_sceneRouteCatalog != null && _sceneRouteCatalog.TryGet(entry.RouteId, out var routeDefinition))
            {
                return payload.WithSceneData(routeDefinition);
            }

            return payload;
        }

        private (SceneFlowProfileId profileId, bool useFade) ResolveStyle(
            GameNavigationEntry entry,
            SceneTransitionPayload payload)
        {
            if (_styleCatalog != null && _styleCatalog.TryGet(entry.StyleId, out var style))
            {
                return (style.ProfileId, style.UseFade);
            }

            if (payload.HasLegacyStyle)
            {
                return (payload.LegacyProfileId, payload.UseFade);
            }

            return (SceneFlowProfileId.None, true);
        }
    }
}
