using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Result
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostRunResultService : IPostRunResultService
    {
        private readonly EventBinding<GameRunStartedEvent> _runStartedBinding;
        private readonly EventBinding<GameplayPhaseRuntimeMaterializedEvent> _phaseRuntimeMaterializedBinding;
        private bool _disposed;
        private bool _runStartedBindingRegistered;
        private bool _phaseRuntimeMaterializedBindingRegistered;
        private string _lastPhaseRuntimeClearIdentity = string.Empty;

        public PostRunResultService()
        {
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);
            _phaseRuntimeMaterializedBinding = new EventBinding<GameplayPhaseRuntimeMaterializedEvent>(OnGameplayPhaseRuntimeMaterialized);
            RegisterBindings();
        }

        public bool HasResult { get; private set; }
        public PostRunResult Result { get; private set; } = PostRunResult.None;
        public string Reason { get; private set; } = string.Empty;

        public void Clear(string reason = null)
        {
            HasResult = false;
            Result = PostRunResult.None;
            Reason = string.Empty;

            DebugUtility.LogVerbose<PostRunResultService>(
                $"[OBS][RunResultStage] RunResultCleared trigger='Manual' entrySignature='<none>' phaseRuntimeSignature='<none>' sessionSignature='<none>' source='manual' cleared='true' reason='{Normalize(reason)}'.");
        }

        // �nico ponto de consolida��o can�nica do resultado da run.
        public bool TrySetRunOutcome(GameRunOutcome outcome, string reason = null)
        {
            if (_disposed)
            {
                return false;
            }

            PostRunResult mapped = outcome switch
            {
                GameRunOutcome.Victory => PostRunResult.Victory,
                GameRunOutcome.Defeat => PostRunResult.Defeat,
                _ => PostRunResult.None,
            };

            if (mapped == PostRunResult.None)
            {
                return false;
            }

            return TrySet(mapped, reason, "RunOutcome");
        }

        public bool TrySetExit(string reason = null)
        {
            return TrySet(PostRunResult.Exit, reason, "Exit");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UnregisterBindings();
        }

        private bool TrySet(PostRunResult result, string reason, string source)
        {
            if (_disposed)
            {
                return false;
            }

            if (result == PostRunResult.None)
            {
                return false;
            }

            if (HasResult)
            {
                DebugUtility.LogVerbose<PostRunResultService>(
                    $"[OBS][RunResultStage] RunResultIgnored result='{result}' reason='{Normalize(reason)}' source='{source}' already='{Result}'.");
                return false;
            }

            HasResult = true;
            Result = result;
            Reason = Normalize(reason);

            DebugUtility.Log<PostRunResultService>(
                $"[OBS][RunResultStage] RunResultUpdated result='{Result}' reason='{Reason}' source='{source}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            ClearOperationalState(
                trigger: "GameRunStarted",
                entrySignature: "<none>",
                phaseRuntimeSignature: "<none>",
                sessionSignature: "<none>",
                source: nameof(GameRunStartedEvent),
                reason: "boundary_run_started",
                cleared: true);
        }

        private void OnGameplayPhaseRuntimeMaterialized(GameplayPhaseRuntimeMaterializedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            string entrySignature = Normalize(evt.EntrySignature);
            string phaseRuntimeSignature = Normalize(evt.Runtime.PhaseRuntimeSignature);
            string sessionSignature = Normalize(evt.Runtime.SessionContext.SessionSignature);
            string source = Normalize(evt.Source);

            if (!evt.Runtime.IsValid)
            {
                ClearOperationalState(
                    trigger: "PhaseRuntimeMaterialized",
                    entrySignature: entrySignature,
                    phaseRuntimeSignature: phaseRuntimeSignature,
                    sessionSignature: sessionSignature,
                    source: source,
                    reason: "invalid_phase_runtime_materialized_event",
                    cleared: false);
                return;
            }

            string dedupeKey = ResolveDedupeKey(entrySignature, phaseRuntimeSignature, sessionSignature);
            if (IsDuplicatePhaseRuntimeMaterialization(dedupeKey))
            {
                ClearOperationalState(
                    trigger: "PhaseRuntimeMaterialized",
                    entrySignature: entrySignature,
                    phaseRuntimeSignature: phaseRuntimeSignature,
                    sessionSignature: sessionSignature,
                    source: source,
                    reason: "duplicate_entry_signature",
                    cleared: false);
                return;
            }

            _lastPhaseRuntimeClearIdentity = dedupeKey;

            ClearOperationalState(
                trigger: "PhaseRuntimeMaterialized",
                entrySignature: entrySignature,
                phaseRuntimeSignature: phaseRuntimeSignature,
                sessionSignature: sessionSignature,
                source: source,
                reason: "new_phase_runtime_materialized",
                cleared: true);
        }

        private void RegisterBindings()
        {
            if (!_runStartedBindingRegistered)
            {
                EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
                _runStartedBindingRegistered = true;
            }

            if (!_phaseRuntimeMaterializedBindingRegistered)
            {
                EventBus<GameplayPhaseRuntimeMaterializedEvent>.Register(_phaseRuntimeMaterializedBinding);
                _phaseRuntimeMaterializedBindingRegistered = true;
            }
        }

        private void UnregisterBindings()
        {
            if (_runStartedBindingRegistered)
            {
                EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
                _runStartedBindingRegistered = false;
            }

            if (_phaseRuntimeMaterializedBindingRegistered)
            {
                EventBus<GameplayPhaseRuntimeMaterializedEvent>.Unregister(_phaseRuntimeMaterializedBinding);
                _phaseRuntimeMaterializedBindingRegistered = false;
            }
        }

        private void ClearOperationalState(
            string trigger,
            string entrySignature,
            string phaseRuntimeSignature,
            string sessionSignature,
            string source,
            string reason,
            bool cleared)
        {
            if (cleared)
            {
                HasResult = false;
                Result = PostRunResult.None;
                Reason = string.Empty;
            }

            DebugUtility.LogVerbose<PostRunResultService>(
                $"[OBS][RunResultStage] RunResultCleared trigger='{Normalize(trigger)}' entrySignature='{Normalize(entrySignature)}' phaseRuntimeSignature='{Normalize(phaseRuntimeSignature)}' sessionSignature='{Normalize(sessionSignature)}' source='{Normalize(source)}' cleared='{cleared.ToString().ToLowerInvariant()}' reason='{Normalize(reason)}'.");
        }

        private bool IsDuplicatePhaseRuntimeMaterialization(string dedupeKey)
        {
            if (string.IsNullOrWhiteSpace(dedupeKey))
            {
                return true;
            }

            return string.Equals(dedupeKey, _lastPhaseRuntimeClearIdentity, StringComparison.Ordinal);
        }

        private static string ResolveDedupeKey(string entrySignature, string phaseRuntimeSignature, string sessionSignature)
        {
            if (!string.IsNullOrWhiteSpace(entrySignature))
            {
                return entrySignature;
            }

            if (!string.IsNullOrWhiteSpace(phaseRuntimeSignature))
            {
                return phaseRuntimeSignature;
            }

            return sessionSignature;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}


