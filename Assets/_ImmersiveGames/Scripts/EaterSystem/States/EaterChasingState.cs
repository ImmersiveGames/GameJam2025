using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado de perseguição: o eater avança na direção do planeta marcado via PlanetMarkingManager.
    /// </summary>
    internal sealed class EaterChasingState : EaterBehaviorState
    {
        private const float DistanceTolerance = 0.05f;

        private bool _reportedMissingTarget;
        private bool _haltMovement;
        private Transform _haltedTarget;
        private PlanetOrbitFreezeController _orbitFreezeController;
        private bool _pendingEatingTransition;
        private Collider _cachedTargetCollider;
        private Transform _cachedColliderOwner;
        private Collider _eaterCollider;
        private bool _preserveOrbitAnchor;
        private bool _pendingHungryTransition;

        public EaterChasingState() : base("Chasing")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _pendingEatingTransition = false;
            _preserveOrbitAnchor = false;
            _pendingHungryTransition = false;
        }

        public override void OnExit()
        {
            ResetMovementHalt(_preserveOrbitAnchor);
            ClearEatingTransitionRequest();
            _pendingHungryTransition = false;
            base.OnExit();
        }

        public override void Update()
        {
            base.Update();

            Transform target = Behavior.CurrentTargetPlanet;
            if (target == null)
            {
                ResetMovementHalt();
                ClearEatingTransitionRequest();
                ClearTargetColliderCache();
                Behavior?.ClearOrbitAnchor();
                RequestHungryTransition("TargetLost");
                if (!_reportedMissingTarget && Behavior.ShouldLogStateTransitions)
                {
                    DebugUtility.LogVerbose(
                        "Nenhum planeta marcado para perseguir.",
                        context: Behavior,
                        instance: this);
                    _reportedMissingTarget = true;
                }
                return;
            }

            _reportedMissingTarget = false;

            Collider targetCollider = ResolveTargetCollider(target);
            Vector3 targetCenter = ResolveTargetCenter(target, targetCollider);
            Behavior.LookAt(targetCenter);

            if (_haltMovement && !ReferenceEquals(target, _haltedTarget))
            {
                ResetMovementHalt();
                ClearEatingTransitionRequest();
            }

            Vector3 toCenter = targetCenter - Transform.position;
            Vector3 fallbackForward = Transform != null ? Transform.forward : Vector3.forward;
            Vector3 centerDirection = toCenter.sqrMagnitude > Mathf.Epsilon
                ? toCenter.normalized
                : fallbackForward;

            Vector3 surfaceDirection = centerDirection;
            float surfaceDistance = CalculateSurfaceDistance(target, targetCollider, toCenter, centerDirection, out surfaceDirection);

            if (surfaceDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                surfaceDirection = centerDirection;
            }

            float stopDistance = ResolveStopDistance();
            float tolerance = Mathf.Max(DistanceTolerance, stopDistance * 0.1f);

            if (stopDistance > 0f && surfaceDistance < stopDistance - tolerance)
            {
                Vector3 retreatDirection = surfaceDistance < 0f ? surfaceDirection : -surfaceDirection;
                if (retreatDirection.sqrMagnitude <= Mathf.Epsilon)
                {
                    retreatDirection = -centerDirection;
                }

                retreatDirection = retreatDirection.sqrMagnitude > Mathf.Epsilon
                    ? retreatDirection.normalized
                    : fallbackForward;

                float correction = stopDistance - surfaceDistance;
                Behavior.Translate(retreatDirection * correction, respectPlayerBounds: false);
                surfaceDistance = stopDistance;
                surfaceDirection = centerDirection;
            }

            if (stopDistance <= 0f)
            {
                ChaseTarget(target, targetCenter, surfaceDirection, Mathf.Max(surfaceDistance, 0f), 0f);
                return;
            }

            if (surfaceDistance <= stopDistance + tolerance)
            {
                HaltChasingTarget(target, targetCenter, surfaceDirection, stopDistance, surfaceDistance);
                RequestEatingTransition();
                return;
            }

            ChaseTarget(target, targetCenter, surfaceDirection, surfaceDistance, stopDistance);
        }

        internal bool ConsumeEatingTransitionRequest()
        {
            if (!_pendingEatingTransition)
            {
                return false;
            }

            _pendingEatingTransition = false;
            return true;
        }

        internal bool ConsumeHungryTransitionRequest()
        {
            if (!_pendingHungryTransition)
            {
                return false;
            }

            _pendingHungryTransition = false;
            return true;
        }

        private float ResolveStopDistance()
        {
            return Mathf.Max(Config?.MinimumChaseDistance ?? 0f, 0f);
        }

        private void ChaseTarget(Transform target, Vector3 targetCenter, Vector3 direction, float distance, float stopDistance)
        {
            if (_haltMovement)
            {
                ResumeChasingTarget();
            }

            Behavior.LookAt(targetCenter);
            float speed = Behavior.GetChaseSpeed();
            float travelDistance = speed * Time.deltaTime;

            if (stopDistance > 0f)
            {
                float remaining = Mathf.Max(distance - stopDistance, 0f);
                travelDistance = Mathf.Min(travelDistance, remaining);
            }

            if (travelDistance <= 0f)
            {
                HaltChasingTarget(target, targetCenter, direction, stopDistance, distance);
                RequestEatingTransition();
                return;
            }

            Behavior.RotateTowards(direction, Time.deltaTime);
            Behavior.Translate(direction * travelDistance, respectPlayerBounds: false);
        }

        private void HaltChasingTarget(Transform target, Vector3 targetCenter, Vector3 surfaceDirection, float stopDistance, float surfaceDistance)
        {
            if (!ReferenceEquals(_haltedTarget, target))
            {
                ResetMovementHalt();
                _haltedTarget = target;
            }

            _haltMovement = true;
            Behavior.LookAt(targetCenter);
            EnsureOrbitFreezeController().TryFreeze(Behavior, target);
            AlignToMinimumDistance(surfaceDirection, stopDistance, surfaceDistance);
            RegisterOrbitAnchor(target, targetCenter, stopDistance, surfaceDistance);
        }

        private void ResumeChasingTarget()
        {
            ResetMovementHalt();
            ClearEatingTransitionRequest();
        }

        private void ResetMovementHalt(bool preserveOrbitAnchor = false)
        {
            if (_haltMovement)
            {
                EnsureOrbitFreezeController().Release();
            }

            if (!preserveOrbitAnchor && Behavior != null && _haltedTarget != null)
            {
                Behavior.ClearOrbitAnchor(_haltedTarget);
            }

            _haltMovement = false;
            _haltedTarget = null;
            _preserveOrbitAnchor = false;
        }

        private void ClearTargetColliderCache()
        {
            _cachedTargetCollider = null;
            _cachedColliderOwner = null;
        }

        private PlanetOrbitFreezeController EnsureOrbitFreezeController()
        {
            return _orbitFreezeController ??= new PlanetOrbitFreezeController(this);
        }

        private void RequestEatingTransition()
        {
            if (_pendingEatingTransition)
            {
                return;
            }

            _pendingEatingTransition = true;
            _preserveOrbitAnchor = true;

            if (!Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            DebugUtility.Log(
                "Distância mínima alcançada. Solicitando transição para o estado de alimentação.",
                DebugUtility.Colors.CrucialInfo,
                Behavior,
                this);
        }

        private void ClearEatingTransitionRequest()
        {
            _pendingEatingTransition = false;
            _preserveOrbitAnchor = false;
        }

        private void RequestHungryTransition(string reason)
        {
            if (_pendingHungryTransition)
            {
                return;
            }

            _pendingHungryTransition = true;

            if (!Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            DebugUtility.Log(
                $"Planeta perdido durante a perseguição. Solicitando retorno ao estado faminto: {reason}.",
                DebugUtility.Colors.CrucialInfo,
                Behavior,
                this);
        }

        private Collider ResolveTargetCollider(Transform target)
        {
            if (target == null)
            {
                ClearTargetColliderCache();
                return null;
            }

            if (ReferenceEquals(_cachedColliderOwner, target) && _cachedTargetCollider != null)
            {
                return _cachedTargetCollider;
            }

            Collider collider = target.GetComponent<Collider>();
            if (collider == null)
            {
                collider = target.GetComponentInChildren<Collider>();
            }

            _cachedColliderOwner = target;
            _cachedTargetCollider = collider;
            return collider;
        }

        private Collider EnsureEaterCollider()
        {
            if (_eaterCollider != null)
            {
                return _eaterCollider;
            }

            return Behavior != null && Behavior.TryGetComponent(out _eaterCollider)
                ? _eaterCollider
                : null;
        }

        /// <summary>
        /// Calcula a distância até a superfície efetiva do planeta marcado considerando o colisor disponível.
        /// Quando possível usa <see cref="Physics.ComputePenetration"/> para detectar sobreposição e gerar um vetor de separação.
        /// </summary>
        private float CalculateSurfaceDistance(
            Transform target,
            Collider targetCollider,
            Vector3 toCenter,
            Vector3 centerDirection,
            out Vector3 directionToSurface)
        {
            directionToSurface = centerDirection;

            if (target == null)
            {
                return 0f;
            }

            Vector3 normalizedCenterDirection = centerDirection.sqrMagnitude > Mathf.Epsilon
                ? centerDirection
                : (Transform != null ? Transform.forward : Vector3.forward);

            if (targetCollider == null || !targetCollider.enabled)
            {
                directionToSurface = normalizedCenterDirection;
                return toCenter.magnitude;
            }

            Collider eaterCollider = EnsureEaterCollider();
            Vector3 eaterReference = eaterCollider != null
                ? eaterCollider.bounds.center
                : Transform.position;

            Vector3 closestPoint = targetCollider.ClosestPoint(eaterReference);
            Vector3 toClosest = closestPoint - eaterReference;
            if (toClosest.sqrMagnitude > Mathf.Epsilon)
            {
                directionToSurface = toClosest.normalized;
                return toClosest.magnitude;
            }

            if (eaterCollider != null && eaterCollider.enabled && Physics.ComputePenetration(
                    eaterCollider,
                    eaterCollider.transform.position,
                    eaterCollider.transform.rotation,
                    targetCollider,
                    targetCollider.transform.position,
                    targetCollider.transform.rotation,
                    out Vector3 separationDirection,
                    out float separationDistance))
            {
                directionToSurface = separationDirection.sqrMagnitude > Mathf.Epsilon
                    ? separationDirection.normalized
                    : normalizedCenterDirection;
                return -separationDistance;
            }

            directionToSurface = normalizedCenterDirection;
            return 0f;
        }

        private static Vector3 ResolveTargetCenter(Transform target, Collider targetCollider)
        {
            if (targetCollider != null)
            {
                return targetCollider.bounds.center;
            }

            return target != null ? target.position : Vector3.zero;
        }

        private void AlignToMinimumDistance(Vector3 surfaceDirection, float stopDistance, float surfaceDistance)
        {
            if (Behavior == null || stopDistance <= 0f)
            {
                return;
            }

            Vector3 fallbackForward = Transform != null ? Transform.forward : Vector3.forward;
            Vector3 direction = surfaceDirection.sqrMagnitude > Mathf.Epsilon
                ? surfaceDirection.normalized
                : fallbackForward.sqrMagnitude > Mathf.Epsilon
                    ? fallbackForward.normalized
                    : Vector3.forward;

            float difference = surfaceDistance - stopDistance;
            if (Mathf.Abs(difference) <= DistanceTolerance)
            {
                return;
            }

            Behavior.Translate(direction * difference, respectPlayerBounds: false);
        }

        private void RegisterOrbitAnchor(Transform target, Vector3 targetCenter, float stopDistance, float surfaceDistance)
        {
            if (Behavior == null)
            {
                return;
            }

            float referenceSurfaceDistance = stopDistance > 0f
                ? Mathf.Max(0f, stopDistance)
                : Mathf.Max(0f, surfaceDistance);

            Behavior.RegisterOrbitAnchor(target, targetCenter, referenceSurfaceDistance);
        }
    }
}
