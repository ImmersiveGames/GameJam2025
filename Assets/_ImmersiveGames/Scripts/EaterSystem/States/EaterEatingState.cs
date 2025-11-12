using DG.Tweening;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.EaterSystem.Animations;
using _ImmersiveGames.Scripts.EaterSystem.Detections;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado de alimentação: orbita o planeta marcado utilizando DOTween.
    /// </summary>
    internal sealed class EaterEatingState : EaterBehaviorState
    {
        private const float DefaultOrbitDistance = 3f;
        private const float DefaultOrbitDuration = 4f;
        private const float DefaultOrbitApproachDuration = 0.5f;

        private Tween _approachTween;
        private Tween _orbitTween;
        private Transform _currentTarget;
        private Vector3 _radialBasis;
        private float _currentAngle;
        private EaterAnimationController _animationController;
        private bool _missingAnimationLogged;
        private readonly PlanetOrbitFreezeHandle _orbitFreezeHandle;
        private bool _proximityEventsSubscribed;
        private EventBinding<DetectionEnterEvent> _proximityEnterBinding;
        private EventBinding<DetectionExitEvent> _proximityExitBinding;
        private EaterDetectionController _detectionController;
        private DetectionType _planetProximityDetectionType;

        private float OrbitDistance => Config?.OrbitDistance ?? DefaultOrbitDistance;

        private float OrbitDuration => Config?.OrbitDuration ?? DefaultOrbitDuration;

        private float OrbitApproachDuration
        {
            get
            {
                float duration = OrbitDuration;
                float approach = Config?.OrbitApproachDuration ?? DefaultOrbitApproachDuration;
                approach = Mathf.Max(0.1f, approach);
                return Mathf.Min(approach, duration);
            }
        }

        public EaterEatingState() : base("Eating")
        {
            _orbitFreezeHandle = new PlanetOrbitFreezeHandle(this);
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (TryEnsureAnimationController())
            {
                _animationController.SetEating(true);
            }

            SubscribeToProximityEvents();
            PrepareTarget(Behavior?.CurrentTargetPlanet, restartOrbit: true, reason: "EatingState.OnEnter");
        }

        public override void OnExit()
        {
            if (TryEnsureAnimationController())
            {
                _animationController.SetEating(false);
            }

            base.OnExit();
            StopTweens();
            _currentTarget = null;
            _orbitFreezeHandle.Release();
            UnsubscribeFromProximityEvents();
        }

        public override void Update()
        {
            base.Update();

            Transform target = Behavior.CurrentTargetPlanet;
            if (target == null)
            {
                HandleNoMarkedPlanet();
                return;
            }

            if (!ReferenceEquals(target, _currentTarget))
            {
                PrepareTarget(target, restartOrbit: true, reason: "EatingState.Update.TargetChanged");
            }
            else if (!IsTargetWithinProximity(target))
            {
                HandleTargetOutsideSensor("EatingState.Update.TargetOutsideSensor");
                return;
            }

            if (target != null)
            {
                LookAtTarget();
            }
        }

        private void PrepareTarget(Transform target, bool restartOrbit, string reason)
        {
            _currentTarget = target;
            StopTweens();

            if (_currentTarget == null)
            {
                _orbitFreezeHandle.Release();
                if (Behavior != null && Behavior.ShouldLogStateTransitions)
                {
                    DebugUtility.LogWarning<EaterEatingState>("Estado Eating sem planeta marcado.", Behavior);
                }
                Behavior?.TryEnterHungryState($"{reason}.NoMarkedPlanet");
                return;
            }

            if (!IsTargetWithinProximity(_currentTarget))
            {
                HandleTargetOutsideSensor(reason);
                return;
            }

            _orbitFreezeHandle.Request(_currentTarget, Behavior != null && Behavior.ShouldLogStateTransitions, Behavior, this);

            _radialBasis = ResolveRadialBasis();
            Vector3 desiredPosition = _currentTarget.position + _radialBasis * OrbitDistance;
            float distanceToDesired = Vector3.Distance(Transform.position, desiredPosition);

            if (distanceToDesired > 0.05f)
            {
                _approachTween = Transform.DOMove(desiredPosition, OrbitApproachDuration)
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
                OrbitDuration)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .OnUpdate(LookAtTarget);

            if (Behavior.ShouldLogStateTransitions)
            {
                DebugUtility.Log(
                    "Órbita iniciada.",
                    DebugUtility.Colors.Success,
                    context: Behavior,
                    instance: this);
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
            Vector3 offset = (radial * Mathf.Cos(radians) + tangent * Mathf.Sin(radians)) * OrbitDistance;
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

        private bool TryEnsureAnimationController()
        {
            if (_animationController != null)
            {
                return true;
            }

            if (Behavior == null)
            {
                return false;
            }

            if (Behavior.TryGetAnimationController(out EaterAnimationController controller))
            {
                _animationController = controller;
                _missingAnimationLogged = false;
                return true;
            }

            if (!_missingAnimationLogged)
            {
                DebugUtility.LogWarning(
                    "EaterAnimationController não encontrado. Não será possível atualizar animação de alimentação.",
                    Behavior,
                    this);
                _missingAnimationLogged = true;
            }

            return false;
        }

        private void SubscribeToProximityEvents()
        {
            if (_proximityEventsSubscribed)
            {
                return;
            }

            if (!TryResolveProximityDependencies())
            {
                return;
            }

            _proximityEnterBinding ??= new EventBinding<DetectionEnterEvent>(HandleProximityEnter);
            _proximityExitBinding ??= new EventBinding<DetectionExitEvent>(HandleProximityExit);

            EventBus<DetectionEnterEvent>.Register(_proximityEnterBinding);
            EventBus<DetectionExitEvent>.Register(_proximityExitBinding);
            _proximityEventsSubscribed = true;
        }

        private void UnsubscribeFromProximityEvents()
        {
            if (!_proximityEventsSubscribed)
            {
                return;
            }

            if (_proximityEnterBinding != null)
            {
                EventBus<DetectionEnterEvent>.Unregister(_proximityEnterBinding);
            }

            if (_proximityExitBinding != null)
            {
                EventBus<DetectionExitEvent>.Unregister(_proximityExitBinding);
            }

            _proximityEventsSubscribed = false;
        }

        private bool TryResolveProximityDependencies()
        {
            if (Behavior == null)
            {
                return false;
            }

            if (_detectionController == null && !Behavior.TryGetDetectionController(out _detectionController))
            {
                DebugUtility.LogWarning(
                    "EaterDetectionController não encontrado para monitorar eventos de proximidade durante Eating.",
                    Behavior,
                    this);
                return false;
            }

            _planetProximityDetectionType = _detectionController.PlanetProximityDetectionType;
            if (_planetProximityDetectionType == null)
            {
                DebugUtility.LogWarning(
                    "DetectionType de proximidade não configurado para Eating.",
                    Behavior,
                    this);
                return false;
            }

            return true;
        }

        private bool IsRelevantProximityEvent(IDetector detector, DetectionType detectionType)
        {
            if (_detectionController == null || _planetProximityDetectionType == null)
            {
                return false;
            }

            if (!ReferenceEquals(detector, _detectionController))
            {
                return false;
            }

            return ReferenceEquals(detectionType, _planetProximityDetectionType);
        }

        private void HandleProximityEnter(DetectionEnterEvent enterEvent)
        {
            if (!IsRelevantProximityEvent(enterEvent.Detector, enterEvent.DetectionType))
            {
                return;
            }

            if (!EaterPlanetUtility.TryResolveMarkedPlanet(enterEvent.Detectable, out var planetActor, out var markedPlanet))
            {
                return;
            }

            Transform planetTransform = markedPlanet.transform;
            if (!ReferenceEquals(Behavior?.CurrentTargetPlanet, planetTransform))
            {
                return;
            }

            _orbitFreezeHandle.Request(planetTransform, Behavior != null && Behavior.ShouldLogStateTransitions, Behavior, this);

            if (!ReferenceEquals(_currentTarget, planetTransform))
            {
                PrepareTarget(planetTransform, restartOrbit: true, reason: "EatingState.ProximityEnter");
            }
        }

        private void HandleProximityExit(DetectionExitEvent exitEvent)
        {
            if (!IsRelevantProximityEvent(exitEvent.Detector, exitEvent.DetectionType))
            {
                return;
            }

            if (!EaterPlanetUtility.TryResolveMarkedPlanet(exitEvent.Detectable, out var planetActor, out var markedPlanet))
            {
                return;
            }

            Transform planetTransform = markedPlanet.transform;
            if (!ReferenceEquals(_currentTarget, planetTransform))
            {
                return;
            }

            string planetName = EaterPlanetUtility.GetPlanetDisplayName(planetActor);
            DebugUtility.Log(
                $"Planeta {planetName} saiu do sensor durante Eating.",
                DebugUtility.Colors.Info,
                Behavior,
                this);

            HandleTargetOutsideSensor("EatingState.ProximityExit");
        }

        private bool IsTargetWithinProximity(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            if (_detectionController == null)
            {
                TryResolveProximityDependencies();
            }

            return _detectionController != null && _detectionController.IsTransformWithinProximity(target);
        }

        private void HandleTargetOutsideSensor(string reason)
        {
            _orbitFreezeHandle.Release();
            StopTweens();

            if (Behavior == null)
            {
                return;
            }

            Transform target = Behavior.CurrentTargetPlanet;
            if (target != null)
            {
                Behavior.TryEnterChasingState($"{reason}.ResumeChasing");
            }
            else
            {
                Behavior.TryEnterHungryState($"{reason}.NoMarkedPlanet");
            }
        }

        private void HandleNoMarkedPlanet()
        {
            _orbitFreezeHandle.Release();
            StopTweens();
            Behavior?.TryEnterHungryState("EatingState.NoMarkedPlanet");
        }
    }
}
