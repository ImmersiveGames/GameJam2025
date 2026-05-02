using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
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

        public async Task StartGameplayRouteAsync(SceneRouteId routeId, SceneTransitionPayload payload, string reason = null)
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
            SceneTransitionPayload normalizedPayload = ValidateGameplayRoutePayloadOrFail(routeId, payload, normalizedReason);
            ValidateGameplayRouteOrFail(routeId, gameplayEntry, normalizedReason);

            DebugUtility.Log(typeof(GameNavigationService),
                "[OBS][NavigationCore][Operational] StartGameplayRouteAsync dispatched using canonical route payload and style resolution.",
                DebugUtility.Colors.Info);

            var routeEntry = new GameNavigationEntry(routeId, gameplayEntry.StyleRef, normalizedPayload, gameplayEntry.RouteRef);
            TransitionStyleDefinition definition = ResolveStyle(routeEntry);

            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][NavigationCore][Operational] StartGameplayRouteRequested routeId='{routeId}', reason='{normalizedReason}', style='{routeEntry.StyleLabel}', profile='{definition.ProfileLabel}', profileAsset='{(definition.Profile != null ? definition.Profile.name : "<null>")}' gameplayEntryKind='{routeEntry.Payload.GameplayEntryKind}'.",
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

        public async Task NavigateToRoute(SceneRouteId routeId, string reason = null)
        {
            if (!routeId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][NavigationCore] NavigateToRoute with invalid routeId. routeId='{routeId}' reason='{reason ?? "<null>"}'.");
            }

            if (Interlocked.CompareExchange(ref _navigationInProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning(typeof(GameNavigationService),
                    $"[NavigationCore][Operational] Navigation already in progress. Ignoring routeId='{routeId}'.");
                return;
            }

            try
            {
                if (!_catalog.TryGet(routeId.Value, out GameNavigationEntry entry) || !entry.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameNavigationService),
                        $"[FATAL][H1][NavigationCore] Missing explicit route entry. routeId='{routeId}' reason='{reason ?? "<null>"}'.");
                }

                if (entry.RouteRef == null)
                {
                    HardFailFastH1.Trigger(typeof(GameNavigationService),
                        $"[FATAL][H1][NavigationCore] Route entry without direct routeRef. routeId='{routeId}' reason='{reason ?? "<null>"}'.");
                }

                string normalizedReason = string.IsNullOrWhiteSpace(reason) ? $"Navigation/Route:{routeId}" : reason.Trim();
                SceneRouteDefinition routeDefinition = entry.RouteRef.ToDefinition();
                SceneTransitionPayload payload = routeDefinition.RouteKind == SceneRouteKind.Gameplay
                    ? SceneTransitionPayload.GameplayInitialEntry
                    : SceneTransitionPayload.Empty;
                TransitionStyleDefinition definition = ResolveStyle(entry);

                var request = new SceneTransitionRequest(
                    routeDefinition,
                    routeId,
                    entry.StyleRef,
                    payload,
                    definition.Profile,
                    useFade: definition.UseFade,
                    requestedBy: normalizedReason,
                    reason: normalizedReason,
                    resolvedRouteRef: entry.RouteRef);

                string signature = SceneTransitionSignature.Compute(SceneTransitionSignature.BuildContext(request));
                DebugUtility.Log(typeof(GameNavigationService),
                    $"[OBS][NavigationCore] DispatchRoute -> routeId='{routeId}', style='{request.StyleLabel}', reason='{normalizedReason}', signature='{signature}', routeKind='{routeDefinition.RouteKind}', routeProfileId='{routeDefinition.RouteProfile.ProfileId}', routeProfileAsset='{entry.RouteRef.RouteProfile.name}'.",
                    DebugUtility.Colors.Info);

                await _sceneFlow.TransitionAsync(request);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GameNavigationService),
                    $"[NavigationCore][Operational] Exception while navigating route. routeId='{routeId}', reason='{reason ?? "<null>"}', ex={ex}");
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
                throw new InvalidOperationException($"[FATAL][Config][NavigationCore] Missing core intent entry '{intentId}'.");
                }

                if (intent == GameNavigationIntentKind.Gameplay)
                {
                    entry = CreateGameplayInitialEntry(entry, intentId, reason);
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
            SceneTransitionPayload dispatchPayload = entry.RouteRef.RouteKind == SceneRouteKind.Gameplay
                ? ValidateGameplayDispatchPayloadOrFail(entry, intentId, reason)
                : entry.Payload ?? SceneTransitionPayload.Empty;

            var request = new SceneTransitionRequest(
                routeDefinition,
                entry.RouteId,
                entry.StyleRef,
                dispatchPayload,
                definition.Profile,
                useFade: definition.UseFade,
                requestedBy: reason,
                reason: reason,
                resolvedRouteRef: entry.RouteRef);

            string signature = SceneTransitionSignature.Compute(SceneTransitionSignature.BuildContext(request));
            DebugUtility.Log(typeof(GameNavigationService),
                $"[OBS][NavigationCore] DispatchIntent -> intentId='{intentId}', sceneRouteId='{entry.RouteId}', style='{request.StyleLabel}', reason='{reason ?? "<null>"}', signature='{signature}', gameplayEntryKind='{request.Payload.GameplayEntryKind}', UseFade={request.UseFade}, Profile='{request.TransitionProfileName}', routeProfileId='{routeDefinition.RouteProfile.ProfileId}', routeProfileAsset='{entry.RouteRef.RouteProfile.name}'.",
                DebugUtility.Colors.Info);

            await _sceneFlow.TransitionAsync(request);
        }


        private static GameNavigationEntry CreateGameplayInitialEntry(GameNavigationEntry entry, string intentId, string reason)
        {
            if (entry.RouteRef == null || entry.RouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][NavigationCore] Initial gameplay entry requires direct Gameplay routeRef. intentId='{intentId}' routeId='{entry.RouteId}' reason='{reason ?? "<null>"}'.");
            }

            return new GameNavigationEntry(
                entry.RouteId,
                entry.StyleRef,
                SceneTransitionPayload.GameplayInitialEntry,
                entry.RouteRef);
        }

        private static SceneTransitionPayload ValidateGameplayRoutePayloadOrFail(SceneRouteId routeId, SceneTransitionPayload payload, string reason)
        {
            SceneTransitionPayload normalizedPayload = payload ?? SceneTransitionPayload.Empty;
            if (normalizedPayload.GameplayEntryKind == SceneTransitionGameplayEntryKind.None)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][NavigationCore] StartGameplayRouteAsync requires explicit gameplay entry payload. routeId='{routeId}' reason='{reason}'.");
            }

            if (!normalizedPayload.IsGameplayReentry)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][NavigationCore] StartGameplayRouteAsync is reserved for gameplay reentry. routeId='{routeId}' gameplayEntryKind='{normalizedPayload.GameplayEntryKind}' reason='{reason}'.");
            }

            return normalizedPayload;
        }

        private static SceneTransitionPayload ValidateGameplayDispatchPayloadOrFail(GameNavigationEntry entry, string intentId, string reason)
        {
            SceneTransitionPayload payload = entry.Payload ?? SceneTransitionPayload.Empty;
            if (payload.GameplayEntryKind == SceneTransitionGameplayEntryKind.None)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][NavigationCore] Gameplay dispatch requires explicit gameplay entry payload. intentId='{intentId}' routeId='{entry.RouteId}' reason='{reason ?? "<null>"}'.");
            }

            if (!payload.IsGameplayInitialEntry && !payload.IsGameplayReentry)
            {
                HardFailFastH1.Trigger(typeof(GameNavigationService),
                    $"[FATAL][H1][NavigationCore] Gameplay dispatch received unsupported entry kind. intentId='{intentId}' routeId='{entry.RouteId}' gameplayEntryKind='{payload.GameplayEntryKind}' reason='{reason ?? "<null>"}'.");
            }

            return payload;
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

    public sealed class SessionIntegrationNavigationHandoffService : ISessionIntegrationNavigationHandoffService
    {
        private readonly IGameNavigationService _navigationService;

        public SessionIntegrationNavigationHandoffService(IGameNavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        }

        public SceneRouteId ResolveGameplayRouteIdOrFail(string reason, string source)
        {
            string normalizedReason = NormalizeReason(reason);
            string normalizedSource = NormalizeSource(source);

            SceneRouteId routeId = _navigationService.ResolveGameplayRouteIdOrFail();
            if (!routeId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionIntegrationNavigationHandoffService),
                    $"[FATAL][H1][SessionIntegration] Navigation handoff returned invalid gameplay routeId. source='{normalizedSource}' reason='{normalizedReason}'.");
            }

            DebugUtility.Log<SessionIntegrationNavigationHandoffService>(
                $"[OBS][SessionIntegration][Handoff] GameplayRouteResolved target='Navigation' source='{normalizedSource}' routeId='{routeId}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            return routeId;
        }

        public async Task RequestStartGameplayRouteAsync(
            SceneRouteId routeId,
            string reason,
            string source,
            CancellationToken ct = default)
        {
            string normalizedReason = NormalizeReason(reason);
            string normalizedSource = NormalizeSource(source);

            if (!routeId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionIntegrationNavigationHandoffService),
                    $"[FATAL][H1][SessionIntegration] Navigation handoff received invalid gameplay routeId. source='{normalizedSource}' reason='{normalizedReason}'.");
            }

            ct.ThrowIfCancellationRequested();

            DebugUtility.Log<SessionIntegrationNavigationHandoffService>(
                $"[OBS][SessionIntegration][Handoff] StartGameplayRouteAccepted target='Navigation' source='{normalizedSource}' routeId='{routeId}' gameplayEntryKind='Reentry' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            await _navigationService.StartGameplayRouteAsync(routeId, SceneTransitionPayload.GameplayReentry, normalizedReason);

            DebugUtility.Log<SessionIntegrationNavigationHandoffService>(
                $"[OBS][SessionIntegration][Handoff] StartGameplayRouteCompleted target='Navigation' source='{normalizedSource}' routeId='{routeId}' gameplayEntryKind='Reentry' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        public async Task RequestExitToMenuAsync(string reason, string source, CancellationToken ct = default)
        {
            string normalizedReason = NormalizeReason(reason);
            string normalizedSource = NormalizeSource(source);

            ct.ThrowIfCancellationRequested();

            DebugUtility.Log<SessionIntegrationNavigationHandoffService>(
                $"[OBS][SessionIntegration][Handoff] ExitToMenuAccepted target='Navigation' source='{normalizedSource}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            await _navigationService.GoToMenuAsync(normalizedReason);

            DebugUtility.Log<SessionIntegrationNavigationHandoffService>(
                $"[OBS][SessionIntegration][Handoff] ExitToMenuCompleted target='Navigation' source='{normalizedSource}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        private static string NormalizeSource(string source)
        {
            return string.IsNullOrWhiteSpace(source) ? "<none>" : source.Trim();
        }
    }
}

