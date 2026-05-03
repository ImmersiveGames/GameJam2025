using System;
using System.Text;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionOperationalPipeline
    {
        private readonly SessionActivityMiniCatalog _catalog;
        private readonly _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline.SessionActivityPipeline _activityPipeline;
        private readonly SessionOperationalRuntimeState _state;
        private readonly string _sessionPipelineId;
        private readonly string _sessionStateId;
        private readonly string _routeId;
        private readonly string _routeProfileId;
        private readonly int _transitionSequence;

        public SessionOperationalPipeline(
            SessionActivityMiniCatalog catalog,
            _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline.SessionActivityPipeline activityPipeline,
            string sessionPipelineId,
            string sessionStateId,
            string routeId,
            string routeProfileId,
            int transitionSequence)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _activityPipeline = activityPipeline ?? throw new ArgumentNullException(nameof(activityPipeline));
            _state = new SessionOperationalRuntimeState();

            _sessionPipelineId = Normalize(sessionPipelineId);
            _sessionStateId = Normalize(sessionStateId);
            _routeId = Normalize(routeId);
            _routeProfileId = Normalize(routeProfileId);
            _transitionSequence = transitionSequence;

            if (string.IsNullOrWhiteSpace(_sessionPipelineId))
            {
                throw new ArgumentException("sessionPipelineId is required.", nameof(sessionPipelineId));
            }

            if (string.IsNullOrWhiteSpace(_sessionStateId))
            {
                throw new ArgumentException("sessionStateId is required.", nameof(sessionStateId));
            }

            if (string.IsNullOrWhiteSpace(_routeId))
            {
                throw new ArgumentException("routeId is required.", nameof(routeId));
            }

            if (string.IsNullOrWhiteSpace(_routeProfileId))
            {
                throw new ArgumentException("routeProfileId is required.", nameof(routeProfileId));
            }

            if (_transitionSequence <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(transitionSequence), "transitionSequence must be positive.");
            }

            if (!_catalog.TryGetFirst(out SessionActivityDefinition firstDefinition) || !firstDefinition.IsValid)
            {
                throw new InvalidOperationException("SessionActivityMiniCatalog requires Activity 01.");
            }
        }

        public SessionOperationalRuntimeState State => _state;
        public SessionActivityMiniCatalog Catalog => _catalog;
        public _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline.SessionActivityPipeline ActivityPipeline => _activityPipeline;

        public SessionOperationalResult StartEnvelopeAndActivity(string source, string reason)
        {
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);

            if (_state.HasStarted)
            {
                return Reject(normalizedSource, normalizedReason, "envelope_already_started");
            }

            if (!_catalog.TryGetFirst(out SessionActivityDefinition initialActivity) || !initialActivity.IsValid)
            {
                throw new InvalidOperationException("SessionOperationalPipeline requires Activity 01.");
            }

            _state.Reset(_sessionPipelineId, _sessionStateId, _routeId, _routeProfileId, _transitionSequence);
            _state.MarkStarted();

            SessionOperationalIdentity envelopeStartedIdentity = BuildIdentity(SessionOperationalStage.EnvelopeStarted, normalizedSource);
            EmitFact(
                SessionOperationalFactKind.EnvelopeStarted,
                envelopeStartedIdentity,
                normalizedSource,
                normalizedReason,
                "Envelope started.");
            EmitSnapshot(
                envelopeStartedIdentity,
                initialActivity,
                default,
                normalizedSource,
                normalizedReason,
                "Envelope started.");

            SessionOperationalIdentity curtainClosedIdentity = BuildIdentity(SessionOperationalStage.CurtainClosed, normalizedSource);
            EmitFact(
                SessionOperationalFactKind.CurtainClosed,
                curtainClosedIdentity,
                normalizedSource,
                normalizedReason,
                "Curtain closed.");
            EmitSnapshot(
                curtainClosedIdentity,
                initialActivity,
                default,
                normalizedSource,
                normalizedReason,
                "Curtain closed.");

            SessionOperationalIdentity setupExecutingIdentity = BuildIdentity(SessionOperationalStage.SessionOperationalSetupExecuting, normalizedSource);
            EmitFact(
                SessionOperationalFactKind.SessionOperationalSetupExecuting,
                setupExecutingIdentity,
                normalizedSource,
                normalizedReason,
                "Session operational setup executing.");
            EmitSnapshot(
                setupExecutingIdentity,
                initialActivity,
                default,
                normalizedSource,
                normalizedReason,
                "Session operational setup executing.");

            _state.SetInitialActivity(initialActivity);
            SessionOperationalIdentity selectedIdentity = BuildIdentity(SessionOperationalStage.InitialActivitySelected, normalizedSource);
            EmitFact(
                SessionOperationalFactKind.InitialActivitySelected,
                selectedIdentity,
                normalizedSource,
                normalizedReason,
                $"Initial activity selected: '{initialActivity.ActivityId}'.");
            EmitSnapshot(
                selectedIdentity,
                initialActivity,
                default,
                normalizedSource,
                normalizedReason,
                $"Initial activity selected: '{initialActivity.ActivityId}'.");

            SessionActivityEntryHandoff activityHandoff = new(
                initialActivity.ActivityId,
                initialActivity.ActivityOrdinal,
                entrySequence: 1,
                sessionStateId: _sessionStateId,
                source: nameof(SessionOperationalPipeline),
                reason: normalizedReason);
            _state.SetActivityEntryHandoff(activityHandoff);

            SessionOperationalIdentity handoffPreparedIdentity = BuildIdentity(SessionOperationalStage.SessionActivityEntryHandoffPrepared, normalizedSource);
            EmitFact(
                SessionOperationalFactKind.SessionActivityEntryHandoffPrepared,
                handoffPreparedIdentity,
                normalizedSource,
                normalizedReason,
                $"Session activity entry handoff prepared for '{initialActivity.ActivityId}'.");
            EmitSnapshot(
                handoffPreparedIdentity,
                initialActivity,
                activityHandoff,
                normalizedSource,
                normalizedReason,
                $"Session activity entry handoff prepared for '{initialActivity.ActivityId}'.");

            SessionOperationalIdentity readyIdentity = BuildIdentity(SessionOperationalStage.ReadyToOpenCurtain, normalizedSource);
            EmitFact(
                SessionOperationalFactKind.ReadyToOpenCurtain,
                readyIdentity,
                normalizedSource,
                normalizedReason,
                "Ready to open curtain.");
            EmitSnapshot(
                readyIdentity,
                initialActivity,
                activityHandoff,
                normalizedSource,
                normalizedReason,
                "Ready to open curtain.");

            SessionActivityCommandResult activityResult = _activityPipeline.StartFromPreparedHandoff(
                activityHandoff,
                nameof(SessionOperationalPipeline),
                normalizedReason);
            _state.SetActivityStartResult(activityResult);

            if (!activityResult.IsValid || activityResult.IsRejected)
            {
                HardFailFastH1.Trigger(typeof(SessionOperationalPipeline),
                    $"[FATAL][H1][SessionOperationalPipeline] Activity pipeline rejected prepared handoff. routeId='{_routeId}' routeProfileId='{_routeProfileId}' sessionStateId='{_sessionStateId}' result='{activityResult}' reason='{normalizedReason}'.");
            }

            SessionOperationalIdentity completedIdentity = BuildIdentity(SessionOperationalStage.Completed, normalizedSource);
            EmitFact(
                SessionOperationalFactKind.Completed,
                completedIdentity,
                normalizedSource,
                normalizedReason,
                "Session operational envelope completed.");
            EmitSnapshot(
                completedIdentity,
                initialActivity,
                activityHandoff,
                normalizedSource,
                normalizedReason,
                "Session operational envelope completed.");

            _state.MarkCompleted();

            SessionOperationalResult result = new(
                SessionOperationalResultKind.Completed,
                completedIdentity,
                activityHandoff,
                activityResult,
                _state.Facts,
                _state.Snapshots,
                normalizedReason);
            _state.SetResult(result);

            DebugUtility.Log<SessionOperationalPipeline>(
                $"[OBS][SessionOperationalPipeline] StartEnvelopeAndActivityCompleted sessionPipelineId='{_sessionPipelineId}' sessionStateId='{_sessionStateId}' routeId='{_routeId}' routeProfileId='{_routeProfileId}' transitionSequence='{_transitionSequence}' activityHandoff='{activityHandoff}' activityResult='{activityResult}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Success);

            return result;
        }

        public string DumpOperationalState()
        {
            StringBuilder builder = new();
            builder.AppendLine("[OBS][SessionOperationalPipeline] DumpState");
            builder.AppendLine($"sessionPipelineId='{_sessionPipelineId}' sessionStateId='{_sessionStateId}' routeId='{_routeId}' routeProfileId='{_routeProfileId}' transitionSequence='{_transitionSequence}'");
            builder.AppendLine($"started='{_state.HasStarted}' completed='{_state.HasCompleted}' stage='{_state.CurrentStage}'");
            builder.AppendLine($"currentIdentity='{_state.CurrentIdentity}'");
            builder.AppendLine($"initialActivity='{_state.InitialActivity}'");
            builder.AppendLine($"activityEntryHandoff='{_state.CurrentActivityEntryHandoff}'");
            builder.AppendLine($"activityStartResult='{_state.ActivityStartResult}'");
            builder.AppendLine("facts:");
            for (int index = 0; index < _state.Facts.Count; index++)
            {
                builder.AppendLine($"- {_state.Facts[index]}");
            }

            builder.AppendLine("snapshots:");
            for (int index = 0; index < _state.Snapshots.Count; index++)
            {
                builder.AppendLine($"- {_state.Snapshots[index]}");
            }

            builder.AppendLine("trace:");
            for (int index = 0; index < _state.Trace.Count; index++)
            {
                builder.AppendLine($"- {_state.Trace[index]}");
            }

            string dump = builder.ToString().TrimEnd();
            Debug.Log(dump);
            return dump;
        }

        private SessionOperationalIdentity BuildIdentity(SessionOperationalStage stage, string source)
        {
            return new SessionOperationalIdentity(
                _sessionPipelineId,
                _sessionStateId,
                _routeId,
                _routeProfileId,
                _transitionSequence,
                stage,
                source);
        }

        private void EmitFact(
            SessionOperationalFactKind kind,
            SessionOperationalIdentity identity,
            string source,
            string reason,
            string message)
        {
            SessionOperationalFact fact = new(kind, identity, source, reason, message, _state.CurrentActivityEntryHandoff);
            if (!fact.IsValid)
            {
                throw new InvalidOperationException($"Cannot emit invalid operational fact '{kind}'.");
            }

            _state.AppendFact(fact);
            _state.SetIdentity(identity, identity.Stage);
            _state.AppendTrace($"[OBS][SessionOperationalPipeline] fact='{fact.Kind}' stage='{fact.Identity.Stage}' transitionSequence='{fact.Identity.TransitionSequence}' sessionStateId='{fact.Identity.SessionStateId}' routeId='{fact.Identity.RouteId}' routeProfileId='{fact.Identity.RouteProfileId}' source='{fact.Source}' reason='{fact.Reason}' message='{fact.Message}' handoff='{fact.ActivityEntryHandoff}'");
        }

        private void EmitSnapshot(
            SessionOperationalIdentity identity,
            SessionActivityDefinition initialActivity,
            SessionActivityEntryHandoff activityEntryHandoff,
            string source,
            string reason,
            string message)
        {
            SessionOperationalSnapshot snapshot = new(identity, initialActivity, activityEntryHandoff, source, reason, message);
            if (!snapshot.IsValid)
            {
                throw new InvalidOperationException($"Cannot emit invalid operational snapshot '{identity.Stage}'.");
            }

            _state.AppendSnapshot(snapshot);
            _state.AppendTrace($"[OBS][SessionOperationalPipeline] snapshot='{identity.Stage}' identity='{snapshot.Identity}' initialActivity='{snapshot.InitialActivity}' source='{snapshot.Source}' reason='{snapshot.Reason}' message='{snapshot.Message}' handoff='{snapshot.ActivityEntryHandoff}'");
        }

        private SessionOperationalResult Reject(string source, string reason, string rejectionReason)
        {
            SessionOperationalIdentity identity = new(
                sessionPipelineId: _sessionPipelineId,
                sessionStateId: _sessionStateId,
                routeId: _routeId,
                routeProfileId: _routeProfileId,
                transitionSequence: _transitionSequence,
                stage: SessionOperationalStage.Unknown,
                source: source);

            return new SessionOperationalResult(
                SessionOperationalResultKind.Rejected,
                identity,
                default,
                default,
                Array.Empty<SessionOperationalFact>(),
                Array.Empty<SessionOperationalSnapshot>(),
                rejectionReason);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
