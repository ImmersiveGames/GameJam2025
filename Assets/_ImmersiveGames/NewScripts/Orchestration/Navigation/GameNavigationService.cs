using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.Navigation
{
    /// <summary>
    /// Canonical navigation service.
    /// Owns core intent resolution and dispatch through SceneFlow.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameNavigationService : IGameNavigationService
    {
        private readonly ISceneTransitionService _sceneFlow;
        private readonly IGameNavigationCatalog _catalog;
        private int _navigationInProgress;

        public GameNavigationService(
            ISceneTransitionService sceneFlow,
            IGameNavigationCatalog catalog)
        {
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

            DebugUtility.LogVerbose(typeof(GameNavigationService),
                $"[Navigation] GameNavigationService initialized. Entries: [{string.Join(", ", _catalog.RouteIds)}]",
                DebugUtility.Colors.Info);
        }

        public Task GoToMenuAsync(string reason = null)
        {
            DebugUtility.LogVerbose(typeof(GameNavigationService),
                $"[OBS][Navigation] GoToMenuRequested reason='{reason ?? "<null>"}'.",
                DebugUtility.Colors.Info);
            return NavigateAsync(GameNavigationIntentKind.Menu, reason);
        }

        public SceneRouteId ResolveGameplayRouteIdOrFail()
        {
            if (!TryResolveCoreEntry(GameNavigationIntentKind.Gameplay, out var gameplayEntry) || !gameplayEntry.IsValid || gameplayEntry.RouteRef == null)
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
            ValidateGameplayRouteOrFail(routeId, gameplayEntry, normalizedReason);

            DebugUtility.Log(typeof(GameNavigationService),
                "[OBS][Navigation] StartGameplayRouteAsync dispatched without explicit level selection; LevelFlow owns default selection in LevelPrepare.",
                DebugUtility.Colors.Info);

            var routeEntry = new GameNavigationEntry(routeId, gameplayEntry.StyleRef, payload ?? SceneTransitionPayload.Empty, gameplayEntry.RouteRef);
            TransitionStyleDefinition definition = ResolveStyle(routeEntry);

            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][Navigation] StartGameplayRouteRequested routeId='{routeId}', reason='{normalizedReason}', style='{routeEntry.StyleLabel}', profile='{definition.ProfileLabel}', profileAsset='{(definition.Profile != null ? definition.Profile.name : "<null>")}'.",
                DebugUtility.Colors.Info);

            await ExecuteEntryAsync(GetCoreIntentId(GameNavigationIntentKind.Gameplay), routeEntry, normalizedReason);
        }

        public Task NavigateAsync(GameNavigationIntentKind intent, string reason = null)
        {
            if (Interlocked.CompareExchange(ref _navigationInProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning(typeof(GameNavigationService),
                    $"[Navigation] Navigation already in progress. Ignoring core intent='{intent}'.");
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
                    $"[Navigation] Exception while navigating (core). intent='{intent}', reason='{reason ?? "<null>"}', ex={ex}");
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
                string message = $"[FATAL][Config] GameNavigationIntents invalid for core intent. intent='{intent}', intentId='<empty>'.";
                DebugUtility.LogError(typeof(GameNavigationService), message);
                throw new InvalidOperationException(message);
            }

            return intentId.Value;
        }

        private async Task ExecuteEntryAsync(string intentId, GameNavigationEntry entry, string reason)
        {
            TransitionStyleDefinition definition = ResolveStyle(entry);
            if (entry.RouteRef == null)
            {
                throw new InvalidOperationException($"[FATAL][Config] Navigation without direct routeRef. routeId='{entry.RouteId}'.");
            }

            SceneRouteDefinition routeDefinition = entry.RouteRef.ToDefinition();

            var request = new SceneTransitionRequest(
                routeDefinition,
                entry.RouteId,
                entry.StyleRef,
                entry.Payload ?? SceneTransitionPayload.Empty,
                definition.Profile,
                useFade: definition.UseFade,
                requestedBy: reason,
                reason: reason,
                resolvedRouteRef: entry.RouteRef);

            string signature = SceneTransitionSignature.Compute(SceneTransitionSignature.BuildContext(request));
            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][Navigation] DispatchIntent -> intentId='{intentId}', sceneRouteId='{entry.RouteId}', style='{request.StyleLabel}', reason='{reason ?? "<null>"}', signature='{signature}', UseFade={request.UseFade}, Profile='{request.TransitionProfileName}'.",
                DebugUtility.Colors.Info);

            await _sceneFlow.TransitionAsync(request);
        }

        private static void ValidateGameplayRouteOrFail(SceneRouteId routeId, GameNavigationEntry gameplayEntry, string reason)
        {
            if (gameplayEntry.RouteRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] Gameplay route validation requires direct routeRef. routeId='{routeId}' reason='{reason}'.");
            }

            if (gameplayEntry.RouteRef.RouteId != routeId)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] Gameplay routeId mismatch against direct routeRef. routeId='{routeId}' routeRefRouteId='{gameplayEntry.RouteRef.RouteId}' reason='{reason}'.");
            }

            if (gameplayEntry.RouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] Gameplay routeRef with invalid RouteKind. routeId='{routeId}' routeKind='{gameplayEntry.RouteRef.RouteKind}' reason='{reason}'.");
            }

            if (gameplayEntry.RouteRef.LevelCollection == null ||
                gameplayEntry.RouteRef.LevelCollection.Levels == null ||
                gameplayEntry.RouteRef.LevelCollection.Levels.Count == 0)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][Navigation] Gameplay route without LevelCollection. routeId='{routeId}' reason='{reason}'.");
            }
        }

        private static TransitionStyleDefinition ResolveStyle(GameNavigationEntry entry)
        {
            if (entry.StyleRef == null)
            {
                throw new InvalidOperationException($"[FATAL][Config] Navigation without TransitionStyleAsset direct. routeId='{entry.RouteId}'.");
            }

            return entry.StyleRef.ToDefinitionOrFail(nameof(GameNavigationService), $"routeId='{entry.RouteId}'");
        }
    }
}
