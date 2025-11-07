using DG.Tweening;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado de alimentação: orbita o planeta marcado utilizando DOTween.
    /// </summary>
    internal sealed class EaterEatingState : EaterBehaviorState
    {
        private Tween _approachTween;
        private Tween _orbitTween;
        private Transform _currentTarget;
        private Vector3 _radialBasis;
        private float _currentAngle;

        public EaterEatingState() : base("Eating")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            PrepareTarget(Behavior.CurrentTargetPlanet, restartOrbit: true);
        }

        public override void OnExit()
        {
            base.OnExit();
            StopTweens();
            _currentTarget = null;
        }

        public override void Update()
        {
            base.Update();

            Transform target = Behavior.CurrentTargetPlanet;
            if (target != _currentTarget)
            {
                PrepareTarget(target, restartOrbit: true);
            }

            if (target != null)
            {
                LookAtTarget();
            }
        }

        private void PrepareTarget(Transform target, bool restartOrbit)
        {
            _currentTarget = target;
            StopTweens();

            if (_currentTarget == null)
            {
                if (Behavior.ShouldLogStateTransitions)
                {
                    DebugUtility.LogWarning<EaterEatingState>("Estado Eating sem planeta marcado.", Behavior);
                }
                return;
            }

            _radialBasis = ResolveRadialBasis();
            Vector3 desiredPosition = _currentTarget.position + _radialBasis * Behavior.OrbitDistance;
            float distanceToDesired = Vector3.Distance(Transform.position, desiredPosition);

            if (distanceToDesired > 0.05f)
            {
                _approachTween = Transform.DOMove(desiredPosition, Behavior.OrbitApproachDuration)
                    .SetEase(Ease.InOutSine)
                    .OnUpdate(LookAtTarget)
                    .OnComplete(StartOrbit);
            }
            else if (restartOrbit)
            {
                Transform.position = desiredPosition;
                StartOrbit();
            }
        }

        private void StartOrbit()
        {
            StopOrbitTween();

            if (_currentTarget == null)
            {
                return;
            }

            _radialBasis = ResolveRadialBasis();
            _currentAngle = 0f;

            _orbitTween = DOTween.To(() => _currentAngle, angle =>
                {
                    _currentAngle = angle;
                    ApplyOrbitAngle(angle);
                },
                360f,
                Behavior.OrbitDuration)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .OnUpdate(LookAtTarget);

            if (Behavior.ShouldLogStateTransitions)
            {
                DebugUtility.Log<EaterEatingState>("Órbita iniciada.", DebugUtility.Colors.Success, Behavior);
            }
        }

        private void ApplyOrbitAngle(float angle)
        {
            if (_currentTarget == null)
            {
                return;
            }

            Vector3 axis = ResolveOrbitAxis();
            Vector3 radial = Vector3.ProjectOnPlane(_radialBasis, axis);
            if (radial.sqrMagnitude <= Mathf.Epsilon)
            {
                radial = ResolveRadialBasis();
            }
            radial = radial.normalized;
            _radialBasis = radial;
            Vector3 tangent = Vector3.Cross(axis, radial).normalized;

            float radians = angle * Mathf.Deg2Rad;
            Vector3 offset = (radial * Mathf.Cos(radians) + tangent * Mathf.Sin(radians)) * Behavior.OrbitDistance;
            Transform.position = _currentTarget.position + offset;
        }

        private Vector3 ResolveRadialBasis()
        {
            if (_currentTarget == null)
            {
                return Transform.forward.sqrMagnitude > Mathf.Epsilon ? Transform.forward.normalized : Vector3.forward;
            }

            Vector3 axis = ResolveOrbitAxis();
            Vector3 offset = Transform.position - _currentTarget.position;
            Vector3 planarOffset = Vector3.ProjectOnPlane(offset, axis);
            if (planarOffset.sqrMagnitude <= Mathf.Epsilon)
            {
                planarOffset = Vector3.ProjectOnPlane(Transform.forward, axis);
            }

            if (planarOffset.sqrMagnitude <= Mathf.Epsilon)
            {
                planarOffset = Vector3.ProjectOnPlane(Vector3.right, axis);
            }

            return planarOffset.normalized;
        }

        private Vector3 ResolveOrbitAxis()
        {
            return _currentTarget != null ? _currentTarget.up.normalized : Vector3.up;
        }

        private void LookAtTarget()
        {
            if (_currentTarget == null)
            {
                return;
            }

            Behavior.LookAt(_currentTarget.position);
        }

        private void StopTweens()
        {
            StopOrbitTween();
            if (_approachTween != null && _approachTween.IsActive())
            {
                _approachTween.Kill();
            }
            _approachTween = null;
        }

        private void StopOrbitTween()
        {
            if (_orbitTween != null && _orbitTween.IsActive())
            {
                _orbitTween.Kill();
            }
            _orbitTween = null;
        }
    }
}
