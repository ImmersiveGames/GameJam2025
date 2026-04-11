#nullable enable
using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage
{
    /// <summary>
    /// Servico global que controla o termino da IntroStage via comando explicito.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageControlService : IIntroStageControlService
    {
        private enum IntroStageExecutionState
        {
            Idle,
            Beginning,
            WaitingConfirm,
            Completed,
            Skipped,
            Superseded,
            Cancelled
        }

        private readonly object _sync = new();
        private bool _isActive;
        private IntroStageContext _activeContext;
        private IntroStageExecutionState _state = IntroStageExecutionState.Idle;

        public bool IsIntroStageActive
        {
            get
            {
                lock (_sync)
                {
                    return _isActive;
                }
            }
        }

        public void BeginIntroStage(IntroStageContext context)
        {
            bool hadActiveContext;
            IntroStageContext previousContext = default;

            lock (_sync)
            {
                hadActiveContext = _isActive;
                if (hadActiveContext)
                {
                    previousContext = _activeContext;
                    _state = IntroStageExecutionState.Superseded;
                }

                _isActive = true;
                _activeContext = context;
                _state = IntroStageExecutionState.Beginning;
                _state = IntroStageExecutionState.WaitingConfirm;
            }

                if (hadActiveContext && previousContext.IsValid)
                {
                    DebugUtility.LogWarning<IntroStageControlService>(
                        $"[OBS][IntroStageControlService] BeginIntroStage chamado enquanto outra IntroStage ainda esta ativa. Intro antiga sera superseded signature='{NormalizeValue(previousContext.ContextSignature)}'.");

                EventBus<IntroStageCompletedEvent>.Raise(new IntroStageCompletedEvent(
                    previousContext.Session,
                    "GameplaySessionFlow",
                    wasSkipped: true,
                    "superseded"));
            }
        }

        public void CompleteIntroStage(string reason)
        {
            FinishIntroStage(reason, wasSkipped: false);
        }

        public void SkipIntroStage(string reason)
        {
            FinishIntroStage(reason, wasSkipped: true);
        }

        public void MarkSessionClosed()
        {
            lock (_sync)
            {
                if (!_isActive)
                {
                    return;
                }

                if (_state == IntroStageExecutionState.WaitingConfirm)
                {
                    _state = IntroStageExecutionState.Cancelled;
                }
            }
        }

        private void FinishIntroStage(string reason, bool wasSkipped)
        {
            try
            {
                IntroStageContext context = default;
                bool wasActive;
                IntroStageExecutionState previousState;

                lock (_sync)
                {
                    wasActive = _isActive;
                    previousState = _state;

                    if (_isActive)
                    {
                        context = _activeContext;
                        _isActive = false;
                        _state = wasSkipped ? IntroStageExecutionState.Skipped : IntroStageExecutionState.Completed;
                    }
                }

                string normalizedReason = NormalizeValue(reason);
                string actionName = wasSkipped ? "SkipIntroStage" : "CompleteIntroStage";
                string gameLoopState = NormalizeValue(ResolveGameLoopStateName());
                var logContext = BuildSafeLogContext(context);
                string signature = logContext.Signature;
                string routeKind = logContext.RouteKind;
                string targetScene = logContext.TargetScene;

                if (!wasActive)
                {
                    DebugUtility.Log<IntroStageControlService>(
                        $"[OBS][IntroStageControlService] {actionName} received reason='{normalizedReason}' skip={wasSkipped.ToString().ToLowerInvariant()} decision='ignored' ignoreReason='not_active' state='{gameLoopState}' executionState='{previousState}' isActive=false signature='{signature}' routeKind='{routeKind}' target='{targetScene}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                DebugUtility.Log<IntroStageControlService>(
                    $"[OBS][IntroStageControlService] {actionName} received reason='{normalizedReason}' skip={wasSkipped.ToString().ToLowerInvariant()} decision='applied' state='{gameLoopState}' executionState='{_state}' isActive=true signature='{signature}' routeKind='{routeKind}' target='{targetScene}'.",
                    DebugUtility.Colors.Info);

                if (string.Equals(normalizedReason, "timeout", StringComparison.OrdinalIgnoreCase))
                {
                    DebugUtility.LogWarning<IntroStageControlService>(
                        $"[OBS][IntroStageControlService] IntroStageTimedOut signature='{signature}' routeKind='{routeKind}' target='{targetScene}'.");
                }

                if (context.IsValid)
                {
                    DebugUtility.Log<IntroStageControlService>(
                        $"[OBS][IntroStageControlService] IntroStageCompletedPublished source='GameplaySessionFlow' handshake='GameLoop.RequestStart' signature='{signature}' routeKind='{routeKind}' target='{targetScene}' skipped={wasSkipped.ToString().ToLowerInvariant()} reason='{normalizedReason}'.",
                        DebugUtility.Colors.Info);

                    EventBus<IntroStageCompletedEvent>.Raise(new IntroStageCompletedEvent(
                        context.Session,
                        "GameplaySessionFlow",
                        wasSkipped,
                        normalizedReason));
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<IntroStageControlService>(
                    $"[OBS][IntroStageController] FinishIntroStage FAILED ex='{ex.GetType().Name}: {ex.Message}'.");
                throw;
            }
        }

        private static string NormalizeValue(string? value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private static string ResolveGameLoopStateName()
        {
            return DependencyManager.Provider != null
                   && DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoop)
                   && gameLoop != null
                ? gameLoop.CurrentStateIdName
                : "<none>";
        }

        private static IntroStageLogContext BuildSafeLogContext(IntroStageContext context)
        {
            try
            {
                return new IntroStageLogContext(
                    NormalizeValue(context.ContextSignature),
                    NormalizeValue(context.RouteKind.ToString()),
                    NormalizeValue(context.TargetScene));
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<IntroStageControlService>(
                    $"[OBS][IntroStageController] Failed to read IntroStageContext for logging. ex='{ex.GetType().Name}: {ex.Message}'.");
                return IntroStageLogContext.Fallback;
            }
        }

        private readonly struct IntroStageLogContext
        {
            public static readonly IntroStageLogContext Fallback = new("<error>", "<error>", "<error>");

            public string Signature { get; }
            public string RouteKind { get; }
            public string TargetScene { get; }

            public IntroStageLogContext(string signature, string routeKind, string targetScene)
            {
                Signature = signature;
                RouteKind = routeKind;
                TargetScene = targetScene;
            }
        }
    }
}

