using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Attributes.UI;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryActorAttributeUiBindingStage
    {
        public static ActivityEntryActorAttributeUiBindingResult Execute(
            SessionActivityIdentity identity,
            string source,
            string reason,
            IActorAttributeEventStream eventStream,
            ActivityPlayerActorRegistry registry,
            ActivityParticipationContext participationContext,
            ActivityActorExitRuntimeState runtimeState,
            IActorAttributeUiBindingRequestProvider requestProvider,
            ActivityActorAttributeUiBindingRuntimeState bindingRuntimeState)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("SessionActivityIdentity is invalid for attribute UI binding.");
            }

            eventStream = eventStream ?? throw new ArgumentNullException(nameof(eventStream));
            registry = registry ?? throw new ArgumentNullException(nameof(registry));
            runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            requestProvider = requestProvider ?? throw new ArgumentNullException(nameof(requestProvider));
            bindingRuntimeState = bindingRuntimeState ?? throw new ArgumentNullException(nameof(bindingRuntimeState));

            IReadOnlyList<ActorAttributeUiBindingRequestEntry> requestEntries = requestProvider.GetRequests() ?? Array.Empty<ActorAttributeUiBindingRequestEntry>();
            string normalizedSource = source.TrimToEmpty();
            string normalizedReason = reason.TrimToEmpty();

            DebugUtility.LogVerbose(
                typeof(ActivityEntryActorAttributeUiBindingStage),
                $"event='ActivityEntryActorAttributeUiBindingStarted' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' requestCount='{requestEntries.Count}' source='{normalizedSource}' reason='{normalizedReason}'",
                DebugUtility.Colors.Info);

            if (requestEntries.Count == 0)
            {
                DebugUtility.LogVerbose(
                    typeof(ActivityEntryActorAttributeUiBindingStage),
                    $"event='ActivityEntryActorAttributeUiBindingSkipped' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' reason='no_binding_requests' source='{normalizedSource}' reasonDetail='{normalizedReason}'",
                    DebugUtility.Colors.Info);

                return new ActivityEntryActorAttributeUiBindingResult(
                    0,
                    0,
                    0,
                    bindingRuntimeState.ActiveHandleCount,
                    true,
                    "no_binding_requests");
            }

            ActivityEntryActorAttributeUiTargetResolver targetResolver = new();
            int resolvedCount = 0;
            int boundCount = 0;

            try
            {
                for (int index = 0; index < requestEntries.Count; index++)
                {
                    var entry = requestEntries[index];
                    if (!entry.IsValid)
                    {
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeUiBindingStage][ActorAttributeUiBinding] Invalid binding request index='{index}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' reason='{entry.GetInvalidReason()}'.");
                    }

                    var targetResolveResult = targetResolver.Resolve(
                        entry.Request,
                        participationContext,
                        registry,
                        normalizedSource,
                        normalizedReason);
                    if (!targetResolveResult.IsResolved)
                    {
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeUiBindingStage][ActorAttributeUiBinding] Target resolve rejected activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' selectorKind='{targetResolveResult.SelectorKind}' failureReason='{targetResolveResult.FailureReason}'.");
                    }

                    resolvedCount += 1;

                    if (!runtimeState.TryGetActiveActorAttributeCapability(targetResolveResult.ActorInstanceRuntimeId, out var capabilityState) ||
                        !capabilityState.IsValid ||
                        capabilityState.Endpoint == null)
                    {
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeUiBindingStage][ActorAttributeUiBinding] Missing active actor attribute capability activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' actorInstanceRuntimeId='{targetResolveResult.ActorInstanceRuntimeId}' attributeId='{targetResolveResult.AttributeId}'.");
                    }

                    ActorAttributeEndpointUiStateReader stateReader = new(capabilityState.Endpoint);
                    ActorAttributeUiBindingRuntime bindingRuntime = new(eventStream, stateReader);
                    var bindingResult = bindingRuntime.Bind(
                        targetResolveResult.Target,
                        entry.Sink,
                        normalizedSource,
                        normalizedReason);

                    if (!bindingResult.IsBound)
                    {
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeUiBindingStage][ActorAttributeUiBinding] Binding rejected activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' actorInstanceRuntimeId='{targetResolveResult.ActorInstanceRuntimeId}' attributeId='{targetResolveResult.AttributeId}' reason='{bindingResult.FailureReason}'.");
                    }

                    bindingRuntimeState.Store(bindingResult.Handle);
                    boundCount += 1;

                    DebugUtility.LogVerbose(
                        typeof(ActivityEntryActorAttributeUiBindingStage),
                        $"event='ActivityEntryActorAttributeUiBindingApplied' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' actorId='{targetResolveResult.ActorId}' actorInstanceRuntimeId='{targetResolveResult.ActorInstanceRuntimeId}' attributeId='{targetResolveResult.AttributeId}' sinkType='{entry.Sink.GetType().Name}' activeHandleCount='{bindingRuntimeState.ActiveHandleCount}' source='{normalizedSource}' reason='{normalizedReason}'",
                        DebugUtility.Colors.Success);
                }
            }
            catch
            {
                bindingRuntimeState.ClearAll();
                throw;
            }

            return new ActivityEntryActorAttributeUiBindingResult(
                requestEntries.Count,
                resolvedCount,
                boundCount,
                bindingRuntimeState.ActiveHandleCount,
                false,
                "bindings_applied");
        }
    }
}
