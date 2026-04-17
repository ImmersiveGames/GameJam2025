using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.ResetInterop.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Interop;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Loading.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneComposition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SessionIntegration.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Policies;
using _ImmersiveGames.NewScripts.Infrastructure.InputModes.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Bootstrap
{
    /// <summary>
    /// Runtime composer do SceneFlow.
    ///
    /// Responsabilidade:
    /// - compor e ativar o runtime do SceneFlow depois que os installers relevantes concluíram;
    /// - nao registrar contratos de boot.
    /// </summary>
    public static class SceneFlowBootstrap
    {
        private static bool _runtimeComposed;
        private static SceneFlowInputModeBridge _inputModeBridge;
        private static GameplayParticipationInputModeBridge _participationInputModeBridge;
        private static LoadingHudOrchestrator _loadingHudOrchestrator;
        private static LoadingProgressOrchestrator _loadingProgressOrchestrator;

        public static void ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(SceneFlowBootstrap));

            if (_runtimeComposed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SceneFlow] BootstrapConfigAsset obrigatorio ausente para compor o runtime.");
            }

            EnsureSceneTransitionService();
            EnsureInputModeBridge();
            EnsureParticipationInputModeBridge();
            EnsureLoadingOrchestrators();
            EnsureFadeReadyAsync();
            EnsureSceneFlowModuleComposition();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(SceneFlowBootstrap),
                "[SceneFlow] Runtime composition concluida.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSceneTransitionService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var existing) && existing != null)
            {
                return;
            }

            var loaderAdapter = SceneFlowAdapterFactory.CreateLoaderAdapter();
            var fadeAdapter = SceneFlowAdapterFactory.CreateFadeAdapter(DependencyManager.Provider);
            var completionGate = ResolveOrComposeCompletionGate();
            var navigationPolicy = ResolveRequired<INavigationPolicy>();
            var routeGuard = ResolveRequired<IRouteGuard>();
            var routeResetPolicy = ResolveRequired<IRouteResetPolicy>();

            var service = new SceneTransitionService(
                loaderAdapter,
                fadeAdapter,
                completionGate,
                navigationPolicy,
                routeGuard,
                routeResetPolicy);

            DependencyManager.Provider.RegisterGlobal<ISceneTransitionService>(service);

            DebugUtility.LogVerbose(typeof(SceneFlowBootstrap),
                $"[SceneFlow] SceneTransitionService composto no runtime (Loader={loaderAdapter.GetType().Name}, FadeAdapter={fadeAdapter.GetType().Name}, Gate={completionGate.GetType().Name}, Policy={navigationPolicy.GetType().Name}, RouteGuard={routeGuard.GetType().Name}, RouteResetPolicy={routeResetPolicy.GetType().Name}).",
                DebugUtility.Colors.Info);
        }

        private static ISceneTransitionCompletionGate ResolveOrComposeCompletionGate()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionCompletionGate>(out var existingGate) && existingGate != null)
            {
                return existingGate;
            }

            var fallbackGate = new WorldResetCompletionGate(timeoutMs: 20000);
            DependencyManager.Provider.RegisterGlobal<ISceneTransitionCompletionGate>(fallbackGate, allowOverride: true);

            DebugUtility.LogVerbose(typeof(SceneFlowBootstrap),
                "[SceneFlow] Fallback ISceneTransitionCompletionGate composto via WorldResetCompletionGate.",
                DebugUtility.Colors.Info);

            return fallbackGate;
        }

        private static void EnsureInputModeBridge()
        {
            if (_inputModeBridge != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<SceneFlowInputModeBridge>(out var existing) && existing != null)
            {
                _inputModeBridge = existing;
                return;
            }

            _inputModeBridge = new SceneFlowInputModeBridge();
            DependencyManager.Provider.RegisterGlobal(_inputModeBridge);
        }

        private static void EnsureParticipationInputModeBridge()
        {
            if (_participationInputModeBridge != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<GameplayParticipationInputModeBridge>(out var existing) && existing != null)
            {
                _participationInputModeBridge = existing;
                return;
            }

            _participationInputModeBridge = new GameplayParticipationInputModeBridge();
            DependencyManager.Provider.RegisterGlobal(_participationInputModeBridge);
        }

        private static void EnsureLoadingOrchestrators()
        {
            ResolveRequired<ILoadingPresentationService>();
            ResolveRequired<ILoadingHudService>();

            if (_loadingHudOrchestrator == null)
            {
                if (DependencyManager.Provider.TryGetGlobal<LoadingHudOrchestrator>(out var existingHud) && existingHud != null)
                {
                    _loadingHudOrchestrator = existingHud;
                }
                else
                {
                    _loadingHudOrchestrator = new LoadingHudOrchestrator();
                    DependencyManager.Provider.RegisterGlobal(_loadingHudOrchestrator);
                }
            }

            if (_loadingProgressOrchestrator == null)
            {
                if (DependencyManager.Provider.TryGetGlobal<LoadingProgressOrchestrator>(out var existingProgress) && existingProgress != null)
                {
                    _loadingProgressOrchestrator = existingProgress;
                }
                else
                {
                    _loadingProgressOrchestrator = new LoadingProgressOrchestrator();
                    DependencyManager.Provider.RegisterGlobal(_loadingProgressOrchestrator);
                }
            }
        }

        private static async void EnsureFadeReadyAsync()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IFadeService>(out var fadeService) || fadeService == null)
            {
                return;
            }

            try
            {
                await fadeService.EnsureReadyAsync();
                DebugUtility.LogVerbose(typeof(SceneFlowBootstrap),
                    "[OBS][Fade] FadeScene ready (source=SceneFlowBootstrap/ComposeRuntime).",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(SceneFlowBootstrap),
                    $"[ERROR][Fade] Failed to preload FadeScene during SceneFlow runtime bootstrap. ex='{ex.GetType().Name}: {ex.Message}'");
            }
        }

        private static void EnsureSceneFlowModuleComposition()
        {
            ResolveRequired<ISceneTransitionService>();
            ResolveRequired<INavigationPolicy>();
            ResolveRequired<IRouteGuard>();
            ResolveRequired<IRouteResetPolicy>();
            ResolveRequired<ILoadingPresentationService>();
            ResolveRequired<ILoadingHudService>();
            ResolveRequired<IFadeService>();
            ResolveRequired<SceneFlowInputModeBridge>();
            ResolveRequired<GameplayParticipationInputModeBridge>();
            ResolveRequired<LoadingHudOrchestrator>();
            ResolveRequired<LoadingProgressOrchestrator>();

            DebugUtility.Log(typeof(SceneFlowBootstrap),
                "[OBS][SceneFlow] Runtime composition consolidada. scope='transition macro -> loading/fade -> navigation'.",
                DebugUtility.Colors.Info);
        }

        private static T ResolveRequired<T>() where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var service) && service != null)
            {
                return service;
            }

            throw new InvalidOperationException($"[FATAL][Config][SceneFlow] {typeof(T).Name} obrigatorio ausente no DI global antes da composicao runtime.");
        }
    }

    /// <summary>
    /// Thin bridge from semantic participation to concrete input mode requests.
    /// It does not own roster derivation or concrete binding state.
    /// </summary>
    public sealed class GameplayParticipationInputModeBridge : IDisposable
    {
        private readonly _ImmersiveGames.NewScripts.Core.Events.EventBinding<ParticipationSnapshotChangedEvent> _participationBinding;
        private bool _disposed;
        private string _lastProcessedSignature = string.Empty;

        public GameplayParticipationInputModeBridge()
        {
            _participationBinding = new _ImmersiveGames.NewScripts.Core.Events.EventBinding<ParticipationSnapshotChangedEvent>(OnParticipationChanged);
            _ImmersiveGames.NewScripts.Core.Events.EventBus<ParticipationSnapshotChangedEvent>.Register(_participationBinding);

            DebugUtility.LogVerbose<GameplayParticipationInputModeBridge>(
                "[InputMode] GameplayParticipationInputModeBridge registered as semantic bridge.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _ImmersiveGames.NewScripts.Core.Events.EventBus<ParticipationSnapshotChangedEvent>.Unregister(_participationBinding);
        }

        private void OnParticipationChanged(ParticipationSnapshotChangedEvent evt)
        {
            if (_disposed || !evt.IsValid)
            {
                return;
            }

            if (evt.IsCleared)
            {
                _lastProcessedSignature = string.Empty;
                DebugUtility.LogVerbose<GameplayParticipationInputModeBridge>(
                    $"[InputMode][Participation] Cleared event observed source='{evt.Source}' reason='{evt.Reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            ParticipationSnapshot snapshot = evt.Snapshot;
            string signature = snapshot.Signature.Value;

            if (!string.IsNullOrWhiteSpace(signature)
                && string.Equals(_lastProcessedSignature, signature, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<GameplayParticipationInputModeBridge>(
                    $"[InputMode][Participation] Duplicate snapshot ignored signature='{signature}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastProcessedSignature = signature;

            if (!snapshot.Readiness.CanEnterGameplay)
            {
                DebugUtility.LogVerbose<GameplayParticipationInputModeBridge>(
                    $"[InputMode][Participation] Snapshot not liberating input mode readinessState='{snapshot.Readiness.State}' signature='{signature}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!snapshot.TryGetLocalBindingCandidate(out ParticipantSnapshot localParticipant))
            {
                DebugUtility.LogVerbose<GameplayParticipationInputModeBridge>(
                    $"[InputMode][Participation] No local binding candidate found signature='{signature}' readinessState='{snapshot.Readiness.State}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            PublishGameplayInputModeRequest(snapshot, localParticipant, signature);

            DebugUtility.Log(typeof(GameplayParticipationInputModeBridge),
                $"[OBS][InputModes][Participation] Gameplay requested from participation signature='{signature}' localParticipantId='{localParticipant.ParticipantId}' bindingHint='{localParticipant.BindingHint}' ownership='{localParticipant.OwnershipKind}'.",
                DebugUtility.Colors.Info);
        }

        private static void PublishGameplayInputModeRequest(
            ParticipationSnapshot snapshot,
            ParticipantSnapshot localParticipant,
            string signature)
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISessionIntegrationContextService>(out var sessionIntegration) || sessionIntegration == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayParticipationInputModeBridge),
                    $"[FATAL][H1][SessionIntegration] ISessionIntegrationContextService indisponivel para InputMode gameplay. signature='{signature}' readinessState='{snapshot.Readiness.State}'.");
                return;
            }

            sessionIntegration.RequestGameplayInputMode(
                BuildReason(snapshot, localParticipant),
                "GameplayParticipation",
                signature);
        }

        private static string BuildReason(ParticipationSnapshot snapshot, ParticipantSnapshot participant)
        {
            return $"Participation/{snapshot.Readiness.State}/local={participant.ParticipantId}";
        }
    }
}
