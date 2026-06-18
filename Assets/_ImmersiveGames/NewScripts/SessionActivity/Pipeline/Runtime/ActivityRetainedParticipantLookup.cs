using System;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Actors.Semantic.Participation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using PlayerActivityParticipationContext = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipationContext;
using PlayerActivityParticipantBinding = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipantBinding;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal sealed class ActivityRetainedParticipantLookup : IActivityRetainedParticipantLookup
    {
        private readonly ActivityPlayerActorRegistry _playerActorRegistry;
        private readonly SessionActorRuntimeStore _sessionActorRuntimeStore;

        public ActivityRetainedParticipantLookup(
            ActivityPlayerActorRegistry playerActorRegistry,
            SessionActorRuntimeStore sessionActorRuntimeStore)
        {
            _playerActorRegistry = playerActorRegistry ?? throw new ArgumentNullException(nameof(playerActorRegistry));
            _sessionActorRuntimeStore = sessionActorRuntimeStore ?? throw new ArgumentNullException(nameof(sessionActorRuntimeStore));
        }

        public ActivityRetainedParticipantLookupResult Execute(ActivityRetainedParticipantLookupCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityRetainedParticipantLookupCommand is invalid.");
            }

            EmitLookupLog(
                "ActivityRetainedParticipantLookupStarted",
                command,
                ActivityRetainedParticipantLookupOutcomeKind.Unknown,
                ActivityRetainedParticipantLookupSourceKind.Unknown,
                "retained_lookup_started");

            if (command.ParticipantBinding is { RequiresPlayerActor: false, RequiresPlayerInput: false })
            {
                return BuildMissResult(command, "participant_binding_not_actor_or_input_relevant");
            }

            var retainedParticipationContext = command.RetainedParticipationContext;
            bool hasRetainedParticipationContext =
                retainedParticipationContext is { IsValid: true, Participants: { Count: > 0 } };
            bool hasMatchingRetainedBinding = hasRetainedParticipationContext &&
                TryFindMatchingRetainedBinding(command, retainedParticipationContext, out _);

            if (hasRetainedParticipationContext &&
                IsForeignCycle(retainedParticipationContext.SessionActivityIdentity, command.Identity))
            {
                return BuildRejectedResult(
                    command,
                    ActivityRetainedParticipantLookupOutcomeKind.RejectedForeign,
                    "retained_participation_context_foreign");
            }

            if (hasRetainedParticipationContext && !hasMatchingRetainedBinding)
            {
                return BuildRejectedResult(
                    command,
                    ActivityRetainedParticipantLookupOutcomeKind.RejectedStale,
                    "retained_participation_context_binding_mismatch");
            }

            if (command.ParticipantBinding.ActorScope == ActorScope.RouteScoped)
            {
                if (TryResolveRouteScopedHandle(command, out var retainedHandle))
                {
                    if (!IsCompatibleRetainedHandle(command, retainedHandle))
                    {
                        return BuildRejectedResult(
                            command,
                            ActivityRetainedParticipantLookupOutcomeKind.RejectedStale,
                            "retained_route_scoped_handle_mismatch");
                    }

                    return BuildResolvedResult(
                        command,
                        ActivityRetainedParticipantLookupSourceKind.RouteRegistry,
                        retainedHandle,
                        GetResolvedDetail(command, "retained_route_scoped_handle_resolved"));
                }

                if (hasMatchingRetainedBinding)
                {
                    return BuildRejectedResult(
                        command,
                        ActivityRetainedParticipantLookupOutcomeKind.RejectedStale,
                        "retained_route_scoped_handle_missing");
                }

                return BuildMissResult(command, "route_scoped_retained_handle_missing");
            }

            if (command.ParticipantBinding.ActorScope == ActorScope.SessionScoped)
            {
                if (TryResolveActiveHandle(command, out var activeHandle))
                {
                    if (!IsCompatibleRetainedHandle(command, activeHandle))
                    {
                        return BuildRejectedResult(
                            command,
                            ActivityRetainedParticipantLookupOutcomeKind.RejectedStale,
                            "retained_active_handle_mismatch");
                    }

                    return BuildResolvedResult(
                        command,
                        ActivityRetainedParticipantLookupSourceKind.ActiveRegistry,
                        activeHandle,
                        GetResolvedDetail(command, "retained_active_handle_resolved"));
                }

                if (TryResolveSessionScopedHandle(command, out var sessionScopedHandle))
                {
                    if (!IsCompatibleRetainedHandle(command, sessionScopedHandle))
                    {
                        return BuildRejectedResult(
                            command,
                            ActivityRetainedParticipantLookupOutcomeKind.RejectedStale,
                            "retained_session_scoped_handle_mismatch");
                    }

                    return BuildResolvedResult(
                        command,
                        ActivityRetainedParticipantLookupSourceKind.SessionActorStore,
                        sessionScopedHandle,
                        GetResolvedDetail(command, "retained_session_scoped_handle_resolved"));
                }

                if (hasMatchingRetainedBinding)
                {
                    return BuildRejectedResult(
                        command,
                        ActivityRetainedParticipantLookupOutcomeKind.RejectedStale,
                        "retained_session_scoped_handle_missing");
                }

                return BuildMissResult(command, "session_scoped_retained_handle_missing");
            }

            return BuildMissResult(command, "retained_actor_scope_unsupported");
        }

        private bool TryResolveActiveHandle(
            ActivityRetainedParticipantLookupCommand command,
            out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (!_playerActorRegistry.TryGetActiveHandleByParticipant(command.ParticipantBinding.ParticipantId, out handle) ||
                !handle.IsValid)
            {
                return false;
            }

            return true;
        }

        private bool TryResolveRouteScopedHandle(
            ActivityRetainedParticipantLookupCommand command,
            out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (!_playerActorRegistry.TryGetRouteScopedHandleByParticipant(command.ParticipantBinding.ParticipantId, out handle) ||
                !handle.IsValid)
            {
                return false;
            }

            return true;
        }

        private bool TryResolveSessionScopedHandle(
            ActivityRetainedParticipantLookupCommand command,
            out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (!_sessionActorRuntimeStore.TryGetByParticipantId(command.Identity, command.ParticipantBinding.ParticipantId, out var entry) ||
                !entry.IsValid)
            {
                return false;
            }

            var reboundIdentity = PlayerActorIdentityRecord.Create(command.Identity, command.ParticipantBinding);
            handle = new PlayerActorRuntimeHandle(reboundIdentity, entry.Instance, entry.Actor);
            return handle.IsValid;
        }

        private static bool TryFindMatchingRetainedBinding(
            ActivityRetainedParticipantLookupCommand command,
            PlayerActivityParticipationContext retainedParticipationContext,
            out PlayerActivityParticipantBinding retainedBinding)
        {
            retainedBinding = default;
            if (retainedParticipationContext == null ||
                !retainedParticipationContext.IsValid ||
                retainedParticipationContext.Participants == null)
            {
                return false;
            }

            for (int index = 0; index < retainedParticipationContext.Participants.Count; index++)
            {
                var candidate = retainedParticipationContext.Participants[index];
                if (!candidate.IsValid)
                {
                    continue;
                }

                if (candidate.ParticipantId != command.ParticipantBinding.ParticipantId ||
                    candidate.ActorId != command.ParticipantBinding.ActorId ||
                    candidate.ActorScope != command.ParticipantBinding.ActorScope ||
                    candidate.PlayerSlotId != command.ParticipantBinding.PlayerSlotId)
                {
                    continue;
                }

                retainedBinding = candidate;
                return true;
            }

            return false;
        }

        private static bool IsCompatibleRetainedHandle(
            ActivityRetainedParticipantLookupCommand command,
            PlayerActorRuntimeHandle retainedHandle)
        {
            if (!retainedHandle.IsValid)
            {
                return false;
            }

            return retainedHandle.ParticipantId == command.ParticipantBinding.ParticipantId &&
                retainedHandle.ActorId == command.ParticipantBinding.ActorId &&
                retainedHandle.ParticipantBinding.ActorScope == command.ParticipantBinding.ActorScope &&
                retainedHandle.PlayerSlotId == command.ParticipantBinding.PlayerSlotId;
        }

        private static string GetResolvedDetail(
            ActivityRetainedParticipantLookupCommand command,
            string defaultDetail)
        {
            if (command.RetainedParticipationContext is { IsValid: true })
            {
                int previousEntrySequence = command.RetainedParticipationContext.SessionActivityIdentity.EntrySequence;
                if (previousEntrySequence != command.Identity.EntrySequence)
                {
                    return $"retained_context_previous_entry_accepted previousEntrySequence='{previousEntrySequence}' currentEntrySequence='{command.Identity.EntrySequence}'";
                }
            }

            return defaultDetail;
        }

        private static ActivityRetainedParticipantLookupResult BuildResolvedResult(
            ActivityRetainedParticipantLookupCommand command,
            ActivityRetainedParticipantLookupSourceKind sourceKind,
            PlayerActorRuntimeHandle retainedHandle,
            string detail)
        {
            ActivityRetainedParticipantLookupResult result = new(
                command,
                ActivityRetainedParticipantLookupOutcomeKind.Resolved,
                sourceKind,
                command.ParticipantBinding,
                retainedHandle,
                detail);
            EmitLookupLog(
                "ActivityRetainedParticipantLookupResolved",
                command,
                result.OutcomeKind,
                result.SourceKind,
                detail);
            return result;
        }

        private static ActivityRetainedParticipantLookupResult BuildMissResult(
            ActivityRetainedParticipantLookupCommand command,
            string detail)
        {
            ActivityRetainedParticipantLookupResult result = new(
                command,
                ActivityRetainedParticipantLookupOutcomeKind.Missed,
                ActivityRetainedParticipantLookupSourceKind.None,
                default,
                default,
                detail);
            EmitLookupLog(
                "ActivityRetainedParticipantLookupMissed",
                command,
                result.OutcomeKind,
                result.SourceKind,
                detail);
            return result;
        }

        private static ActivityRetainedParticipantLookupResult BuildRejectedResult(
            ActivityRetainedParticipantLookupCommand command,
            ActivityRetainedParticipantLookupOutcomeKind outcomeKind,
            string detail)
        {
            ActivityRetainedParticipantLookupResult result = new(
                command,
                outcomeKind,
                ActivityRetainedParticipantLookupSourceKind.None,
                default,
                default,
                detail);
            string logEvent = outcomeKind == ActivityRetainedParticipantLookupOutcomeKind.RejectedForeign
                ? "ActivityRetainedParticipantLookupRejectedForeign"
                : "ActivityRetainedParticipantLookupRejectedStale";
            EmitLookupLog(
                logEvent,
                command,
                result.OutcomeKind,
                result.SourceKind,
                detail);
            return result;
        }

        private static void EmitLookupLog(
            string eventName,
            ActivityRetainedParticipantLookupCommand command,
            ActivityRetainedParticipantLookupOutcomeKind outcomeKind,
            ActivityRetainedParticipantLookupSourceKind sourceKind,
            string detail)
        {
            DebugUtility.LogVerbose(
                typeof(ActivityRetainedParticipantLookup),
                $"event='{eventName}' owner='ActivityRetainedParticipantLookup' pipelineId='{command.Identity.PipelineId}' sessionStateId='{command.Identity.SessionId}' activityId='{command.Identity.ActivityId}' entrySequence='{command.Identity.EntrySequence}' previousEntrySequence='{(command.RetainedParticipationContext is { IsValid: true } ? command.RetainedParticipationContext.SessionActivityIdentity.EntrySequence : 0)}' participantId='{command.ParticipantBinding.ParticipantId}' actorId='{command.ParticipantBinding.ActorId}' actorScope='{command.ParticipantBinding.ActorScope}' outcomeKind='{outcomeKind}' sourceKind='{sourceKind}' retainedContextState='{(command.RetainedParticipationContext is { IsValid: true } ? "present" : "absent")}' detail='{detail.TrimToEmpty()}' source='{command.Source.TrimToEmpty()}' reason='{command.Reason.TrimToEmpty()}'.",
                outcomeKind == ActivityRetainedParticipantLookupOutcomeKind.Resolved ? DebugUtility.Colors.Success : DebugUtility.Colors.Info);
        }

        private static bool IsForeignCycle(SessionActivityIdentity left, SessionActivityIdentity right)
        {
            return left.IsValid &&
                right.IsValid &&
                (!string.Equals(left.PipelineId, right.PipelineId, StringComparison.Ordinal) ||
                    !string.Equals(left.SessionId, right.SessionId, StringComparison.Ordinal));
        }
    }
}
