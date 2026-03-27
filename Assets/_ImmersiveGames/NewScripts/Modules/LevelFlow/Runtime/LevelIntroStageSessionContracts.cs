#nullable enable
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public enum LevelIntroStageDisposition
    {
        NoIntro = 0,
        HasIntro = 1
    }

    public readonly struct LevelIntroStageSession
    {
        public LevelIntroStageSession(
            LevelDefinitionAsset levelRef,
            SceneRouteId macroRouteId,
            SceneRouteDefinitionAsset macroRouteRef,
            string localContentId,
            string reason,
            int selectionVersion,
            string levelSignature,
            GameObject? presenterPrefab,
            LevelIntroStageDisposition disposition)
        {
            LevelRef = levelRef;
            MacroRouteId = macroRouteId;
            MacroRouteRef = macroRouteRef;
            LocalContentId = string.IsNullOrWhiteSpace(localContentId) ? string.Empty : localContentId.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? string.Empty : levelSignature.Trim();
            PresenterPrefab = presenterPrefab;
            Disposition = disposition;
        }

        public LevelDefinitionAsset LevelRef { get; }
        public SceneRouteId MacroRouteId { get; }
        public SceneRouteDefinitionAsset MacroRouteRef { get; }
        public string LocalContentId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string LevelSignature { get; }
        public GameObject? PresenterPrefab { get; }
        public LevelIntroStageDisposition Disposition { get; }

        public bool HasIntroStage => Disposition == LevelIntroStageDisposition.HasIntro;
        public bool HasLevelRef => LevelRef != null;
        public bool HasPresenterPrefab => PresenterPrefab != null;
        public bool IsValid => HasLevelRef && MacroRouteId.IsValid && MacroRouteRef != null;

        public static LevelIntroStageSession Empty => default;
    }

    public readonly struct LevelEnteredEvent : IEvent
    {
        public LevelEnteredEvent(LevelIntroStageSession session, string source)
        {
            Session = session;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public LevelIntroStageSession Session { get; }
        public string Source { get; }
    }

    public readonly struct LevelIntroCompletedEvent : IEvent
    {
        public LevelIntroCompletedEvent(LevelIntroStageSession session, string source, bool wasSkipped, string reason)
        {
            Session = session;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            WasSkipped = wasSkipped;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public LevelIntroStageSession Session { get; }
        public string Source { get; }
        public bool WasSkipped { get; }
        public string Reason { get; }
    }

    public interface ILevelIntroStageSessionService
    {
        bool TryGetCurrentSession(out LevelIntroStageSession session);
    }

    public interface ILevelIntroStagePresenterRegistry
    {
        bool TryGetCurrentPresenter(out ILevelIntroStagePresenter presenter);
        bool TryEnsureCurrentPresenter(LevelIntroStageSession session, string source, out ILevelIntroStagePresenter presenter);
        void Register(ILevelIntroStagePresenter presenter, string sessionSignature);
        void Unregister(ILevelIntroStagePresenter presenter);
    }

    public interface ILevelIntroStagePresenterScopeResolver
    {
        bool TryResolvePresenters(LevelIntroStageSession session, out IReadOnlyList<ILevelIntroStagePresenter> presenters);
    }

    public interface ILevelIntroStagePresenter
    {
        string PresenterSignature { get; }
        bool IsReady { get; }
        void BindToSession(string sessionSignature);
    }

    public sealed class LevelIntroStageSessionService : ILevelIntroStageSessionService, System.IDisposable
    {
        private readonly object _sync = new();
        private readonly EventBinding<LevelSelectedEvent> _levelSelectedBinding;
        private LevelIntroStageSession _currentSession;
        private bool _disposed;

        public LevelIntroStageSessionService()
        {
            _levelSelectedBinding = new EventBinding<LevelSelectedEvent>(OnLevelSelected);
            EventBus<LevelSelectedEvent>.Register(_levelSelectedBinding);

            DebugUtility.LogVerbose<LevelIntroStageSessionService>(
                "[OBS][LevelFlow] LevelIntroStageSessionService registrado (LevelSelectedEvent -> intro session canonica).",
                DebugUtility.Colors.Info);
        }

        public bool TryGetCurrentSession(out LevelIntroStageSession session)
        {
            lock (_sync)
            {
                session = _currentSession;
                return _currentSession.IsValid;
            }
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
            LevelDefinitionAsset levelRef = evt.LevelRef;
            SceneRouteDefinitionAsset macroRouteRef = evt.MacroRouteRef;
            string levelName = levelRef != null ? levelRef.name : "<none>";

            if (levelRef == null)
            {
                HardFailFastH1.Trigger(typeof(LevelIntroStageSessionService),
                    "[FATAL][H1][LevelFlow] LevelSelectedEvent sem levelRef ao materializar intro session.");
            }

            if (macroRouteRef == null)
            {
                HardFailFastH1.Trigger(typeof(LevelIntroStageSessionService),
                    $"[FATAL][H1][LevelFlow] LevelSelectedEvent sem macroRouteRef ao materializar intro session. levelRef='{levelName}' routeId='{evt.MacroRouteId}'.");
            }

            bool hasIntro = levelRef != null && levelRef.HasIntroStage;
            var session = new LevelIntroStageSession(
                levelRef!,
                evt.MacroRouteId,
                macroRouteRef!,
                evt.LocalContentId,
                evt.Reason,
                evt.SelectionVersion,
                evt.LevelSignature,
                levelRef != null ? levelRef.IntroPresenterPrefab : null,
                hasIntro ? LevelIntroStageDisposition.HasIntro : LevelIntroStageDisposition.NoIntro);

            lock (_sync)
            {
                _currentSession = session;
            }

            DebugUtility.Log<LevelIntroStageSessionService>(
                $"[OBS][LevelFlow] LevelIntroStageSessionUpdated levelRef='{levelName}' routeId='{evt.MacroRouteId}' disposition='{session.Disposition}' v='{session.SelectionVersion}' signature='{session.LevelSignature}' reason='{session.Reason}'.",
                DebugUtility.Colors.Info);
        }
    }
}
