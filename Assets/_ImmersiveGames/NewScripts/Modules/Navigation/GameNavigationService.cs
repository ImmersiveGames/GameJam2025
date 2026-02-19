using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
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
        private readonly ITransitionStyleCatalog _styleCatalog;
        private readonly ILevelFlowService _levelFlowService;
        private readonly GameNavigationIntentCatalogAsset _intentsCatalog;
        private readonly IRestartContextService _restartContextService;

        private LevelId _lastStartedGameplayLevelId;
        private SceneRouteId _lastGameplayRouteId;
        private string _lastNavigationIntentId = string.Empty;
        private int _navigationInProgress;

        public GameNavigationService(
            ISceneTransitionService sceneFlow,
            IGameNavigationCatalog catalog,
            ISceneRouteResolver sceneRouteResolver,
            ITransitionStyleCatalog styleCatalog,
            ILevelFlowService levelFlowService,
            GameNavigationIntentCatalogAsset intentsCatalog,
            IRestartContextService restartContextService = null)
        {
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _ = sceneRouteResolver ?? throw new ArgumentNullException(nameof(sceneRouteResolver));
            _styleCatalog = styleCatalog ?? throw new ArgumentNullException(nameof(styleCatalog));
            _levelFlowService = levelFlowService;
            _intentsCatalog = intentsCatalog;
            _restartContextService = restartContextService;

            if (_intentsCatalog == null)
            {
                string message =
                    "[FATAL][Config] GameNavigationService requires GameNavigationIntentCatalogAsset (navigationIntentCatalog).";
                DebugUtility.LogError(typeof(GameNavigationService), message);
                throw new InvalidOperationException(message);
            }

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
            string resolvedReason = string.IsNullOrWhiteSpace(reason) ? "Restart" : reason.Trim();

            GameplayStartSnapshot snapshot = default;
            bool hasSnapshot = _restartContextService != null && _restartContextService.TryGetCurrent(out snapshot);
            if (hasSnapshot)
            {
                DebugUtility.Log(typeof(GameNavigationService),
                    $"[OBS][Navigation] RestartUsingSnapshot routeId='{snapshot.RouteId}', levelId='{(snapshot.HasLevelId ? snapshot.LevelId.ToString() : "<none>")}', contentId='{(snapshot.HasContentId ? snapshot.ContentId : "<none>")}', styleId='{snapshot.StyleId}', v='{snapshot.SelectionVersion}', reason='{(string.IsNullOrWhiteSpace(reason) ? snapshot.Reason : resolvedReason)}', contextSignature='{(string.IsNullOrWhiteSpace(snapshot.ContextSignature) ? "<none>" : snapshot.ContextSignature)}'.",
                    DebugUtility.Colors.Info);
            }
            else
            {
                DebugUtility.Log(typeof(GameNavigationService),
                    "[OBS][Navigation] RestartUsingSnapshot status='empty_or_compat'.",
                    DebugUtility.Colors.Info);
            }

            SceneRouteId resolvedRouteId = SceneRouteId.None;
            SceneTransitionPayload payload = SceneTransitionPayload.Empty;
            string resolveSource = "unknown";

            if (hasSnapshot && snapshot.RouteId.IsValid)
            {
                resolvedRouteId = snapshot.RouteId;
                payload = SceneTransitionPayload.Empty;
                resolveSource = "snapshot";
            }
            else
            {
                if (_lastStartedGameplayLevelId.IsValid)
                {
                    if (_levelFlowService == null)
                    {
                        if (_lastGameplayRouteId.IsValid)
                        {
                            resolvedRouteId = _lastGameplayRouteId;
                            payload = SceneTransitionPayload.Empty;
                            resolveSource = "legacy_route_only";

                            DebugUtility.LogWarning(typeof(GameNavigationService),
                                $"[OBS][Navigation][Compat] ReverseLookupMissingOrAmbiguous routeId='{_lastGameplayRouteId}' reason='Restart/MissingILevelFlowService'.");
                        }
                        else
                        {
                            DebugUtility.LogError(typeof(GameNavigationService),
                                $"[FATAL][Config] Missing ILevelFlowService for Restart legacy fallback. levelId='{_lastStartedGameplayLevelId}', reason='{reason ?? "<null>"}'.");
                            throw new InvalidOperationException("[FATAL][Config] Missing ILevelFlowService for Restart legacy fallback.");
                        }
                    }
                    else if (!_levelFlowService.TryResolve(_lastStartedGameplayLevelId, out resolvedRouteId, out var resolvedPayload) || !resolvedRouteId.IsValid)
                    {
                        if (_lastGameplayRouteId.IsValid)
                        {
                            resolvedRouteId = _lastGameplayRouteId;
                            payload = SceneTransitionPayload.Empty;
                            resolveSource = "legacy_route_only";

                            DebugUtility.LogWarning(typeof(GameNavigationService),
                                $"[OBS][Navigation][Compat] ReverseLookupMissingOrAmbiguous routeId='{_lastGameplayRouteId}' levelId='{_lastStartedGameplayLevelId}' reason='Restart/LevelResolveFailed'.");
                        }
                        else
                        {
                            DebugUtility.LogError(typeof(GameNavigationService),
                                $"[FATAL][Config] Restart unresolved LevelId in ILevelFlowService. levelId='{_lastStartedGameplayLevelId}', reason='{reason ?? "<null>"}'.");
                            throw new InvalidOperationException("[FATAL][Config] Restart unresolved LevelId in ILevelFlowService.");
                        }
                    }
                    else
                    {
                        payload = resolvedPayload ?? SceneTransitionPayload.Empty;
                        resolveSource = "legacy";
                    }
                }
                else if (_lastGameplayRouteId.IsValid)
                {
                    resolvedRouteId = _lastGameplayRouteId;
                    payload = SceneTransitionPayload.Empty;
                    resolveSource = "legacy_route_only";
                }
                else
                {
                    string lastRouteId = _lastGameplayRouteId.IsValid ? _lastGameplayRouteId.ToString() : "<none>";
                    string lastIntent = string.IsNullOrWhiteSpace(_lastNavigationIntentId) ? "<none>" : _lastNavigationIntentId;

                    DebugUtility.LogError(typeof(GameNavigationService),
                        $"[FATAL][Config] Restart called before StartGameplayAsync; no last levelId/routeId. lastRouteId='{lastRouteId}', lastIntent='{lastIntent}', reason='{reason ?? "<null>"}'.");
                    throw new InvalidOperationException($"[FATAL][Config] Restart called before StartGameplayAsync; no last levelId/routeId. lastRouteId='{lastRouteId}', lastIntent='{lastIntent}'.");
                }
            }

            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][Navigation] RestartResolved routeId='{resolvedRouteId}' source='{resolveSource}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][Navigation] RestartRequested levelId='{_lastStartedGameplayLevelId}', routeId='{resolvedRouteId}', reason='{resolvedReason}'.",
                DebugUtility.Colors.Info);

            await StartGameplayRouteAsync(resolvedRouteId, payload, resolvedReason);
        }


        public Task ExitToMenuAsync(string reason = null)
        {
            string styleLabel = "<unknown>";
            string profileLabel = "<unknown>";
            if (TryResolveCoreEntry(GameNavigationIntentKind.Menu, out var menuEntry) && menuEntry.IsValid)
            {
                styleLabel = menuEntry.StyleId.ToString();
                if (_styleCatalog != null && _styleCatalog.TryGet(menuEntry.StyleId, out var style))
                {
                    profileLabel = style.ProfileId.ToString();
                }
            }

            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][Navigation] ExitToMenuRequested reason='{reason ?? "<null>"}', styleId='{styleLabel}', profile='{profileLabel}'.",
                DebugUtility.Colors.Info);
            return NavigateAsync(GameNavigationIntentKind.Menu, reason);
        }

        [Obsolete("Use GoToMenuAsync(reason).")]
        public Task RequestMenuAsync(string reason = null)
        {
            DebugUtility.LogError(typeof(GameNavigationService),
                "[FATAL][Config] Obsolete navigation API called: RequestMenuAsync. Use the non-obsolete API.");
            throw new InvalidOperationException("Obsolete navigation API called: RequestMenuAsync. Use GoToMenuAsync(reason).");
        }

        [Obsolete("Use RestartAsync(reason) ou StartGameplayAsync(levelId, reason).")]
        public Task RequestGameplayAsync(string reason = null)
        {
            DebugUtility.LogError(typeof(GameNavigationService),
                "[FATAL][Config] Obsolete navigation API called: RequestGameplayAsync. Use the non-obsolete API.");
            throw new InvalidOperationException("Obsolete navigation API called: RequestGameplayAsync. Use RestartAsync(reason) or StartGameplayAsync(levelId, reason).");
        }

        [Obsolete("Use ILevelFlowRuntimeService.StartGameplayAsync(levelId, reason, ct) ou IGameNavigationService.StartGameplayRouteAsync(routeId, payload, reason).") ]
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
                if (!TryResolveCoreEntry(GameNavigationIntentKind.Gameplay, out var entry) || !entry.IsValid)
                {
                    DebugUtility.LogError(typeof(GameNavigationService),
                        $"[FATAL][Config] Missing gameplay intent entry. " +
                        $"intentId='{GetCoreIntentId(GameNavigationIntentKind.Gameplay)}', levelId='{levelId}'. " +
                        $"Entries disponíveis: [{string.Join(", ", _catalog.RouteIds)}].");
                    throw new InvalidOperationException(
                        $"[FATAL][Config] Missing gameplay intent entry '{GetCoreIntentId(GameNavigationIntentKind.Gameplay)}'.");
                }

                if (_levelFlowService == null)
                {
                    DebugUtility.LogError(typeof(GameNavigationService),
                        $"[FATAL][Config] Missing ILevelFlowService. levelId='{levelId}', reason='{reason ?? "<null>"}'.");
                    throw new InvalidOperationException("[FATAL][Config] Missing ILevelFlowService.");
                }

                if (!_levelFlowService.TryResolve(levelId, out var resolvedRouteId, out var payload) || !resolvedRouteId.IsValid)
                {
                    DebugUtility.LogError(typeof(GameNavigationService),
                        $"[FATAL][Config] LevelId unresolved in ILevelFlowService. levelId='{levelId}', reason='{reason ?? "<null>"}'.");
                    throw new InvalidOperationException(
                        $"[FATAL][Config] LevelId unresolved in ILevelFlowService. levelId='{levelId}'.");
                }

                DebugUtility.LogVerbose(typeof(GameNavigationService),
                    $"[OBS][Navigation] StartGameplayRequested levelId='{levelId}' routeId='{resolvedRouteId}' reason='{reason ?? "<null>"}'.",
                    DebugUtility.Colors.Info);

                UpdateLastLevelId(levelId, resolvedRouteId, source: "LegacyStartGameplayAsync", reason: reason);
                await StartGameplayRouteAsync(resolvedRouteId, payload, reason);

                DebugUtility.LogVerbose(typeof(GameNavigationService),
                    $"[OBS][Navigation] StartGameplayCompleted levelId='{levelId}' routeId='{resolvedRouteId}' reason='{reason ?? "<null>"}'.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GameNavigationService),
                    $"[Navigation] Exceção ao iniciar gameplay. levelId='{levelId}', reason='{reason ?? "<null>"}', ex={ex}");
                throw;
            }
            finally
            {
                Interlocked.Exchange(ref _navigationInProgress, 0);
            }
        }

        public async Task StartGameplayRouteAsync(SceneRouteId routeId, SceneTransitionPayload payload = null, string reason = null)
        {
            if (!routeId.IsValid)
            {
                DebugUtility.LogError(typeof(GameNavigationService),
                    $"[FATAL][Config] StartGameplayRouteAsync com RouteId inválido. routeId='{routeId}', reason='{reason ?? "<null>"}'.");
                throw new InvalidOperationException("[FATAL][Config] StartGameplayRouteAsync with invalid RouteId.");
            }

            if (!TryResolveCoreEntry(GameNavigationIntentKind.Gameplay, out var gameplayEntry) || !gameplayEntry.IsValid)
            {
                DebugUtility.LogError(typeof(GameNavigationService),
                    $"[FATAL][Config] Missing gameplay intent entry for StartGameplayRouteAsync. intentId='{GetCoreIntentId(GameNavigationIntentKind.Gameplay)}'.");
                throw new InvalidOperationException("[FATAL][Config] Missing gameplay intent entry.");
            }

            LevelId resolvedLevelId = LevelId.None;
            string resolveSource = "none";

            GameplayStartSnapshot snapshot = default;
            bool hasSnapshot = _restartContextService != null && _restartContextService.TryGetCurrent(out snapshot);
            if (hasSnapshot && snapshot.RouteId == routeId && snapshot.HasLevelId)
            {
                resolvedLevelId = snapshot.LevelId;
                resolveSource = "snapshot";
            }
            else if (_levelFlowService != null && _levelFlowService.TryResolveLevelId(routeId, out var mappedLevelId) && mappedLevelId.IsValid)
            {
                resolvedLevelId = mappedLevelId;
                resolveSource = "legacy_reverse_lookup";
            }
            else
            {
                DebugUtility.LogWarning(typeof(GameNavigationService),
                    $"[OBS][Navigation][Compat] ReverseLookupMissingOrAmbiguous routeId='{routeId}' reason='StartGameplayRouteAsync'.");
            }

            UpdateLastLevelId(resolvedLevelId, routeId, source: $"StartGameplayRouteAsync/{resolveSource}", reason: reason);

            var routeEntry = new GameNavigationEntry(routeId, gameplayEntry.StyleId, payload ?? SceneTransitionPayload.Empty);

            var (profile, profileId, _) = ResolveStyle(routeEntry);
            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][Navigation] StartGameplayRouteRequested routeId='{routeId}', reason='{reason ?? "<null>"}', styleId='{routeEntry.StyleId}', profile='{profileId}', profileAsset='{(profile != null ? profile.name : "<null>")}'.",
                DebugUtility.Colors.Info);

            await ExecuteEntryAsync(GetCoreIntentId(GameNavigationIntentKind.Gameplay), routeEntry, reason);
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

        [Obsolete("Prefira NavigateAsync(GameNavigationIntentKind, reason) para core intents; mantenha string para extras/custom.")]
        public Task NavigateAsync(string routeId, string reason = null)
        {
            return ExecuteIntentAsync(routeId, reason);
        }

        private async Task ExecuteIntentAsync(string intentId, string reason = null)
        {
            if (string.IsNullOrWhiteSpace(intentId))
            {
                DebugUtility.LogError(typeof(GameNavigationService),
                    "[Navigation] ExecuteIntentAsync chamado com id vazio. Abortando.");
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

        private async Task ExecuteCoreIntentAsync(GameNavigationIntentKind intent, string reason)
        {
            string intentId = GetCoreIntentId(intent);

            try
            {
                if (!TryResolveCoreEntry(intent, out GameNavigationEntry entry) || !entry.IsValid)
                {
                    DebugUtility.LogError(typeof(GameNavigationService),
                        $"[FATAL][Config] Missing core intent entry. intent='{intent}', intentId='{intentId}'.");
                    throw new InvalidOperationException(
                        $"[FATAL][Config] Missing core intent entry '{intentId}'.");
                }

                DebugUtility.LogVerbose(typeof(GameNavigationService),
                    $"[OBS][Navigation] RuntimeResolveChain intent -> entry -> routeRef. intentKind='{intent}', intentId='{intentId}', routeId='{entry.RouteId}'.",
                    DebugUtility.Colors.Info);

                await ExecuteEntryAsync(intentId, entry, reason);
            }
            catch (Exception ex)
            {
                // Comentário: navegação é infraestrutura de fluxo; não deve derrubar o jogo.
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

        private string GetCoreIntentId(GameNavigationIntentKind intent)
        {
            NavigationIntentId intentId;
            switch (intent)
            {
                case GameNavigationIntentKind.Menu:
                    intentId = _intentsCatalog.Menu;
                    break;
                case GameNavigationIntentKind.Gameplay:
                    intentId = _intentsCatalog.Gameplay;
                    break;
                case GameNavigationIntentKind.GameOver:
                    intentId = _intentsCatalog.GameOver;
                    break;
                case GameNavigationIntentKind.Victory:
                    intentId = _intentsCatalog.Victory;
                    break;
                case GameNavigationIntentKind.Restart:
                    intentId = _intentsCatalog.Restart;
                    break;
                case GameNavigationIntentKind.ExitToMenu:
                    intentId = _intentsCatalog.ExitToMenu;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(intent), intent, null);
            }

            if (!intentId.IsValid)
            {
                string message =
                    $"[FATAL][Config] GameNavigationIntentCatalogAsset inválido para intent core. intent='{intent}', intentId='<empty>'.";
                DebugUtility.LogError(typeof(GameNavigationService), message);
                throw new InvalidOperationException(message);
            }

            return intentId.Value;
        }

        private async Task ExecuteEntryAsync(string intentId, GameNavigationEntry entry, string reason)
        {
            // F3 Plan-v2:
            // - SceneRouteDefinition é a fonte única de Scene Data.
            // - Navigation não injeta dados de cena no payload.
            var payload = entry.Payload ?? SceneTransitionPayload.Empty;
            var (profile, profileId, useFade) = ResolveStyle(entry);

            _lastNavigationIntentId = intentId ?? string.Empty;
            _lastGameplayRouteId = entry.RouteId;

            var request = new SceneTransitionRequest(
                entry.RouteId,
                entry.StyleId,
                payload,
                profile,
                transitionProfileId: profileId,
                useFade: useFade,
                requestedBy: reason,
                reason: reason);

            var signature = SceneTransitionSignature.Compute(SceneTransitionSignature.BuildContext(request));

            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][Navigation] DispatchIntent -> intentId='{intentId}', sceneRouteId='{entry.RouteId}', " +
                $"styleId='{entry.StyleId}', reason='{reason ?? "<null>"}', " +
                $"signature='{signature}', UseFade={request.UseFade}, Profile='{request.TransitionProfileName}'.",
                DebugUtility.Colors.Info);

            await _sceneFlow.TransitionAsync(request);
        }


        private void UpdateLastLevelId(LevelId levelId, SceneRouteId routeId, string source, string reason)
        {
            _lastStartedGameplayLevelId = levelId.IsValid ? levelId : LevelId.None;

            _lastGameplayRouteId = routeId;

            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][Navigation] LastLevelIdUpdated levelId='{levelId}' source='{source}' reason='{reason ?? "<null>"}'.",
                DebugUtility.Colors.Info);
        }

        private (SceneTransitionProfile? profile, SceneFlowProfileId profileId, bool useFade) ResolveStyle(
            GameNavigationEntry entry)
        {
            if (_styleCatalog != null && _styleCatalog.TryGet(entry.StyleId, out var style))
            {
                if (style.Profile == null)
                {
                    DebugUtility.LogWarning(typeof(GameNavigationService),
                        $"[WARN][Degraded] TransitionStyle sem SceneTransitionProfile. styleId='{entry.StyleId}', routeId='{entry.RouteId}'. Fallback=no-fade (dur=0).");

                    return (null, style.ProfileId, false);
                }

                return (style.Profile, style.ProfileId, style.UseFade);
            }

            throw new InvalidOperationException(
                $"[FATAL][Config] TransitionStyleId sem resolução no catálogo. styleId='{entry.StyleId}', routeId='{entry.RouteId}'.");
        }
    }
}
