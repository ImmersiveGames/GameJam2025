using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    public sealed class ActorImpactEffectEventStream : IActorImpactEffectEventStream
    {
        public void Publish(ActorImpactEffectEvent impactEffectEvent)
        {
            if (!impactEffectEvent.IsValid)
            {
                throw new InvalidOperationException("ActorImpactEffectEvent is invalid.");
            }

            EventBus<ActorImpactEffectEvent>.Raise(impactEffectEvent);
            FilteredEventBus<ActorInstanceRuntimeId, ActorImpactEffectEvent>.Raise(
                impactEffectEvent.ImpactActorInstanceRuntimeId,
                impactEffectEvent);

            DebugUtility.LogVerbose(
                typeof(ActorImpactEffectEventStream),
                $"event='ActorImpactEffectEventPublished' impactActorId='{impactEffectEvent.ImpactActorId}' impactActorInstanceRuntimeId='{impactEffectEvent.ImpactActorInstanceRuntimeId}' ownerActorId='{impactEffectEvent.OwnerActorId}' ownerActorInstanceRuntimeId='{impactEffectEvent.OwnerActorInstanceRuntimeId}' targetActorId='{impactEffectEvent.TargetActorId}' targetActorInstanceRuntimeId='{impactEffectEvent.TargetActorInstanceRuntimeId}' impactKind='{impactEffectEvent.ImpactKind}' targetObject='{impactEffectEvent.TargetObjectName}' targetCollider='{impactEffectEvent.TargetColliderName}' hasContact='{impactEffectEvent.HasContact}' contactPoint='{FormatVector(impactEffectEvent.ContactPoint)}' contactNormal='{FormatVector(impactEffectEvent.ContactNormal)}' rawDamageAmount='{impactEffectEvent.RawDamageAmount:0.###}' damageApplicationRequested='{impactEffectEvent.DamageApplicationRequested}' damageApplicationCompleted='{impactEffectEvent.DamageApplicationCompleted}' returnRequestEligible='{impactEffectEvent.ReturnRequestEligible}' source='{impactEffectEvent.Source}' reason='{impactEffectEvent.Reason}'",
                DebugUtility.Colors.Success);
        }

        public IDisposable Subscribe(Action<ActorImpactEffectEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var binding = new EventBinding<ActorImpactEffectEvent>(handler);
            EventBus<ActorImpactEffectEvent>.Register(binding);
            return new GlobalSubscription(binding);
        }

        public IDisposable Subscribe(
            ActorInstanceRuntimeId impactActorInstanceRuntimeId,
            Action<ActorImpactEffectEvent> handler)
        {
            if (!impactActorInstanceRuntimeId.IsValid)
            {
                throw new ArgumentException("impactActorInstanceRuntimeId is required.", nameof(impactActorInstanceRuntimeId));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var binding = new EventBinding<ActorImpactEffectEvent>(evt =>
            {
                if (!evt.IsValid || evt.ImpactActorInstanceRuntimeId != impactActorInstanceRuntimeId)
                {
                    return;
                }

                handler(evt);
            });

            FilteredEventBus<ActorInstanceRuntimeId, ActorImpactEffectEvent>.Register(
                impactActorInstanceRuntimeId,
                binding);

            return new ScopedSubscription(impactActorInstanceRuntimeId, binding);
        }

        private sealed class GlobalSubscription : IDisposable
        {
            private EventBinding<ActorImpactEffectEvent> _binding;
            private bool _disposed;

            public GlobalSubscription(EventBinding<ActorImpactEffectEvent> binding)
            {
                _binding = binding;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                EventBus<ActorImpactEffectEvent>.Unregister(_binding);
                _binding = null;
            }
        }

        private sealed class ScopedSubscription : IDisposable
        {
            private ActorInstanceRuntimeId _impactActorInstanceRuntimeId;
            private EventBinding<ActorImpactEffectEvent> _binding;
            private bool _disposed;

            public ScopedSubscription(
                ActorInstanceRuntimeId impactActorInstanceRuntimeId,
                EventBinding<ActorImpactEffectEvent> binding)
            {
                _impactActorInstanceRuntimeId = impactActorInstanceRuntimeId;
                _binding = binding;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                FilteredEventBus<ActorInstanceRuntimeId, ActorImpactEffectEvent>.Unregister(
                    _impactActorInstanceRuntimeId,
                    _binding);
                _binding = null;
                _impactActorInstanceRuntimeId = default;
            }
        }

        private static string FormatVector(UnityEngine.Vector3 value)
        {
            return $"{value.x:0.###},{value.y:0.###},{value.z:0.###}";
        }
    }
}
