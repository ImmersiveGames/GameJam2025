using System;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    public sealed class ActorAttributeUiBindingHandle : IDisposable
    {
        private readonly ActorAttributeUiBindingTarget _target;
        private readonly string _sinkType;
        private readonly string _source;
        private readonly string _reason;
        private IDisposable _subscription;
        private IActorAttributeUiSink _sink;
        private bool _disposed;

        internal ActorAttributeUiBindingHandle(
            ActorAttributeUiBindingTarget target,
            IActorAttributeUiSink sink,
            IDisposable subscription,
            string sinkType,
            string source,
            string reason)
        {
            _target = target;
            _sink = sink;
            _subscription = subscription;
            _sinkType = sinkType ?? string.Empty;
            _source = Normalize(source);
            _reason = Normalize(reason);
        }

        public bool IsDisposed => _disposed;

        internal void ApplyChangedValue(ActorAttributeChangedEvent changedEvent)
        {
            if (_disposed || _sink == null)
            {
                return;
            }

            ActorAttributeUiValue value = new(
                changedEvent.ActorId,
                changedEvent.ActorInstanceRuntimeId,
                changedEvent.AttributeId,
                changedEvent.CurrentValue,
                changedEvent.MinValue,
                changedEvent.MaxValue,
                changedEvent.HasNormalizedValue,
                changedEvent.HasNormalizedValue ? changedEvent.NormalizedValue : 0f,
                changedEvent.Source,
                changedEvent.Reason);

            _sink.Apply(value);

            DebugUtility.LogVerbose(
                typeof(ActorAttributeUiBindingHandle),
                $"event='ActorAttributeUiBindingEventApplied' actorId='{_target.ActorId}' actorInstanceRuntimeId='{_target.ActorInstanceRuntimeId}' attributeId='{_target.AttributeId}' sinkType='{_sinkType}' source='{changedEvent.Source}' reason='{changedEvent.Reason}'",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_sink != null)
            {
                try
                {
                    _sink.Clear(_reason);
                }
                catch (Exception exception)
                {
                    DebugUtility.LogWarning(
                        typeof(ActorAttributeUiBindingHandle),
                        $"event='ActorAttributeUiBindingClearFailed' actorId='{_target.ActorId}' actorInstanceRuntimeId='{_target.ActorInstanceRuntimeId}' attributeId='{_target.AttributeId}' sinkType='{_sinkType}' source='{_source}' reason='{_reason}' detail='{exception.GetType().Name}'");
                }
            }

            if (_subscription != null)
            {
                _subscription.Dispose();
                _subscription = null;
            }

            _sink = null;

            DebugUtility.LogVerbose(
                typeof(ActorAttributeUiBindingHandle),
                $"event='ActorAttributeUiBindingDisposed' actorId='{_target.ActorId}' actorInstanceRuntimeId='{_target.ActorInstanceRuntimeId}' attributeId='{_target.AttributeId}' sinkType='{_sinkType}' source='{_source}' reason='{_reason}'",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
