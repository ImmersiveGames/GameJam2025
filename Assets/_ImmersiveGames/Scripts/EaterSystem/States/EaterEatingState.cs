using DG.Tweening;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
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
        private bool _pendingHungryFallback;
        private bool _markedPlanetSubscribed;
        private EventBinding<PlanetMarkingChangedEvent> _planetMarkingChangedBinding;
        private PlanetMotion _capturedPlanetMotion;
        private float _orbitDistanceOverride;
        private bool _hasOrbitDistanceOverride;
        private bool _proximityEventsSubscribed;
        private EventBinding<DetectionExitEvent> _proximityExitBinding;
        private EaterDetectionController _detectionController;
        private DetectionType _planetProximityDetectionType;

        private float OrbitDistance
        {
            get
            {
                if (_hasOrbitDistanceOverride)
                {
                    return Mathf.Max(_orbitDistanceOverride, 0.1f);
                }

                return Config?.OrbitDistance ?? DefaultOrbitDistance;
            }
        }

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
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _pendingHungryFallback = false;
            _capturedPlanetMotion = null;
            _hasOrbitDistanceOverride = false;
            _orbitDistanceOverride = 0f;
            SubscribeToMarkedPlanets();
            SubscribeToProximityEvents();
            ConfigureEatingContext();
            PrepareTarget(Behavior.CurrentTargetPlanet, restartOrbit: true);
        }

        public override void OnExit()
        {
            base.OnExit();
            UnsubscribeFromMarkedPlanets();
            UnsubscribeFromProximityEvents();
            StopTweens();
            ResumeCapturedPlanetOrbit();
            _currentTarget = null;
            _pendingHungryFallback = false;
            _capturedPlanetMotion = null;
            _hasOrbitDistanceOverride = false;
            _orbitDistanceOverride = 0f;
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
            else
            {
                RequestHungryFallback("EatingState.TargetMissing");
            }
        }

        internal bool ConsumeHungryFallbackRequest()
        {
            if (!_pendingHungryFallback)
            {
                return false;
            }

            _pendingHungryFallback = false;
            return true;
        }

        private void PrepareTarget(Transform target, bool restartOrbit)
        {
            _currentTarget = target;
            StopTweens();

            if (_currentTarget == null)
            {
                RequestHungryFallback("EatingState.TargetUnavailable");
                if (Behavior.ShouldLogStateTransitions)
                {
                    DebugUtility.LogWarning<EaterEatingState>("Estado Eating sem planeta marcado.", Behavior);
                }
                return;
            }

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

        private void SubscribeToMarkedPlanets()
        {
            if (_markedPlanetSubscribed)
            {
                return;
            }

            _planetMarkingChangedBinding ??=
                new EventBinding<PlanetMarkingChangedEvent>(HandlePlanetMarkingChanged);
            EventBus<PlanetMarkingChangedEvent>.Register(_planetMarkingChangedBinding);
            _markedPlanetSubscribed = true;
        }

        private void UnsubscribeFromMarkedPlanets()
        {
            if (!_markedPlanetSubscribed)
            {
                return;
            }

            if (_planetMarkingChangedBinding != null)
            {
                EventBus<PlanetMarkingChangedEvent>.Unregister(_planetMarkingChangedBinding);
            }

            _markedPlanetSubscribed = false;
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

            _proximityExitBinding ??= new EventBinding<DetectionExitEvent>(HandleProximityExit);
            EventBus<DetectionExitEvent>.Register(_proximityExitBinding);
            _proximityEventsSubscribed = true;
        }

        private void UnsubscribeFromProximityEvents()
        {
            if (!_proximityEventsSubscribed)
            {
                return;
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
                if (Behavior.ShouldLogStateTransitions)
                {
                    DebugUtility.LogWarning(
                        "EaterDetectionController não encontrado para monitorar saída de proximidade durante a alimentação.",
                        Behavior,
                        this);
                }

                return false;
            }

            _planetProximityDetectionType = _detectionController.PlanetProximityDetectionType;
            if (_planetProximityDetectionType == null)
            {
                if (Behavior.ShouldLogStateTransitions)
                {
                    DebugUtility.LogWarning(
                        "DetectionType de proximidade de planetas não configurado para o Eater em estado de alimentação.",
                        Behavior,
                        this);
                }

                return false;
            }

            return true;
        }

        private void HandleProximityExit(DetectionExitEvent exitEvent)
        {
            if (!IsRelevantProximityEvent(exitEvent.Detector, exitEvent.DetectionType))
            {
                return;
            }

            Transform detectableTransform = ResolveDetectableTransform(exitEvent.Detectable);
            if (detectableTransform == null || !ReferenceEquals(detectableTransform, _currentTarget))
            {
                return;
            }

            if (Behavior.ShouldLogStateTransitions)
            {
                DebugUtility.Log(
                    "Planeta alvo saiu do sensor durante a alimentação. Retomando movimento e retornando à fome.",
                    DebugUtility.Colors.Warning,
                    context: Behavior,
                    instance: this);
            }

            StopTweens();
            ResumeCapturedPlanetOrbit();
            _currentTarget = null;
            RequestHungryFallback("EatingState.ProximityLost");
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

        private static Transform ResolveDetectableTransform(IDetectable detectable)
        {
            return detectable?.Owner?.Transform;
        }

        private void HandlePlanetMarkingChanged(PlanetMarkingChangedEvent @event)
        {
            if (@event.PreviousMarkedPlanet != null)
            {
                StopTweens();
                ResumeCapturedPlanetOrbit();
            }

            if (@event.NewMarkedPlanet != null)
            {
                return;
            }

            RequestHungryFallback("EatingState.PlanetUnmarked");
        }

        private void RequestHungryFallback(string reason)
        {
            if (_pendingHungryFallback)
            {
                return;
            }

            _pendingHungryFallback = true;

            if (!Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            DebugUtility.Log(
                $"Planeta desmarcado durante a alimentação. Solicitando retorno ao estado faminto ({reason}).",
                DebugUtility.Colors.Warning,
                context: Behavior,
                instance: this);
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

        private void ConfigureEatingContext()
        {
            if (Behavior == null)
            {
                return;
            }

            if (!Behavior.TryConsumeEatingCapture(out PlanetMotion planetMotion, out float orbitDistance))
            {
                return;
            }

            _capturedPlanetMotion = planetMotion;

            if (orbitDistance > Mathf.Epsilon)
            {
                _orbitDistanceOverride = orbitDistance;
                _hasOrbitDistanceOverride = true;
            }
        }

        private void ResumeCapturedPlanetOrbit()
        {
            if (_capturedPlanetMotion == null)
            {
                return;
            }

            _capturedPlanetMotion.ResumeMotion();
            _capturedPlanetMotion = null;
        }
    }
}
