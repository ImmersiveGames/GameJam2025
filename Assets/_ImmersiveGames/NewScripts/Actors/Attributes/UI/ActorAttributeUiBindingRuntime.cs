using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    public sealed class ActorAttributeUiBindingRuntime
    {
        private readonly IActorAttributeEventStream _eventStream;
        private readonly IActorAttributeUiStateReader _stateReader;

        public ActorAttributeUiBindingRuntime(
            IActorAttributeEventStream eventStream,
            IActorAttributeUiStateReader stateReader)
        {
            _eventStream = eventStream;
            _stateReader = stateReader;
        }

        public ActorAttributeUiBindingResult Bind(
            ActorAttributeUiBindingTarget target,
            IActorAttributeUiSink sink,
            string source,
            string reason)
        {
            string normalizedSource = source.TrimToEmpty();
            string normalizedReason = reason.TrimToEmpty();

            if (!target.IsValid)
            {
                return Reject(
                    ActorAttributeUiBindingResultKind.RejectedInvalidTarget,
                    target,
                    sink,
                    normalizedSource,
                    normalizedReason,
                    string.IsNullOrWhiteSpace(target.InvalidReason) ? "invalid_target" : target.InvalidReason);
            }

            if (sink == null)
            {
                return Reject(
                    ActorAttributeUiBindingResultKind.RejectedSinkMissing,
                    target,
                    sink,
                    normalizedSource,
                    normalizedReason,
                    "sink_missing");
            }

            if (!sink.IsReady)
            {
                return Reject(
                    ActorAttributeUiBindingResultKind.RejectedSinkNotReady,
                    target,
                    sink,
                    normalizedSource,
                    normalizedReason,
                    "sink_not_ready");
            }

            if (_eventStream == null)
            {
                return Reject(
                    ActorAttributeUiBindingResultKind.RejectedStreamMissing,
                    target,
                    sink,
                    normalizedSource,
                    normalizedReason,
                    "stream_missing");
            }

            if (_stateReader == null)
            {
                return Reject(
                    ActorAttributeUiBindingResultKind.RejectedStateMissing,
                    target,
                    sink,
                    normalizedSource,
                    normalizedReason,
                    "state_reader_missing");
            }

            DebugUtility.LogVerbose(
                typeof(ActorAttributeUiBindingRuntime),
                $"event='ActorAttributeUiBindingStarted' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId}' attributeId='{target.AttributeId}' sinkType='{sink.GetType().Name}' source='{normalizedSource}' reason='{normalizedReason}'",
                DebugUtility.Colors.Info);

            if (!_stateReader.TryRead(target, out var readValue, out string failureReason))
            {
                return Reject(
                    ActorAttributeUiBindingResultKind.RejectedStateMissing,
                    target,
                    sink,
                    normalizedSource,
                    normalizedReason,
                    failureReason);
            }

            ActorAttributeUiValue initialValue = new(
                readValue.ActorId,
                readValue.ActorInstanceRuntimeId,
                readValue.AttributeId,
                readValue.CurrentValue,
                readValue.MinValue,
                readValue.MaxValue,
                readValue.HasNormalizedValue,
                readValue.NormalizedValue,
                normalizedSource,
                normalizedReason);
            sink.Apply(initialValue);

            DebugUtility.LogVerbose(
                typeof(ActorAttributeUiBindingRuntime),
                $"event='ActorAttributeUiInitialValueApplied' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId}' attributeId='{target.AttributeId}' sinkType='{sink.GetType().Name}' source='{normalizedSource}' reason='{normalizedReason}'",
                DebugUtility.Colors.Info);

            ActorAttributeUiBindingHandle handle = null;
            IDisposable subscription;
            try
            {
                subscription = _eventStream.Subscribe(
                    target.ActorInstanceRuntimeId,
                    target.AttributeId,
                    changedEvent =>
                    {
                        if (!changedEvent.IsValid)
                        {
                            DebugUtility.LogWarning(
                                typeof(ActorAttributeUiBindingRuntime),
                                $"event='ActorAttributeUiBindingRejected' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId}' attributeId='{target.AttributeId}' sinkType='{sink.GetType().Name}' source='{normalizedSource}' reason='{normalizedReason}' failureReason='changed_event_invalid'");
                            return;
                        }

                        handle?.ApplyChangedValue(changedEvent);
                    });
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(
                    typeof(ActorAttributeUiBindingRuntime),
                    $"event='ActorAttributeUiBindingRejected' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId}' attributeId='{target.AttributeId}' sinkType='{sink.GetType().Name}' source='{normalizedSource}' reason='{normalizedReason}' failureReason='subscription_failed' detail='{ex.GetType().Name}'");
                return ActorAttributeUiBindingResult.Rejected(
                    ActorAttributeUiBindingResultKind.RejectedSubscriptionFailed,
                    "subscription_failed");
            }

            if (subscription == null)
            {
                return Reject(
                    ActorAttributeUiBindingResultKind.RejectedSubscriptionFailed,
                    target,
                    sink,
                    normalizedSource,
                    normalizedReason,
                    "subscription_failed");
            }

            handle = new ActorAttributeUiBindingHandle(
                target,
                sink,
                subscription,
                sink.GetType().Name,
                normalizedSource,
                normalizedReason);

            DebugUtility.LogVerbose(
                typeof(ActorAttributeUiBindingRuntime),
                $"event='ActorAttributeUiBindingSubscribed' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId}' attributeId='{target.AttributeId}' sinkType='{sink.GetType().Name}' source='{normalizedSource}' reason='{normalizedReason}'",
                DebugUtility.Colors.Info);

            return ActorAttributeUiBindingResult.Bound(handle);
        }

        private static ActorAttributeUiBindingResult Reject(
            ActorAttributeUiBindingResultKind kind,
            ActorAttributeUiBindingTarget target,
            IActorAttributeUiSink sink,
            string source,
            string reason,
            string failureReason)
        {
            string normalizedFailureReason = string.IsNullOrWhiteSpace(failureReason) ? "binding_rejected" : failureReason;
            DebugUtility.LogWarning(
                typeof(ActorAttributeUiBindingRuntime),
                $"event='ActorAttributeUiBindingRejected' actorId='{target.ActorId}' actorInstanceRuntimeId='{target.ActorInstanceRuntimeId}' attributeId='{target.AttributeId}' sinkType='{sink?.GetType().Name ?? string.Empty}' source='{source}' reason='{reason}' failureReason='{normalizedFailureReason}'");

            return ActorAttributeUiBindingResult.Rejected(kind, normalizedFailureReason);
        }
    }
}
