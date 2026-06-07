using System;
using System.Collections;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ObjectEmission.Runtime
{
    /// <summary>
    /// Canonical pooled runtime object used by ObjectEmission.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObjectEmissionPooledObject : PooledBehaviour
    {
        private ObjectEmissionRuntimePayload _payload;
        private IObjectEmissionReturnSink _returnSink;
        private Coroutine _lifetimeRoutine;
        private bool _hasPayload;
        private bool _lifetimeTimerPending;
        private bool _returnRequested;

        public bool HasPayload => _hasPayload;
        public ObjectEmissionRuntimePayload Payload => _hasPayload ? _payload : throw new InvalidOperationException("ObjectEmissionPooledObject payload is not initialized.");

        public void Initialize(ObjectEmissionRuntimePayload payload, IObjectEmissionReturnSink returnSink)
        {
            if (!payload.IsValid)
            {
                throw new InvalidOperationException("ObjectEmissionPooledObject requires a valid ObjectEmissionRuntimePayload.");
            }

            StopLifetimeTimer();
            _payload = payload;
            _returnSink = returnSink ?? throw new ArgumentNullException(nameof(returnSink), "ObjectEmissionPooledObject requires a non-null return sink.");
            _hasPayload = true;
            _returnRequested = false;
            _lifetimeTimerPending = payload.LifetimeSeconds > 0f;

            transform.SetPositionAndRotation(payload.SpawnPosition, payload.SpawnRotation);

            DebugUtility.Log(
                typeof(ObjectEmissionPooledObject),
                $"[OBS][ObjectEmission] event='ObjectEmissionPooledObjectInitialized' actorId='{payload.ActorId}' actorInstanceRuntimeId='{payload.ActorInstanceRuntimeId}' profileId='{payload.ProfileId}' speed='{payload.Speed:0.###}' lifetimeSeconds='{payload.LifetimeSeconds:0.###}' source='{payload.Source}' reason='{payload.Reason}'.",
                DebugUtility.Colors.Info);

            TryStartLifetimeTimer();
        }

        protected override void OnAfterPoolRent()
        {
            _returnRequested = false;
            _lifetimeTimerPending = _hasPayload && _payload.LifetimeSeconds > 0f;
            TryStartLifetimeTimer();
        }

        protected override void OnAfterPoolReturn()
        {
            StopLifetimeTimer();
            ClearRuntimeState();
        }

        protected override void OnAfterPoolDestroyed()
        {
            StopLifetimeTimer();
            ClearRuntimeState();
        }

        private void OnEnable()
        {
            TryStartLifetimeTimer();
        }

        private void OnDisable()
        {
            StopLifetimeTimer();
        }

        private void TryStartLifetimeTimer()
        {
            if (!_hasPayload || !_lifetimeTimerPending || !isActiveAndEnabled || !IsCurrentlyRented || _lifetimeRoutine != null)
            {
                return;
            }

            _lifetimeRoutine = StartCoroutine(LifetimeRoutine());
        }

        private IEnumerator LifetimeRoutine()
        {
            yield return new WaitForSeconds(_payload.LifetimeSeconds);
            _lifetimeRoutine = null;

            DebugUtility.Log(
                typeof(ObjectEmissionPooledObject),
                $"[OBS][ObjectEmission] event='ObjectEmissionPooledObjectLifetimeExpired' actorId='{_payload.ActorId}' actorInstanceRuntimeId='{_payload.ActorInstanceRuntimeId}' profileId='{_payload.ProfileId}' lifetimeSeconds='{_payload.LifetimeSeconds:0.###}' source='{_payload.Source}' reason='{_payload.Reason}'.",
                DebugUtility.Colors.Info);

            RequestReturn("lifetime_expired");
        }

        private void RequestReturn(string reason)
        {
            if (_returnRequested)
            {
                return;
            }

            if (_returnSink == null)
            {
                throw new InvalidOperationException("ObjectEmissionPooledObject requires a return sink before requesting return.");
            }

            _returnRequested = true;

            DebugUtility.Log(
                typeof(ObjectEmissionPooledObject),
                $"[OBS][ObjectEmission] event='ObjectEmissionPooledObjectReturnRequested' actorId='{_payload.ActorId}' actorInstanceRuntimeId='{_payload.ActorInstanceRuntimeId}' profileId='{_payload.ProfileId}' reason='{reason}' source='{_payload.Source}'.",
                DebugUtility.Colors.Info);

            _returnSink.RequestReturn(this, in _payload, reason);
        }

        private void StopLifetimeTimer()
        {
            if (_lifetimeRoutine == null)
            {
                return;
            }

            StopCoroutine(_lifetimeRoutine);
            _lifetimeRoutine = null;
        }

        private void ClearRuntimeState()
        {
            _payload = default;
            _returnSink = null;
            _hasPayload = false;
            _lifetimeTimerPending = false;
            _returnRequested = false;
        }
    }
}
