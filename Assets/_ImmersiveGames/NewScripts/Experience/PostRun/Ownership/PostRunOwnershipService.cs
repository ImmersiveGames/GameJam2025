using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Infrastructure.InputModes.Runtime;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Ownership
{
    public readonly struct PostRunOwnershipContext
    {
        public string Signature { get; }
        public string SceneName { get; }
        public string Profile { get; }
        public int Frame { get; }
        public PostRunResult Result { get; }
        public string Reason { get; }

        public PostRunOwnershipContext(string signature, string sceneName, string profile, int frame, PostRunResult result, string reason)
        {
            Signature = signature;
            SceneName = sceneName;
            Profile = profile;
            Frame = frame;
            Result = result;
            Reason = reason;
        }
    }

    public readonly struct PostRunOwnershipExitContext
    {
        public string Signature { get; }
        public string SceneName { get; }
        public string Profile { get; }
        public int Frame { get; }
        public string Reason { get; }
        public string NextState { get; }
        public PostRunResult Result { get; }

        public PostRunOwnershipExitContext(string signature, string sceneName, string profile, int frame, string reason, string nextState, PostRunResult result)
        {
            Signature = signature;
            SceneName = sceneName;
            Profile = profile;
            Frame = frame;
            Reason = reason;
            NextState = nextState;
            Result = result;
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostRunOwnershipService : IPostRunOwnershipService
    {
        private const string PostRunGateToken = "state.postrun";

        private bool _isPostRunActive;
        private bool _isRunDecisionActive;
        private bool _postRunCompleted;
        private bool _loggedMissingGate;
        private IDisposable _gateHandle;

        public bool IsOwnerEnabled => true;
        public bool IsActive => _isPostRunActive || _isRunDecisionActive;
        public bool IsRunDecisionActive => _isRunDecisionActive;

        public void OnPostRunEntered(PostRunOwnershipContext context)
        {
            if (_isPostRunActive)
            {
                return;
            }

            _isPostRunActive = true;
            _postRunCompleted = false;
            DebugUtility.Log<PostRunOwnershipService>(
                $"[OBS][GameplaySessionFlow][PostRun] PostRunEntered downstreamFrom='RunOutcome' signature='{context.Signature}' scene='{context.SceneName}' frame={context.Frame} result='{context.Result}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info);
            AcquireGate();
            EventBus<PostRunEnteredEvent>.Raise(new PostRunEnteredEvent(context));
        }

        public void OnPostRunCompleted(PostRunOwnershipContext context)
        {
            if (!_isPostRunActive)
            {
                HardFailFastH1.Trigger(typeof(PostRunOwnershipService),
                    $"[FATAL][H1][GameplaySessionFlow] PostRunCompleted recebido antes de PostRun entrar. signature='{context.Signature}' scene='{context.SceneName}' frame={context.Frame} result='{context.Result}' reason='{context.Reason}'.");
            }

            _postRunCompleted = true;

            DebugUtility.Log<PostRunOwnershipService>(
                $"[OBS][GameplaySessionFlow][PostRun] PostRunCompleted acknowledged signature='{context.Signature}' scene='{context.SceneName}' frame={context.Frame} result='{context.Result}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info);

            ClearPhaseRuntimeAxesOrFail(context);
            EventBus<PostRunCompletedEvent>.Raise(new PostRunCompletedEvent(context));
        }

        public void OnRunDecisionEntered(PostRunOwnershipContext context)
        {
            if (!_postRunCompleted)
            {
                HardFailFastH1.Trigger(typeof(PostRunOwnershipService),
                    $"[FATAL][H1][GameplaySessionFlow] RunDecisionEntered antes de PostRunCompleted. signature='{context.Signature}' scene='{context.SceneName}' frame={context.Frame} result='{context.Result}' reason='{context.Reason}'.");
            }

            if (_isRunDecisionActive)
            {
                return;
            }

            _isRunDecisionActive = true;

            DebugUtility.Log<PostRunOwnershipService>(
                $"[OBS][GameplaySessionFlow][RunDecision] RunDecisionEntered downstreamFrom='PostRun' signature='{context.Signature}' scene='{context.SceneName}' frame={context.Frame} result='{context.Result}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info);
            ApplyRunDecisionInputMode(context);
            EventBus<RunDecisionEnteredEvent>.Raise(new RunDecisionEnteredEvent(context));
        }

        public void OnPostRunExited(PostRunOwnershipExitContext context)
        {
            if (!_isPostRunActive && !_isRunDecisionActive)
            {
                return;
            }

            _isPostRunActive = false;
            _isRunDecisionActive = false;
            _postRunCompleted = false;
            DebugUtility.Log<PostRunOwnershipService>(
                $"[OBS][GameplaySessionFlow][RunDecision] RunDecisionExited downstreamTo='{context.NextState}' signature='{context.Signature}' scene='{context.SceneName}' frame={context.Frame} result='{context.Result}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info);
            ReleaseGate(context.Reason);
            ApplyExitInputMode(context);
            EventBus<PostRunExitedEvent>.Raise(new PostRunExitedEvent(context));
        }

        private static void ApplyRunDecisionInputMode(PostRunOwnershipContext context)
        {
            DebugUtility.Log<PostRunOwnershipService>(
                $"[OBS][GameplaySessionFlow][InputMode] Request mode='FrontendMenu' map='UI' phase='RunDecision' reason='RunDecision/Entered' signature='{context.Signature}' scene='{context.SceneName}' profile='{context.Profile}' frame={context.Frame} result='{context.Result}'.",
                DebugUtility.Colors.Info);

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(InputModeRequestKind.FrontendMenu, "RunDecision/Entered", "RunDecision", context.Signature));
        }

        private void ApplyExitInputMode(PostRunOwnershipExitContext context)
        {
            bool applyGameplay = context.NextState == "Playing";
            string modeName = applyGameplay ? "Gameplay" : "FrontendMenu";
            string mapName = applyGameplay ? "Player" : "UI";
            string reason = $"RunDecision/{context.Reason}";

            DebugUtility.Log<PostRunOwnershipService>(
                $"[OBS][GameplaySessionFlow][InputMode] Request mode='{modeName}' map='{mapName}' phase='RunDecisionExit' reason='{reason}' signature='{context.Signature}' scene='{context.SceneName}' profile='{context.Profile}' frame={context.Frame} result='{context.Result}'.",
                DebugUtility.Colors.Info);

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(applyGameplay ? InputModeRequestKind.Gameplay : InputModeRequestKind.FrontendMenu, reason, "RunDecision", context.Signature));
        }

        private void AcquireGate()
        {
            if (_gateHandle != null)
            {
                return;
            }

            var gateService = ResolveGateService();
            if (gateService == null)
            {
                if (_loggedMissingGate)
                {
                    return;
                }

                _loggedMissingGate = true;
                DebugUtility.LogWarning<PostRunOwnershipService>("[OBS][GameplaySessionFlow][RunDecision] ISimulationGateService indisponivel. Gate nao sera adquirido.");
                return;
            }

            _gateHandle = gateService.Acquire(PostRunGateToken);
            DebugUtility.Log<PostRunOwnershipService>($"[OBS][GameplaySessionFlow][PostRun] Gate adquirido token='{PostRunGateToken}'.", DebugUtility.Colors.Info);
        }

        private void ReleaseGate(string reason)
        {
            if (_gateHandle == null)
            {
                return;
            }

            try
            {
                _gateHandle.Dispose();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PostRunOwnershipService>($"[OBS][GameplaySessionFlow][RunDecision] Falha ao liberar gate ({reason}): {ex}");
            }

            _gateHandle = null;
            DebugUtility.Log<PostRunOwnershipService>($"[OBS][GameplaySessionFlow][PostRun] Gate liberado token='{PostRunGateToken}'.", DebugUtility.Colors.Info);
        }

        private static void ClearPhaseRuntimeAxesOrFail(PostRunOwnershipContext context)
        {
            DebugUtility.Log<PostRunOwnershipService>(
                $"[OBS][GameplaySessionFlow][PostRun] PhaseRuntimeAxesCleanupStarted signature='{context.Signature}' scene='{context.SceneName}' frame={context.Frame} result='{context.Result}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info);

            ClearPhaseAxisOrFail<IGameplayPhaseRulesObjectivesService>(
                context,
                "RulesObjectives",
                service => service.Clear(context.Reason));

            ClearPhaseAxisOrFail<IGameplayPhaseInitialStateService>(
                context,
                "InitialState",
                service => service.Clear(context.Reason));

            DebugUtility.Log<PostRunOwnershipService>(
                $"[OBS][GameplaySessionFlow][PostRun] PhaseRuntimeAxesCleanupCompleted signature='{context.Signature}' scene='{context.SceneName}' frame={context.Frame} result='{context.Result}' reason='{context.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void ClearPhaseAxisOrFail<TService>(
            PostRunOwnershipContext context,
            string axisName,
            Action<TService> clearAction)
            where TService : class
        {
            if (!DependencyManager.Provider.TryGetGlobal<TService>(out var service) || service == null)
            {
                HardFailFastH1.Trigger(typeof(PostRunOwnershipService),
                    $"[FATAL][H1][GameplaySessionFlow] PostRunCompleted requires {axisName} service to be registered before phase cleanup. signature='{context.Signature}' scene='{context.SceneName}' frame={context.Frame} result='{context.Result}' reason='{context.Reason}'.");
            }

            clearAction(service);
        }

        private static ISimulationGateService ResolveGateService()
        {
            return DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var service)
                ? service
                : null;
        }
    }
}

