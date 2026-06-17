using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    public sealed class ActorAttributeEventStream : IActorAttributeEventStream
    {
        public void Publish(ActorAttributeChangedEvent changedEvent)
        {
            if (!changedEvent.IsValid)
            {
                throw new InvalidOperationException("ActorAttributeChangedEvent is invalid.");
            }

            FilteredEventBus<ActorInstanceRuntimeId, ActorAttributeChangedEvent>.Raise(
                changedEvent.ActorInstanceRuntimeId,
                changedEvent);

            DebugUtility.LogVerbose(
                typeof(ActorAttributeEventStream),
                $"event='ActorAttributeChangedEventPublished' actorId='{changedEvent.ActorId}' actorInstanceRuntimeId='{changedEvent.ActorInstanceRuntimeId}' attributeId='{changedEvent.AttributeId}' operation='{changedEvent.Operation}' previousValue={changedEvent.PreviousValue} currentValue={changedEvent.CurrentValue} minValue={changedEvent.MinValue} maxValue={changedEvent.MaxValue} hasNormalizedValue={changedEvent.HasNormalizedValue} normalizedValue={changedEvent.NormalizedValue} source='{changedEvent.Source}' reason='{changedEvent.Reason}'",
                DebugUtility.Colors.Info);
        }

        public IDisposable Subscribe(
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorAttributeId attributeId,
            Action<ActorAttributeChangedEvent> handler)
        {
            if (!actorInstanceRuntimeId.IsValid)
            {
                throw new ArgumentException("actorInstanceRuntimeId is required.", nameof(actorInstanceRuntimeId));
            }

            if (!attributeId.IsValid)
            {
                throw new ArgumentException("attributeId is required.", nameof(attributeId));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var binding = new EventBinding<ActorAttributeChangedEvent>(evt =>
            {
                if (!evt.IsValid ||
                    evt.ActorInstanceRuntimeId != actorInstanceRuntimeId ||
                    evt.AttributeId != attributeId)
                {
                    return;
                }

                handler(evt);
            });

            FilteredEventBus<ActorInstanceRuntimeId, ActorAttributeChangedEvent>.Register(
                actorInstanceRuntimeId,
                binding);

            return new Subscription(actorInstanceRuntimeId, binding);
        }

        private sealed class Subscription : IDisposable
        {
            private ActorInstanceRuntimeId _actorInstanceRuntimeId;
            private EventBinding<ActorAttributeChangedEvent> _binding;
            private bool _disposed;

            public Subscription(
                ActorInstanceRuntimeId actorInstanceRuntimeId,
                EventBinding<ActorAttributeChangedEvent> binding)
            {
                _actorInstanceRuntimeId = actorInstanceRuntimeId;
                _binding = binding;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                FilteredEventBus<ActorInstanceRuntimeId, ActorAttributeChangedEvent>.Unregister(
                    _actorInstanceRuntimeId,
                    _binding);
                _binding = null;
                _actorInstanceRuntimeId = default;
            }
        }
    }
}
