using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.EaterSystem.Detections;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado de perseguição: o eater avança na direção do planeta marcado via PlanetMarkingManager.
    /// </summary>
    internal sealed class EaterChasingState : EaterBehaviorState
    {
        private bool _reportedMissingTarget;
        private bool _pendingHungryFallback;
        private bool _markedPlanetSubscribed;
        private bool _proximityEventsSubscribed;
        private EventBinding<DetectionEnterEvent> _proximityEnterBinding;
        private EventBinding<DetectionExitEvent> _proximityExitBinding;
        private EventBinding<PlanetMarkingChangedEvent> _planetMarkingChangedBinding;
        private EaterDetectionController _detectionController;
        private DetectionType _planetProximityDetectionType;
        private bool _haltMovement;
        private Transform _haltedTarget;
        private bool _pendingEatingTransition;
        private PlanetMotion _pausedPlanetMotion;
        private float _capturedOrbitDistance;

        public EaterChasingState() : base("Chasing")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _pendingHungryFallback = false;
            SubscribeToProximityEvents();
            SubscribeToMarkedPlanets();
            _pendingEatingTransition = false;
            _pausedPlanetMotion = null;
            _capturedOrbitDistance = 0f;
        }

        public override void OnExit()
        {
            UnsubscribeFromProximityEvents();
            UnsubscribeFromMarkedPlanets();
            ResetMovementHalt();
            _pendingHungryFallback = false;
            CancelEatingTransition("ChasingState.Exit", log: false);
            base.OnExit();
        }

        public override void Update()
        {
            base.Update();

            Transform target = Behavior.CurrentTargetPlanet;
            if (target == null)
            {
                RequestHungryFallback("ChasingState.TargetMissing");
                ResetMovementHalt();
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

            if (_haltMovement)
            {
                if (ReferenceEquals(target, _haltedTarget))
                {
                    Behavior.LookAt(target.position);
                    return;
                }

                ResetMovementHalt();
            }

            Vector3 toTarget = target.position - Transform.position;
            float distance = toTarget.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return;
            }

            float stopDistance = Mathf.Max(Config?.MinimumChaseDistance ?? 0f, 0f);
            if (stopDistance > 0f && distance <= stopDistance)
            {
                Behavior.LookAt(target.position);
                return;
            }

            Vector3 direction = toTarget.normalized;
            float speed = Behavior.GetChaseSpeed();
            float travelDistance = speed * Time.deltaTime;

            if (stopDistance > 0f)
            {
                float remaining = Mathf.Max(distance - stopDistance, 0f);
                travelDistance = Mathf.Min(travelDistance, remaining);
            }

            if (travelDistance <= 0f)
            {
                return;
            }

            Behavior.RotateTowards(direction, Time.deltaTime);
            Behavior.Translate(direction * travelDistance, respectPlayerBounds: false);
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

        private void HandlePlanetMarkingChanged(PlanetMarkingChangedEvent @event)
        {
            if (@event.PreviousMarkedPlanet != null)
            {
                ResetMovementHalt();
                CancelEatingTransition("ChasingState.MarkingChanged", log: false);
            }

            if (@event.NewMarkedPlanet != null)
            {
                return;
            }

            RequestHungryFallback("ChasingState.PlanetUnmarked");
        }

        private void RequestHungryFallback(string reason)
        {
            CancelEatingTransition(reason, log: false);
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
                $"Planeta desmarcado durante a perseguição. Solicitando retorno ao estado faminto ({reason}).",
                DebugUtility.Colors.Warning,
                context: Behavior,
                instance: this);
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
                    "EaterDetectionController não encontrado para monitorar eventos de proximidade.",
                    Behavior,
                    this);
                return false;
            }

            _planetProximityDetectionType = _detectionController.PlanetProximityDetectionType;
            if (_planetProximityDetectionType == null)
            {
                DebugUtility.LogWarning(
                    "DetectionType de proximidade de planetas não configurado para o Eater.",
                    Behavior,
                    this);
                return false;
            }

            return true;
        }

        private void HandleProximityEnter(DetectionEnterEvent enterEvent)
        {
            if (!IsRelevantProximityEvent(enterEvent.Detector, enterEvent.DetectionType))
            {
                return;
            }

            if (!TryResolveMarkedPlanet(enterEvent.Detectable, out IPlanetActor planetActor, out MarkPlanet markedPlanet))
            {
                return;
            }

            string planetName = GetPlanetDisplayName(planetActor);
            DebugUtility.Log(
                $"Planeta {planetName} entrou no detector de proximidade durante a perseguição.",
                DebugUtility.Colors.Info,
                Behavior,
                this);

            Transform planetTransform = markedPlanet.transform;
            if (ReferenceEquals(Behavior.CurrentTargetPlanet, planetTransform))
            {
                OnTargetProximityReached(planetTransform, planetActor);
            }
        }

        private void HandleProximityExit(DetectionExitEvent exitEvent)
        {
            if (!IsRelevantProximityEvent(exitEvent.Detector, exitEvent.DetectionType))
            {
                return;
            }

            if (!TryResolveMarkedPlanet(exitEvent.Detectable, out IPlanetActor planetActor, out MarkPlanet markedPlanet))
            {
                return;
            }

            string planetName = GetPlanetDisplayName(planetActor);
            DebugUtility.Log(
                $"Planeta {planetName} saiu do detector de proximidade durante a perseguição.",
                DebugUtility.Colors.Info,
                Behavior,
                this);

            Transform planetTransform = markedPlanet.transform;
            if (ReferenceEquals(_haltedTarget, planetTransform))
            {
                ResumeChasingTarget();
                CancelEatingTransition("ChasingState.ProximityLost", log: false);
            }
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

        private void HaltChasingTarget(Transform target)
        {
            _haltMovement = true;
            _haltedTarget = target;
        }

        private void ResumeChasingTarget()
        {
            ResetMovementHalt();
        }

        private void ResetMovementHalt()
        {
            _haltMovement = false;
            _haltedTarget = null;
        }

        private bool TryResolveMarkedPlanet(IDetectable detectable, out IPlanetActor planetActor, out MarkPlanet markedPlanet)
        {
            markedPlanet = null;

            if (!TryResolvePlanetActor(detectable, out planetActor))
            {
                return false;
            }

            return TryResolveMarkPlanet(planetActor, out markedPlanet);
        }

        private static bool TryResolvePlanetActor(IDetectable detectable, out IPlanetActor planetActor)
        {
            planetActor = null;

            if (detectable == null)
            {
                return false;
            }

            if (detectable.Owner is IPlanetActor ownerPlanetActor)
            {
                planetActor = ownerPlanetActor;
                return true;
            }

            if (detectable is Component component)
            {
                if (component.TryGetComponent(out ownerPlanetActor))
                {
                    planetActor = ownerPlanetActor;
                    return true;
                }

                ownerPlanetActor = component.GetComponentInParent<IPlanetActor>();
                if (ownerPlanetActor != null)
                {
                    planetActor = ownerPlanetActor;
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveMarkPlanet(IPlanetActor planetActor, out MarkPlanet markedPlanet)
        {
            markedPlanet = null;

            Transform planetTransform = planetActor?.PlanetActor?.Transform;
            if (planetTransform == null)
            {
                return false;
            }

            if (!planetTransform.TryGetComponent(out markedPlanet))
            {
                markedPlanet = planetTransform.GetComponentInParent<MarkPlanet>();
            }

            if (markedPlanet == null)
            {
                return false;
            }

            return markedPlanet.IsMarked;
        }

        private void OnTargetProximityReached(Transform planetTransform, IPlanetActor planetActor)
        {
            if (planetTransform == null)
            {
                return;
            }

            HaltChasingTarget(planetTransform);

            float distance = Vector3.Distance(Transform.position, planetTransform.position);
            _capturedOrbitDistance = Mathf.Max(distance, 0f);

            PausePlanetOrbit(planetTransform, planetActor);
            RequestEatingTransition();
        }

        private void PausePlanetOrbit(Transform planetTransform, IPlanetActor planetActor)
        {
            if (planetTransform == null)
            {
                return;
            }

            PlanetMotion planetMotion = ResolvePlanetMotion(planetTransform);
            if (planetMotion == null)
            {
                if (Behavior.ShouldLogStateTransitions)
                {
                    string planetName = GetPlanetDisplayName(planetActor);
                    DebugUtility.LogWarning(
                        $"Planeta {planetName} não possui PlanetMotion para pausar órbita.",
                        Behavior,
                        this);
                }

                _pausedPlanetMotion = null;
                return;
            }

            if (!planetMotion.IsMotionPaused)
            {
                planetMotion.PauseMotion();
            }

            _pausedPlanetMotion = planetMotion;
        }

        private static PlanetMotion ResolvePlanetMotion(Transform planetTransform)
        {
            if (planetTransform == null)
            {
                return null;
            }

            if (!planetTransform.TryGetComponent(out PlanetMotion planetMotion))
            {
                planetMotion = planetTransform.GetComponentInParent<PlanetMotion>();
            }

            return planetMotion;
        }

        private void RequestEatingTransition()
        {
            if (_pendingEatingTransition)
            {
                return;
            }

            _pendingEatingTransition = true;

            if (Behavior.ShouldLogStateTransitions)
            {
                DebugUtility.Log(
                    "Planeta alvo alcançado. Preparando transição para o estado de alimentação.",
                    DebugUtility.Colors.Info,
                    context: Behavior,
                    instance: this);
            }
        }

        internal bool ConsumeEatingTransitionRequest()
        {
            if (!_pendingEatingTransition)
            {
                return false;
            }

            Behavior.RegisterEatingCapture(_pausedPlanetMotion, _capturedOrbitDistance);

            _pendingEatingTransition = false;
            _pausedPlanetMotion = null;
            _capturedOrbitDistance = 0f;
            return true;
        }

        private void CancelEatingTransition(string reason, bool log)
        {
            if (!_pendingEatingTransition && _pausedPlanetMotion == null)
            {
                return;
            }

            if (_pausedPlanetMotion != null)
            {
                _pausedPlanetMotion.ResumeMotion();
                _pausedPlanetMotion = null;
            }

            _capturedOrbitDistance = 0f;
            _pendingEatingTransition = false;
            Behavior.ClearEatingCapture();

            if (log && Behavior.ShouldLogStateTransitions)
            {
                DebugUtility.Log(
                    $"Transição para alimentação cancelada ({reason}).",
                    DebugUtility.Colors.Warning,
                    context: Behavior,
                    instance: this);
            }
        }

        private static string GetPlanetDisplayName(IPlanetActor planetActor)
        {
            if (planetActor?.PlanetActor == null)
            {
                return "desconhecido";
            }

            string actorName = planetActor.PlanetActor.ActorName;
            if (!string.IsNullOrWhiteSpace(actorName))
            {
                return actorName;
            }

            Transform transform = planetActor.PlanetActor.Transform;
            return transform != null ? transform.name : "desconhecido";
        }
    }
}
