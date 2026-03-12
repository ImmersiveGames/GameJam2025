using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// ImplementaÃ§Ã£o de produÃ§Ã£o: executa rotas via ISceneTransitionService.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameNavigationService : IGameNavigationService, IGameNavigationLegacyService
    {
        private readonly ISceneTransitionService _sceneFlow;
        private readonly IGameNavigationCatalog _catalog;
        private readonly ITransitionStyleCatalog _styleCatalog;
        private readonly GameNavigationIntentCatalogAsset _intentsCatalog;
        private readonly IRestartContextService _restartContextService;
        private readonly SceneRouteCatalogAsset _sceneRouteCatalog;

        private SceneRouteId _lastGameplayRouteId;
        private string _lastNavigationIntentId = string.Empty;
        private int _navigationInProgress;

        private static int _legacyApiWarningEmitted;

        public GameNavigationService(
            ISceneTransitionService sceneFlow,
            IGameNavigationCatalog catalog,
            ISceneRouteResolver sceneRouteResolver,
            ITransitionStyleCatalog styleCatalog,
            GameNavigationIntentCatalogAsset intentsCatalog,
            IRestartContextService restartContextService = null,
            SceneRouteCatalogAsset sceneRouteCatalog = null)
        {
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _ = sceneRouteResolver ?? throw new ArgumentNullException(nameof(sceneRouteResolver));
            _styleCatalog = styleCatalog ?? throw new ArgumentNullException(nameof(styleCatalog));
            _intentsCatalog = intentsCatalog;
            _restartContextService = restartContextService;
            _sceneRouteCatalog = sceneRouteCatalog;

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
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "Restart" : reason.Trim();

            GameplayStartSnapshot snapshot = default;
            if (_restartContextService == null ||
                !_restartContextService.TryGetCurrent(out snapshot) ||
                !snapshot.IsValid ||
                !snapshot.MacroRouteId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] RestartAsync requires a valid canonical gameplay snapshot. reason='{normalizedReason}'.");
            }

            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][Navigation] RestartUsingSnapshot routeId='{snapshot.MacroRouteId}' levelRef='{(snapshot.HasLevelRef ? snapshot.LevelRef.name : "<none>")}' styleId='{snapshot.StyleId}' v='{snapshot.SelectionVersion}' reason='{normalizedReason}' levelSignature='{(string.IsNullOrWhiteSpace(snapshot.LevelSignature) ? "<none>" : snapshot.LevelSignature)}'.",
                DebugUtility.Colors.Info);

            await StartGameplayRouteAsync(snapshot.MacroRouteId, SceneTransitionPayload.Empty, normalizedReason);
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

        // Compat temporaria com chamadas legacy; nao faz parte da interface principal.
        [Obsolete("Use GoToMenuAsync(reason).")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Task RequestMenuAsync(string reason = null)
        {
            WarnLegacyApiUsedOnce("RequestMenuAsync", "GoToMenuAsync(reason)");
            HardFailFastH1.Trigger(typeof(GameNavigationService), "[FATAL][H1][Navigation] Legacy API blocked: RequestMenuAsync. Use GoToMenuAsync(reason).");
            return Task.CompletedTask;
        }

        // Compat temporaria com chamadas legacy; nao faz parte da interface principal.
        [Obsolete("Legacy API blocked. Use RestartAsync(reason) ou ILevelFlowRuntimeService.StartGameplayDefaultAsync(reason, ct).") ]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Task RequestGameplayAsync(string reason = null)
        {
            WarnLegacyApiUsedOnce("RequestGameplayAsync", "RestartAsync(reason) or ILevelFlowRuntimeService.StartGameplayDefaultAsync(reason, ct)");
            HardFailFastH1.Trigger(typeof(GameNavigationService), "[FATAL][H1][Navigation] Legacy API blocked: RequestGameplayAsync. Use RestartAsync(reason) or StartGameplayDefaultAsync.");
            return Task.CompletedTask;
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

            var routeEntry = new GameNavigationEntry(routeId, gameplayEntry.StyleId, payload ?? SceneTransitionPayload.Empty);
            var (profile, profileId, _) = ResolveStyle(routeEntry);

            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][Navigation] StartGameplayRouteRequested routeId='{routeId}', reason='{normalizedReason}', styleId='{routeEntry.StyleId}', profile='{profileId}', profileAsset='{(profile != null ? profile.name : "<null>")}'.",
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

            if (routeAsset.RouteKind == SceneRouteKind.Gameplay)
            {
                if (routeAsset.LevelCollection == null ||
                    routeAsset.LevelCollection.Levels == null ||
                    routeAsset.LevelCollection.Levels.Count == 0)
                {
                    HardFailFastH1.Trigger(typeof(GameNavigationService),
                        $"[FATAL][H1][Navigation] Gameplay route without LevelCollection. routeId='{routeId}' reason='{reason}'.");
                }
            }
        }

        public Task NavigateAsync(GameNavigationIntentKind intent, string reason = null)
        {
            if (Interlocked.CompareExchange(ref _navigationInProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning(typeof(GameNavigationService),
                    $"[Navigation] NavegaÃ§Ã£o jÃ¡ em progresso. Ignorando intent core='{intent}'.");
                return Task.CompletedTask;
            }

            return ExecuteCoreIntentAsync(intent, reason);
        }

        // Compat temporaria com chamadas legacy/string-first; nao faz parte da interface principal.
        [Obsolete("Prefira NavigateAsync(GameNavigationIntentKind, reason) para core intents; mantenha string para extras/custom.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Task NavigateAsync(string routeId, string reason = null)
        {
            WarnLegacyApiUsedOnce("NavigateAsync(string)", "NavigateAsync(GameNavigationIntentKind, reason)");
            HardFailFastH1.Trigger(typeof(GameNavigationService),
                $"[FATAL][H1][Navigation] Legacy API blocked: NavigateAsync(string). routeId='{routeId ?? "<null>"}' reason='{reason ?? "<null>"}'.");
            return Task.CompletedTask;
        }

        private static void WarnLegacyApiUsedOnce(string apiName, string recommendation)
        {
            if (Interlocked.CompareExchange(ref _legacyApiWarningEmitted, 1, 0) != 0)
            {
                return;
            }

            DebugUtility.LogWarning(typeof(GameNavigationService),
                $"[WARN][LEGACY_API_USED] api='{apiName}' recommendation='{recommendation}'.");
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
                    $"[Navigation] NavegaÃ§Ã£o jÃ¡ em progresso. Ignorando id='{intentId}'.");
                return;
            }

            try
            {
                if (!_catalog.TryGet(intentId, out var entry) || !entry.IsValid)
                {
                    DebugUtility.LogError(typeof(GameNavigationService),
                        $"[Navigation] Intent/rota desconhecida ou sem request. id='{intentId}'. " +
                        $"Entries disponÃ­veis: [{string.Join(", ", _catalog.RouteIds)}].");
                    return;
                }

                await ExecuteEntryAsync(intentId, entry, reason);
            }
            catch (Exception ex)
            {
                // ComentÃ¡rio: navegaÃ§Ã£o Ã© infraestrutura de fluxo; nÃ£o deve derrubar o jogo.
                DebugUtility.LogError(typeof(GameNavigationService),
                    $"[Navigation] ExceÃ§Ã£o ao navegar. id='{intentId}', reason='{reason ?? "<null>"}', ex={ex}");
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
                // ComentÃ¡rio: navegaÃ§Ã£o Ã© infraestrutura de fluxo; nÃ£o deve derrubar o jogo.
                DebugUtility.LogError(typeof(GameNavigationService),
                    $"[Navigation] ExceÃ§Ã£o ao navegar (core). intent='{intent}', reason='{reason ?? "<null>"}', ex={ex}");
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
                    $"[FATAL][Config] GameNavigationIntentCatalogAsset invÃ¡lido para intent core. intent='{intent}', intentId='<empty>'.";
                DebugUtility.LogError(typeof(GameNavigationService), message);
                throw new InvalidOperationException(message);
            }

            return intentId.Value;
        }

        private async Task ExecuteEntryAsync(string intentId, GameNavigationEntry entry, string reason)
        {
            // F3 Plan-v2:
            // - SceneRouteDefinition Ã© a fonte Ãºnica de Scene Data.
            // - Navigation nÃ£o injeta dados de cena no payload.
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
                $"[FATAL][Config] TransitionStyleId sem resoluÃ§Ã£o no catÃ¡logo. styleId='{entry.StyleId}', routeId='{entry.RouteId}'.");
        }
    }
}




















