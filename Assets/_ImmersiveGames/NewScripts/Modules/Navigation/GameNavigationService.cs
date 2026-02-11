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
        private readonly ISceneRouteResolver _sceneRouteResolver;
        private readonly ITransitionStyleCatalog _styleCatalog;
        private readonly ILevelFlowService _levelFlowService;

        private int _navigationInProgress;

        [Obsolete("Debug only. Use the full constructor with SceneRoute/TransitionStyle/LevelFlow services.")]
        public GameNavigationService(ISceneTransitionService sceneFlow)
        {
            throw new InvalidOperationException(
                "GameNavigationService requer SceneRouteCatalog e TransitionStyleCatalog. Use o construtor completo.");
        }

        [Obsolete("Debug only. Use the full constructor with SceneRoute/TransitionStyle/LevelFlow services.")]
        public GameNavigationService(ISceneTransitionService sceneFlow, GameNavigationCatalog catalog)
        {
            throw new InvalidOperationException(
                "GameNavigationService requer SceneRouteCatalog e TransitionStyleCatalog. Use o construtor completo.");
        }

        [Obsolete("Use o construtor completo com SceneRouteCatalog/TransitionStyleCatalog/LevelFlowService.")]
        public GameNavigationService(ISceneTransitionService sceneFlow, GameNavigationCatalogAsset catalogAsset)
        {
            throw new InvalidOperationException(
                "GameNavigationService requer SceneRouteCatalog e TransitionStyleCatalog. Use o construtor completo.");
        }

        [Obsolete("Use o construtor completo com SceneRouteCatalog/TransitionStyleCatalog/LevelFlowService.")]
        public GameNavigationService(ISceneTransitionService sceneFlow, IGameNavigationCatalog catalog)
        {
            throw new InvalidOperationException(
                "GameNavigationService requer SceneRouteCatalog e TransitionStyleCatalog. Use o construtor completo.");
        }

        public GameNavigationService(
            ISceneTransitionService sceneFlow,
            IGameNavigationCatalog catalog,
            ISceneRouteResolver sceneRouteResolver,
            ITransitionStyleCatalog styleCatalog,
            ILevelFlowService levelFlowService)
        {
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _sceneRouteResolver = sceneRouteResolver ?? throw new ArgumentNullException(nameof(sceneRouteResolver));
            _styleCatalog = styleCatalog ?? throw new ArgumentNullException(nameof(styleCatalog));
            _levelFlowService = levelFlowService;

            DebugUtility.LogVerbose(typeof(GameNavigationService),
                $"[Navigation] GameNavigationService inicializado. Entries: [{string.Join(", ", _catalog.RouteIds)}]",
                DebugUtility.Colors.Info);
        }

        public Task GoToMenuAsync(string reason = null)
        {
            DebugUtility.LogVerbose(typeof(GameNavigationService),
                $"[OBS][Navigation] GoToMenuRequested reason='{reason ?? "<null>"}'.",
                DebugUtility.Colors.Info);
            return ExecuteIntentAsync(GameNavigationIntents.ToMenu, reason);
        }

        public Task RestartAsync(string reason = null)
        {
            DebugUtility.LogVerbose(typeof(GameNavigationService),
                $"[OBS][Navigation] RestartRequested reason='{reason ?? "<null>"}'.",
                DebugUtility.Colors.Info);
            return ExecuteIntentAsync(GameNavigationIntents.ToGameplay, reason);
        }

        public Task ExitToMenuAsync(string reason = null)
        {
            DebugUtility.LogVerbose(typeof(GameNavigationService),
                $"[OBS][Navigation] ExitToMenuRequested reason='{reason ?? "<null>"}'.",
                DebugUtility.Colors.Info);
            return ExecuteIntentAsync(GameNavigationIntents.ToMenu, reason);
        }

        [Obsolete("Use GoToMenuAsync(reason).")]
        public Task RequestMenuAsync(string reason = null)
            => GoToMenuAsync(reason);

        [Obsolete("Use RestartAsync(reason) ou StartGameplayAsync(levelId, reason).")]
        public Task RequestGameplayAsync(string reason = null)
            => RestartAsync(reason);

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
                DebugUtility.LogVerbose(typeof(GameNavigationService),
                    $"[OBS][Navigation] StartGameplayRequested levelId='{levelId}' reason='{reason ?? "<null>"}'.",
                    DebugUtility.Colors.Info);

                if (!_catalog.TryGet(GameNavigationIntents.ToGameplay, out var entry) || !entry.IsValid)
                {
                    DebugUtility.LogError(typeof(GameNavigationService),
                        $"[Navigation] Intent padrão de gameplay não encontrado ou inválido. " +
                        $"intentId='{GameNavigationIntents.ToGameplay}', levelId='{levelId}'. " +
                        $"Entries disponíveis: [{string.Join(", ", _catalog.RouteIds)}]");
                    return;
                }

                if (_levelFlowService == null)
                {
                    DebugUtility.LogWarning(typeof(GameNavigationService),
                        $"[Navigation] LevelFlow indisponível. Fallback para rota padrão. levelId='{levelId}'.");
                    await ExecuteEntryAsync(GameNavigationIntents.ToGameplay, entry, reason);
                    return;
                }

                if (!_levelFlowService.TryResolve(levelId, out var resolvedRouteId, out var payload))
                {
                    DebugUtility.LogWarning(typeof(GameNavigationService),
                        $"[Navigation] LevelId não resolvido. Fallback para rota padrão. levelId='{levelId}'.");
                    await ExecuteEntryAsync(GameNavigationIntents.ToGameplay, entry, reason);
                    return;
                }

                DebugUtility.Log(typeof(GameNavigationService),
                    $"[Navigation] LevelFlow resolvido. levelId='{levelId}', resolvedRouteId='{resolvedRouteId}', styleId='{entry.StyleId}', reason='{reason ?? "<null>"}'.",
                    DebugUtility.Colors.Info);

                var levelEntry = new GameNavigationEntry(resolvedRouteId, entry.StyleId, payload);
                await ExecuteEntryAsync(GameNavigationIntents.ToGameplay, levelEntry, reason);
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

        [Obsolete("Use métodos explícitos: GoToMenuAsync, RestartAsync, ExitToMenuAsync ou StartGameplayAsync(levelId, reason).")]
        public async Task NavigateAsync(string routeId, string reason = null)
            => await ExecuteIntentAsync(routeId, reason);

        private async Task ExecuteIntentAsync(string intentId, string reason = null)
        {
            if (string.IsNullOrWhiteSpace(intentId))
            {
                DebugUtility.LogError(typeof(GameNavigationService),
                    "[Navigation] NavigateAsync chamado com id vazio. Abortando.");
                return;
            }

            if (Interlocked.CompareExchange(ref _navigationInProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning(typeof(GameNavigationService),
                    $"[Navigation] Navegação já em progresso. Ignorando id='{intentId}'.");
                return;
            }

            try
            {
                if (!_catalog.TryGet(intentId, out var entry) || !entry.IsValid)
                {
                    DebugUtility.LogError(typeof(GameNavigationService),
                        $"[Navigation] Intent/rota desconhecida ou sem request. id='{intentId}'. " +
                        $"Entries disponíveis: [{string.Join(", ", _catalog.RouteIds)}].");
                    return;
                }

                await ExecuteEntryAsync(intentId, entry, reason);
            }
            catch (Exception ex)
            {
                // Comentário: navegação é infraestrutura de fluxo; não deve derrubar o jogo.
                DebugUtility.LogError(typeof(GameNavigationService),
                    $"[Navigation] Exceção ao navegar. id='{intentId}', reason='{reason ?? "<null>"}', ex={ex}");
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

            if (_sceneRouteResolver != null && _sceneRouteResolver.TryResolve(entry.RouteId, out var routeDefinition))
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
