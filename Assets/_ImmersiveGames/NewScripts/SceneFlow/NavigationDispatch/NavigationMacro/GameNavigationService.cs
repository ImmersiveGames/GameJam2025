using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro
{
    /// <summary>
    /// Servico operacional de navigation.
    /// Resolve intents core e faz dispatch atraves do SceneFlow.
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
                $"[OBS][NavigationCore][Operational] GameNavigationService initialized. Entries: [{string.Join(", ", _catalog.RouteIds)}]",
                DebugUtility.Colors.Info);
        }

        public Task GoToMenuAsync(string reason = null)
        {
            DebugUtility.LogVerbose(typeof(GameNavigationService),
                $"[OBS][NavigationCore][Operational] GoToMenuRequested reason='{reason ?? "<null>"}'.",
                DebugUtility.Colors.Info);
            return NavigateAsync(GameNavigationIntentKind.Menu, reason);
        }

        public SceneRouteId ResolveGameplayRouteIdOrFail()
        {
            if (!TryResolveCoreEntry(GameNavigationIntentKind.Gameplay, out var gameplayEntry) || !gameplayEntry.IsValid || gameplayEntry.RouteRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][NavigationCore] Missing gameplay intent entry while resolving canonical gameplay route. intentId='{GetCoreIntentId(GameNavigationIntentKind.Gameplay)}'.");
            }

            return gameplayEntry.RouteId;
        }

        public async Task StartGameplayRouteAsync(SceneRouteId routeId, SceneTransitionPayload payload = null, string reason = null)
        {
            if (!routeId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][NavigationCore] StartGameplayRouteAsync with invalid routeId. routeId='{routeId}' reason='{reason ?? "<null>"}'.");
            }

            if (!TryResolveCoreEntry(GameNavigationIntentKind.Gameplay, out var gameplayEntry) || !gameplayEntry.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][NavigationCore] Missing gameplay intent entry for StartGameplayRouteAsync. intentId='{GetCoreIntentId(GameNavigationIntentKind.Gameplay)}'.");
            }

            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "Navigation/StartGameplayRoute" : reason.Trim();
            ValidateGameplayRouteOrFail(routeId, gameplayEntry, normalizedReason);

            DebugUtility.Log(typeof(GameNavigationService),
                "[OBS][NavigationCore][Operational] StartGameplayRouteAsync dispatched using canonical phase catalog runtime state; GameplaySessionFlow consome pendingTarget/currentCommitted no prepare phase-side.",
                DebugUtility.Colors.Info);

            var routeEntry = new GameNavigationEntry(routeId, gameplayEntry.StyleRef, payload ?? SceneTransitionPayload.Empty, gameplayEntry.RouteRef);
            TransitionStyleDefinition definition = ResolveStyle(routeEntry);

            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][NavigationCore][Operational] StartGameplayRouteRequested routeId='{routeId}', reason='{normalizedReason}', style='{routeEntry.StyleLabel}', profile='{definition.ProfileLabel}', profileAsset='{(definition.Profile != null ? definition.Profile.name : "<null>")}'.",
                DebugUtility.Colors.Info);

            await ExecuteEntryAsync(GetCoreIntentId(GameNavigationIntentKind.Gameplay), routeEntry, normalizedReason);
        }

        public Task NavigateAsync(GameNavigationIntentKind intent, string reason = null)
        {
            if (Interlocked.CompareExchange(ref _navigationInProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning(typeof(GameNavigationService),
                    $"[NavigationCore][Operational] Navigation already in progress. Ignoring core intent='{intent}'.");
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
                throw new InvalidOperationException($"[FATAL][Config][NavigationCore] Missing core intent entry '{intentId}'.");
                }

                await ExecuteEntryAsync(intentId, entry, reason);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GameNavigationService),
                    $"[NavigationCore][Operational] Exception while navigating (core). intent='{intent}', reason='{reason ?? "<null>"}', ex={ex}");
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
                string message = $"[FATAL][Config][NavigationCore] GameNavigationIntents invalid for core intent. intent='{intent}', intentId='<empty>'.";
                DebugUtility.LogError(typeof(GameNavigationService), message);
                throw new InvalidOperationException(message);
            }

            return intentId.Value;
        }

        private async Task ExecuteEntryAsync(string intentId, GameNavigationEntry entry, string reason)
        {
            ValidateOfficialGameplayPhaseCatalogOrFail(intentId, entry, reason);

            TransitionStyleDefinition definition = ResolveStyle(entry);
            if (entry.RouteRef == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][NavigationCore] Navigation without direct routeRef. routeId='{entry.RouteId}'.");
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
                $"[OBS][NavigationCore] DispatchIntent -> intentId='{intentId}', sceneRouteId='{entry.RouteId}', style='{request.StyleLabel}', reason='{reason ?? "<null>"}', signature='{signature}', UseFade={request.UseFade}, Profile='{request.TransitionProfileName}'.",
                DebugUtility.Colors.Info);

            await _sceneFlow.TransitionAsync(request);
        }

        private void ValidateOfficialGameplayPhaseCatalogOrFail(string intentId, GameNavigationEntry entry, string reason)
        {
            if (entry.RouteRef == null || entry.RouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                return;
            }

            if (_catalog is not GameNavigationCatalogAsset catalogAsset)
            {
                return;
            }

            SceneRouteDefinitionAsset canonicalGameplayRouteRef = catalogAsset.ResolveGameplayRouteRefOrFail();
            if (!entry.RouteId.IsValid || entry.RouteId != canonicalGameplayRouteRef.RouteId)
            {
                return;
            }

            if (entry.RouteRef.PhaseDefinitionCatalog != null)
            {
                DebugUtility.LogVerbose(typeof(GameNavigationService),
                    $"[OBS][NavigationCore][Operational] Official gameplay phase catalog validated routeId='{entry.RouteId}' routeKind='{entry.RouteRef.RouteKind}' phaseCatalogPresent=True reason='{reason ?? "<null>"}' intentId='{intentId}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            HardFailFastH1.Trigger(typeof(GameNavigationService),
                $"[FATAL][H1][NavigationCore] Official gameplay requires PhaseDefinitionCatalog before gameplay entry. routeId='{entry.RouteId}' routeKind='{entry.RouteRef.RouteKind}' intentId='{intentId}' reason='{reason ?? "<null>"}' detail='canonical gameplay route without phaseDefinitionCatalog'.");
        }

        private static void ValidateGameplayRouteOrFail(SceneRouteId routeId, GameNavigationEntry gameplayEntry, string reason)
        {
            if (gameplayEntry.RouteRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][NavigationCore] Gameplay route validation requires direct routeRef. routeId='{routeId}' reason='{reason}'.");
            }

            if (gameplayEntry.RouteRef.RouteId != routeId)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][NavigationCore] Gameplay routeId mismatch against direct routeRef. routeId='{routeId}' routeRefRouteId='{gameplayEntry.RouteRef.RouteId}' reason='{reason}'.");
            }

            if (gameplayEntry.RouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][NavigationCore] Gameplay routeRef with invalid RouteKind. routeId='{routeId}' routeKind='{gameplayEntry.RouteRef.RouteKind}' reason='{reason}'.");
            }

        }

        private static TransitionStyleDefinition ResolveStyle(GameNavigationEntry entry)
        {
            if (entry.StyleRef == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][NavigationCore] Navigation without TransitionStyleAsset direct. routeId='{entry.RouteId}'.");
            }

            return entry.StyleRef.ToDefinitionOrFail(nameof(GameNavigationService), $"routeId='{entry.RouteId}'");
        }
    }
}

