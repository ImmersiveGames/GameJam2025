using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate.Interop;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.PostGame;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Serviço de navegação canônico que gerencia transições entre estados principais do jogo.
    /// Coordena requisições de navegação (Menu, Gameplay, Exit, etc) com o SceneFlow.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameNavigationService : IGameNavigationService
    {
        private readonly ISceneTransitionService _sceneFlow;
        private readonly IGameNavigationCatalog _catalog;
        private readonly IRestartContextService _restartContextService;
        private readonly SceneRouteCatalogAsset _sceneRouteCatalog;
        private readonly SemaphoreSlim _macroCommandGate = new SemaphoreSlim(1, 1);

        private SceneRouteId _lastGameplayRouteId;
        private string _lastNavigationIntentId = string.Empty;
        private int _navigationInProgress;

        public GameNavigationService(
            ISceneTransitionService sceneFlow,
            IGameNavigationCatalog catalog,
            ISceneRouteResolver sceneRouteResolver,
            IRestartContextService restartContextService = null,
            SceneRouteCatalogAsset sceneRouteCatalog = null)
        {
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _ = sceneRouteResolver ?? throw new ArgumentNullException(nameof(sceneRouteResolver));
            _restartContextService = restartContextService;
            _sceneRouteCatalog = sceneRouteCatalog;

            DebugUtility.LogVerbose(typeof(GameNavigationService),
                $"[Navigation] GameNavigationService inicializado. Entries: [{string.Join(", ", _catalog.RouteIds)}]",
                DebugUtility.Colors.Info);
        }

        public Task GoToMenuAsync(string reason = null)
        {
            DebugUtility.LogVerbose(typeof(GameNavigationService),
                $"[OBS][Navigation] GoToMenuRequested reason='{reason ?? "<null>"}'.",
                DebugUtility.Colors.Info);
            return NavigateAsync(GameNavigationIntentKind.Menu, reason);
        }

        public async Task RestartAsync(string reason = null)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "Restart" : reason.Trim();
            GameplayStartSnapshot snapshot = default;
            if (_restartContextService == null || !_restartContextService.TryGetCurrent(out snapshot) || !snapshot.IsValid || !snapshot.MacroRouteId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] RestartAsync requires a valid canonical gameplay snapshot. reason='{normalizedReason}'.");
            }

            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][Navigation] RestartUsingSnapshot routeId='{snapshot.MacroRouteId}' levelRef='{(snapshot.HasLevelRef ? snapshot.LevelRef.name : "<none>")}' v='{snapshot.SelectionVersion}' reason='{normalizedReason}' levelSignature='{(string.IsNullOrWhiteSpace(snapshot.LevelSignature) ? "<none>" : snapshot.LevelSignature)}'.",
                DebugUtility.Colors.Info);

            await StartGameplayRouteAsync(snapshot.MacroRouteId, SceneTransitionPayload.Empty, normalizedReason);
        }

        public Task ExitToMenuAsync(string reason = null)
        {
            string styleLabel = "<unknown>";
            string profileLabel = "<unknown>";
            if (TryResolveCoreEntry(GameNavigationIntentKind.Menu, out var menuEntry) && menuEntry.IsValid)
            {
                styleLabel = string.IsNullOrWhiteSpace(menuEntry.StyleLabel) ? "<unknown>" : menuEntry.StyleLabel;
                TransitionStyleDefinition definition = ResolveStyle(menuEntry);
                profileLabel = string.IsNullOrWhiteSpace(definition.ProfileLabel) ? "<unknown>" : definition.ProfileLabel;
            }

            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][Navigation] ExitToMenuRequested reason='{reason ?? "<null>"}', style='{styleLabel}', profile='{profileLabel}'.",
                DebugUtility.Colors.Info);
            return NavigateAsync(GameNavigationIntentKind.Menu, reason);
        }

        public async Task RestartMacroAsync(string reason = null)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "Restart/Unspecified" : reason.Trim();

            await _macroCommandGate.WaitAsync();
            try
            {
                ResolveMacroRestartDependenciesOrFail(normalizedReason, out var gameLoopService, out var levelFlowRuntimeService, out var restartContextService);

                DebugUtility.Log(typeof(GameNavigationService),
                    $"[OBS][Navigation] MacroRestartDispatch reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                restartContextService.Clear(normalizedReason);
                gameLoopService.RequestReset();
                await levelFlowRuntimeService.StartGameplayDefaultAsync(normalizedReason, CancellationToken.None);
            }
            finally
            {
                _macroCommandGate.Release();
            }
        }

        public async Task ExitToMenuMacroAsync(string reason = null)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "ExitToMenu/Unspecified" : reason.Trim();

            await _macroCommandGate.WaitAsync();
            try
            {
                ResolveMacroNavigationDependenciesOrFail(normalizedReason, out var loop);

                DebugUtility.Log(typeof(GameNavigationService),
                    $"[OBS][Navigation] MacroExitToMenuDispatch reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                ReleasePauseGateIfPresent(normalizedReason);
                MarkExitResultIfInPostGame(loop, normalizedReason);

                loop.RequestReady();
                await ExitToMenuAsync(normalizedReason);
            }
            finally
            {
                _macroCommandGate.Release();
            }
        }

        public SceneRouteId ResolveGameplayRouteIdOrFail()
        {
            if (!TryResolveCoreEntry(GameNavigationIntentKind.Gameplay, out var gameplayEntry) || !gameplayEntry.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] Missing gameplay intent entry while resolving canonical gameplay route. intentId='{GetCoreIntentId(GameNavigationIntentKind.Gameplay)}'.");
            }

            return gameplayEntry.RouteId;
        }

        public async Task StartGameplayRouteAsync(SceneRouteId routeId, SceneTransitionPayload payload = null, string reason = null)
        {
            if (!routeId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] StartGameplayRouteAsync with invalid routeId. routeId='{routeId}' reason='{reason ?? "<null>"}'.");
            }

            if (!TryResolveCoreEntry(GameNavigationIntentKind.Gameplay, out var gameplayEntry) || !gameplayEntry.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] Missing gameplay intent entry for StartGameplayRouteAsync. intentId='{GetCoreIntentId(GameNavigationIntentKind.Gameplay)}'.");
            }

            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "Navigation/StartGameplayRoute" : reason.Trim();
            ValidateGameplayRouteCollectionOrFail(routeId, normalizedReason);

            DebugUtility.Log(typeof(GameNavigationService),
                "[OBS][Navigation] StartGameplayRouteAsync without level selection; default will be selected in LevelPrepare.",
                DebugUtility.Colors.Info);

            var routeEntry = new GameNavigationEntry(routeId, gameplayEntry.StyleRef, payload ?? SceneTransitionPayload.Empty);
            TransitionStyleDefinition definition = ResolveStyle(routeEntry);

            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][Navigation] StartGameplayRouteRequested routeId='{routeId}', reason='{normalizedReason}', style='{routeEntry.StyleLabel}', profile='{definition.ProfileLabel}', profileAsset='{(definition.Profile != null ? definition.Profile.name : "<null>")}'.",
                DebugUtility.Colors.Info);

            await ExecuteEntryAsync(GetCoreIntentId(GameNavigationIntentKind.Gameplay), routeEntry, normalizedReason);
        }

        private void ValidateGameplayRouteCollectionOrFail(SceneRouteId routeId, string reason)
        {
            if (_sceneRouteCatalog == null)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] Gameplay route validation requires SceneRouteCatalogAsset. routeId='{routeId}' reason='{reason}'.");
            }

            if (!_sceneRouteCatalog.TryGetAsset(routeId, out SceneRouteDefinitionAsset routeAsset) || routeAsset == null)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] RouteAsset missing from SceneRouteCatalogAsset. routeId='{routeId}' reason='{reason}'.");
            }

            if (routeAsset.RouteKind == SceneRouteKind.Gameplay && (routeAsset.LevelCollection == null || routeAsset.LevelCollection.Levels == null || routeAsset.LevelCollection.Levels.Count == 0))
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] Gameplay route without LevelCollection. routeId='{routeId}' reason='{reason}'.");
            }
        }

        private static void ResolveMacroRestartDependenciesOrFail(
            string reason,
            out IGameLoopService gameLoopService,
            out ILevelFlowRuntimeService levelFlowRuntimeService,
            out IRestartContextService restartContextService)
        {
            ResolveMacroNavigationDependenciesOrFail(reason, out gameLoopService);

            if (!DependencyManager.Provider.TryGetGlobal(out levelFlowRuntimeService) || levelFlowRuntimeService == null)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] MacroRestart missing ILevelFlowRuntimeService. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal(out restartContextService) || restartContextService == null)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] MacroRestart missing IRestartContextService. reason='{reason}'.");
            }
        }

        private static void ResolveMacroNavigationDependenciesOrFail(string reason, out IGameLoopService gameLoopService)
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] Macro navigation missing DependencyManager.Provider. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal(out gameLoopService) || gameLoopService == null)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] Macro navigation missing IGameLoopService. reason='{reason}'.");
            }
        }

        private static void ReleasePauseGateIfPresent(string reason)
        {
            if (DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<GamePauseGateBridge>(out var pauseBridge) &&
                pauseBridge != null)
            {
                pauseBridge.ReleaseForExitToMenu(reason);
            }
        }

        private static void MarkExitResultIfInPostGame(IGameLoopService loop, string reason)
        {
            if (loop == null || !string.Equals(loop.CurrentStateIdName, nameof(GameLoopStateId.PostPlay), StringComparison.Ordinal))
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<IPostGameResultService>(out var resultService) && resultService != null)
            {
                resultService.TrySetExit(reason);
            }
        }

        public Task NavigateAsync(GameNavigationIntentKind intent, string reason = null)
        {
            if (Interlocked.CompareExchange(ref _navigationInProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning(typeof(GameNavigationService),
                    $"[Navigation] Navegação já em progresso. Ignorando intent core='{intent}'.");
                return Task.CompletedTask;
            }

            return ExecuteCoreIntentAsync(intent, reason);
        }

        private async Task ExecuteCoreIntentAsync(GameNavigationIntentKind intent, string reason)
        {
            string intentId = GetCoreIntentId(intent);
            try
            {
                if (!TryResolveCoreEntry(intent, out GameNavigationEntry entry) || !entry.IsValid)
                {
                    throw new InvalidOperationException($"[FATAL][Config] Missing core intent entry '{intentId}'.");
                }

                await ExecuteEntryAsync(intentId, entry, reason);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GameNavigationService),
                    $"[Navigation] Exceção ao navegar (core). intent='{intent}', reason='{reason ?? "<null>"}', ex={ex}");
            }
            finally
            {
                Interlocked.Exchange(ref _navigationInProgress, 0);
            }
        }

        private bool TryResolveCoreEntry(GameNavigationIntentKind intent, out GameNavigationEntry entry)
        {
            if (_catalog is GameNavigationCatalogAsset assetCatalog)
            {
                entry = assetCatalog.ResolveCoreOrFail(intent);
                return entry.IsValid;
            }

            string intentId = GetCoreIntentId(intent);
            return _catalog.TryGet(intentId, out entry) && entry.IsValid;
        }

        private static string GetCoreIntentId(GameNavigationIntentKind intent)
        {
            NavigationIntentId intentId = GameNavigationIntents.GetCoreId(intent);
            if (!intentId.IsValid)
            {
                string message = $"[FATAL][Config] GameNavigationIntents inválido para intent core. intent='{intent}', intentId='<empty>'.";
                DebugUtility.LogError(typeof(GameNavigationService), message);
                throw new InvalidOperationException(message);
            }

            return intentId.Value;
        }

        private async Task ExecuteEntryAsync(string intentId, GameNavigationEntry entry, string reason)
        {
            TransitionStyleDefinition definition = ResolveStyle(entry);
            _lastNavigationIntentId = intentId ?? string.Empty;
            _lastGameplayRouteId = entry.RouteId;

            var request = new SceneTransitionRequest(
                entry.RouteId,
                entry.StyleRef,
                entry.Payload ?? SceneTransitionPayload.Empty,
                definition.Profile,
                useFade: definition.UseFade,
                requestedBy: reason,
                reason: reason);

            string signature = SceneTransitionSignature.Compute(SceneTransitionSignature.BuildContext(request));
            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][Navigation] DispatchIntent -> intentId='{intentId}', sceneRouteId='{entry.RouteId}', style='{request.StyleLabel}', reason='{reason ?? "<null>"}', signature='{signature}', UseFade={request.UseFade}, Profile='{request.TransitionProfileName}'.",
                DebugUtility.Colors.Info);

            await _sceneFlow.TransitionAsync(request);
        }

        private static TransitionStyleDefinition ResolveStyle(GameNavigationEntry entry)
        {
            if (entry.StyleRef == null)
            {
                throw new InvalidOperationException($"[FATAL][Config] Navegação sem TransitionStyleAsset direto. routeId='{entry.RouteId}'.");
            }

            return entry.StyleRef.ToDefinitionOrFail(nameof(GameNavigationService), $"routeId='{entry.RouteId}'");
        }
    }
}

