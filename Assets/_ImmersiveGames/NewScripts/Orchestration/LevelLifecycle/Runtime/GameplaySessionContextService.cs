using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplaySessionContextService : IGameplaySessionContextService, IDisposable
    {
        private readonly object _sync = new();
        private readonly EventBinding<LevelSelectedEvent> _levelSelectedBinding;
        private GameplaySessionContextSnapshot _current = GameplaySessionContextSnapshot.Empty;
        private GameplaySessionContextSnapshot _last = GameplaySessionContextSnapshot.Empty;
        private bool _disposed;

        public GameplaySessionContextService()
        {
            _levelSelectedBinding = new EventBinding<LevelSelectedEvent>(OnLevelSelected);
            EventBus<LevelSelectedEvent>.Register(_levelSelectedBinding);

            DebugUtility.LogVerbose<GameplaySessionContextService>(
                "[OBS][GameplaySessionFlow][SessionContext] GameplaySessionContextService registrado como owner do contexto da sessao.");
        }

        public GameplaySessionContextSnapshot Current
        {
            get
            {
                lock (_sync)
                {
                    return _current;
                }
            }
        }

        public GameplaySessionContextSnapshot UpdateFromLevelSelectedEvent(LevelSelectedEvent evt)
        {
            return Update(GameplaySessionContextSnapshot.FromLevelSelectedEvent(evt));
        }

        public GameplaySessionContextSnapshot Update(GameplaySessionContextSnapshot snapshot)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplaySessionContextService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid gameplay session context snapshot received.");
                }

                _current = snapshot;
                _last = snapshot;

                DebugUtility.Log<GameplaySessionContextService>(
                    $"[OBS][GameplaySessionFlow][SessionContext] SessionContextUpdated routeId='{snapshot.MacroRouteId}' routeRef='{snapshot.MacroRouteRef.name}' v='{snapshot.SelectionVersion}' reason='{snapshot.Reason}' signature='{snapshot.SessionSignature}'.",
                    DebugUtility.Colors.Info);

                return _current;
            }
        }

        public bool TryGetCurrent(out GameplaySessionContextSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _current;
                return _current.IsValid;
            }
        }

        public bool TryGetLast(out GameplaySessionContextSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _last;
                return _last.IsValid;
            }
        }

        public void Clear(string reason = null)
        {
            int lastSelectionVersion;

            lock (_sync)
            {
                _current = GameplaySessionContextSnapshot.Empty;
                lastSelectionVersion = _last.SelectionVersion;
            }

            DebugUtility.Log<GameplaySessionContextService>(
                $"[OBS][GameplaySessionFlow][SessionContext] SessionContextCleared keepLast='true' lastSelectionV='{lastSelectionVersion}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<LevelSelectedEvent>.Unregister(_levelSelectedBinding);
        }

        private void OnLevelSelected(LevelSelectedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            UpdateFromLevelSelectedEvent(evt);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<null>" : value.Trim();
        }
    }

    public readonly struct GameplayPhaseRuntimeSnapshot
    {
        public static GameplayPhaseRuntimeSnapshot FromLevelSelectedEvent(LevelSelectedEvent evt)
        {
            if (evt.LevelRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayPhaseRuntimeSnapshot),
                    "[FATAL][H1][GameplaySessionFlow] LevelSelectedEvent requires a valid levelRef to build the phase runtime snapshot.");
            }

            GameplaySessionContextSnapshot sessionContext = GameplaySessionContextSnapshot.FromLevelSelectedEvent(evt);
            LevelIntroStageSession levelSession = evt.LevelRef.CreateIntroStageSession(
                evt.LocalContentId,
                evt.Reason,
                evt.SelectionVersion,
                evt.LevelSignature);

            return new GameplayPhaseRuntimeSnapshot(sessionContext, levelSession);
        }

        public GameplayPhaseRuntimeSnapshot(
            GameplaySessionContextSnapshot sessionContext,
            LevelIntroStageSession levelSession)
        {
            SessionContext = sessionContext;
            LevelSession = levelSession;
            PhaseRuntimeSignature = BuildPhaseRuntimeSignature(sessionContext, levelSession);
        }

        public GameplaySessionContextSnapshot SessionContext { get; }
        public LevelIntroStageSession LevelSession { get; }
        public string PhaseRuntimeSignature { get; }

        public bool IsValid => SessionContext.IsValid && LevelSession.IsValid;
        public bool HasLevelRef => LevelSession.HasLevelRef;
        public bool HasPhaseRuntimeSignature => !string.IsNullOrWhiteSpace(PhaseRuntimeSignature);

        public static GameplayPhaseRuntimeSnapshot Empty => new(
            GameplaySessionContextSnapshot.Empty,
            LevelIntroStageSession.Empty);

        public override string ToString()
        {
            return $"sessionContext='{SessionContext}', levelSession='{LevelSession}', phaseRuntimeSignature='{(string.IsNullOrWhiteSpace(PhaseRuntimeSignature) ? "<none>" : PhaseRuntimeSignature)}'";
        }

        private static string BuildPhaseRuntimeSignature(
            GameplaySessionContextSnapshot sessionContext,
            LevelIntroStageSession levelSession)
        {
            string sessionSignature = sessionContext.HasSessionSignature ? sessionContext.SessionSignature : "<no-session>";
            string levelSignature = string.IsNullOrWhiteSpace(levelSession.LevelSignature) ? "<no-level>" : levelSession.LevelSignature;
            return $"{sessionSignature}|{levelSignature}";
        }
    }

    public interface IGameplayPhaseRuntimeService
    {
        GameplayPhaseRuntimeSnapshot Current { get; }
        bool TryGetCurrent(out GameplayPhaseRuntimeSnapshot snapshot);
        bool TryGetLast(out GameplayPhaseRuntimeSnapshot snapshot);
        GameplayPhaseRuntimeSnapshot Update(GameplayPhaseRuntimeSnapshot snapshot);
        GameplayPhaseRuntimeSnapshot UpdateFromLevelSelectedEvent(LevelSelectedEvent evt);
        void Clear(string reason = null);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayPhaseRuntimeService : IGameplayPhaseRuntimeService, IDisposable
    {
        private readonly object _sync = new();
        private readonly EventBinding<LevelSelectedEvent> _levelSelectedBinding;
        private GameplayPhaseRuntimeSnapshot _current = GameplayPhaseRuntimeSnapshot.Empty;
        private GameplayPhaseRuntimeSnapshot _last = GameplayPhaseRuntimeSnapshot.Empty;
        private bool _disposed;

        public GameplayPhaseRuntimeService()
        {
            _levelSelectedBinding = new EventBinding<LevelSelectedEvent>(OnLevelSelected);
            EventBus<LevelSelectedEvent>.Register(_levelSelectedBinding);

            DebugUtility.LogVerbose<GameplayPhaseRuntimeService>(
                "[OBS][GameplaySessionFlow][PhaseRuntime] GameplayPhaseRuntimeService registrado como owner do phase / level runtime.");
        }

        public GameplayPhaseRuntimeSnapshot Current
        {
            get
            {
                lock (_sync)
                {
                    return _current;
                }
            }
        }

        public GameplayPhaseRuntimeSnapshot UpdateFromLevelSelectedEvent(LevelSelectedEvent evt)
        {
            return Update(GameplayPhaseRuntimeSnapshot.FromLevelSelectedEvent(evt));
        }

        public GameplayPhaseRuntimeSnapshot Update(GameplayPhaseRuntimeSnapshot snapshot)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplayPhaseRuntimeService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid gameplay phase runtime snapshot received.");
                }

                _current = snapshot;
                _last = snapshot;

                DebugUtility.Log<GameplayPhaseRuntimeService>(
                    $"[OBS][GameplaySessionFlow][PhaseRuntime] PhaseRuntimeUpdated sessionSignature='{snapshot.SessionContext.SessionSignature}' levelRef='{snapshot.LevelSession.LevelRef.name}' selectionVersion='{snapshot.LevelSession.SelectionVersion}' levelSignature='{snapshot.LevelSession.LevelSignature}' phaseSignature='{snapshot.PhaseRuntimeSignature}'.",
                    DebugUtility.Colors.Info);

                return _current;
            }
        }

        public bool TryGetCurrent(out GameplayPhaseRuntimeSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _current;
                return _current.IsValid;
            }
        }

        public bool TryGetLast(out GameplayPhaseRuntimeSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _last;
                return _last.IsValid;
            }
        }

        public void Clear(string reason = null)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "<null>" : reason.Trim();
            string lastSignature;

            lock (_sync)
            {
                _current = GameplayPhaseRuntimeSnapshot.Empty;
                lastSignature = _last.PhaseRuntimeSignature;
            }

            DebugUtility.Log<GameplayPhaseRuntimeService>(
                $"[OBS][GameplaySessionFlow][PhaseRuntime] PhaseRuntimeCleared keepLast='true' lastPhaseSignature='{Normalize(lastSignature)}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<LevelSelectedEvent>.Unregister(_levelSelectedBinding);
        }

        private void OnLevelSelected(LevelSelectedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            UpdateFromLevelSelectedEvent(evt);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }
}
