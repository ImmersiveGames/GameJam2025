using _ImmersiveGames.NewScripts.Core.Logging;
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

        public EaterChasingState() : base("Chasing")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _pendingEatingTransition = false;
            _preserveOrbitAnchor = false;
        }

        public override void OnExit()
        {
            ResetMovementHalt(_preserveOrbitAnchor);
            ClearEatingTransitionRequest();
            base.OnExit();
        }

        public override void Update()
        {
            base.Update();

            var target = Behavior.CurrentTargetPlanet;
            if (!HasTarget(target))
            {
                return;
            }

            var targetCollider = ResolveTargetCollider(target);
            var targetCenter = ResolveTargetCenter(target, targetCollider);
            Behavior.LookAt(targetCenter);

            EnsureHaltConsistency(target);

            (var centerDirection, float surfaceDistance, var surfaceDirection) = ComputeNavigationVectors(target, targetCollider, targetCenter);

            float stopDistance = ResolveStopDistance();
            if (stopDistance <= 0f)
            {
                // Com distância mínima zero, apenas perseguição contínua (com distância não negativa)
                ChaseTarget(target, targetCenter, surfaceDirection, Mathf.Max(surfaceDistance, 0f), 0f);
                return;
            }

            float tolerance = Mathf.Max(DistanceTolerance, stopDistance * 0.1f);

            // Se estiver mais dentro do que o permitido (além da tolerância), recua para a borda mínima
            RetreatIfTooClose(stopDistance, tolerance, ref surfaceDistance, ref surfaceDirection, centerDirection);

            if (surfaceDistance <= stopDistance + tolerance)
            {
                HaltChasingTarget(target, targetCenter, surfaceDirection, stopDistance, surfaceDistance);
                RequestEatingTransition();
                return;
            }

            ChaseTarget(target, targetCenter, surfaceDirection, surfaceDistance, stopDistance);
            return;

            // -------- Funções locais para reduzir complexidade --------

            bool HasTarget(Transform t)
            {
                if (t != null)
                {
                    _reportedMissingTarget = false;
                    return true;
                }

                ResetMovementHalt();
                ClearEatingTransitionRequest();
                ClearTargetColliderCache();
                Behavior?.ClearOrbitAnchor();

                if (!_reportedMissingTarget && Behavior is { ShouldLogStateTransitions: true })
                {
                    DebugUtility.LogVerbose(
                        "Nenhum planeta marcado para perseguir.",
                        context: Behavior,
                        instance: this);
                    _reportedMissingTarget = true;
                }

                return false;
            }

            void EnsureHaltConsistency(Transform t)
            {
                if (_haltMovement && !ReferenceEquals(t, _haltedTarget))
                {
                    ResetMovementHalt();
                    ClearEatingTransitionRequest();
                }
            }

            (Vector3 centerDir, float surfaceDist, Vector3 surfaceDir) ComputeNavigationVectors(Transform t, Collider tCollider, Vector3 tCenter)
            {
                var toCenter = tCenter - Transform.position;
                var fallbackForward = Transform != null ? Transform.forward : Vector3.forward;
                var centerDir = toCenter.sqrMagnitude > Mathf.Epsilon
                    ? toCenter.normalized
                    : fallbackForward;

                float surfaceDist = CalculateSurfaceDistance(t, tCollider, toCenter, centerDir, out var surfaceDir);
                if (surfaceDir.sqrMagnitude <= Mathf.Epsilon)
                {
                    surfaceDir = centerDir;
                }

                return (centerDir, surfaceDist, surfaceDir);
            }

            void RetreatIfTooClose(float stopDist, float tol, ref float surfDist, ref Vector3 surfDir, Vector3 centerDir)
            {
                if (stopDist <= 0f || surfDist >= stopDist - tol)
                {
                    return;
                }

                var retreatDir = surfDist < 0f ? surfDir : -surfDir;
                if (retreatDir.sqrMagnitude <= Mathf.Epsilon)
                {
                    retreatDir = -centerDir;
                }

                var fallbackForward = Transform != null ? Transform.forward : Vector3.forward;
                retreatDir = retreatDir.sqrMagnitude > Mathf.Epsilon
                    ? retreatDir.normalized
                    : fallbackForward;

                float correction = stopDist - surfDist;
                Behavior.Translate(retreatDir * correction, respectPlayerBounds: false);
                surfDist = stopDist;
                surfDir = centerDir;
            }
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

            DebugUtility.LogVerbose(
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

            var collider = target.GetComponent<Collider>();
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

            var normalizedCenterDirection = centerDirection.sqrMagnitude > Mathf.Epsilon
                ? centerDirection
                : (Transform != null ? Transform.forward : Vector3.forward);

            if (targetCollider == null || !targetCollider.enabled)
            {
                directionToSurface = normalizedCenterDirection;
                return toCenter.magnitude;
            }

            var eaterCollider = EnsureEaterCollider();
            var eaterReference = eaterCollider != null
                ? eaterCollider.bounds.center
                : Transform.position;

            var closestPoint = targetCollider.ClosestPoint(eaterReference);
            var toClosest = closestPoint - eaterReference;
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
                    out var separationDirection,
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

            var fallbackForward = Transform != null ? Transform.forward : Vector3.forward;
            var direction = surfaceDirection.sqrMagnitude > Mathf.Epsilon
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

