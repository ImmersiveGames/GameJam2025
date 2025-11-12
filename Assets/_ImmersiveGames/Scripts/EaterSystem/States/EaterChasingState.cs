using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.EaterSystem.Detections;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Core;
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
        private bool _proximityEventsSubscribed;
        private EventBinding<DetectionEnterEvent> _proximityEnterBinding;
        private EventBinding<DetectionExitEvent> _proximityExitBinding;
        private EaterDetectionController _detectionController;
        private DetectionType _planetProximityDetectionType;
        private bool _haltMovement;
        private Transform _haltedTarget;
        private bool _requestedHungryFallback;
        private readonly PlanetOrbitFreezeHandle _orbitFreezeHandle;

        public EaterChasingState() : base("Chasing")
        {
            _orbitFreezeHandle = new PlanetOrbitFreezeHandle(this);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            SubscribeToProximityEvents();
            _requestedHungryFallback = false;
            EvaluateImmediateProximity();
        }

        public override void OnExit()
        {
            UnsubscribeFromProximityEvents();
            ResetMovementHalt();
            base.OnExit();
        }

        public override void Update()
        {
            base.Update();

            Transform target = Behavior.CurrentTargetPlanet;
            if (target == null)
            {
                ResetMovementHalt();
                TryReturnToHungryState();
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

            if (!EaterPlanetUtility.TryResolveMarkedPlanet(enterEvent.Detectable, out IPlanetActor planetActor, out MarkPlanet markedPlanet))
            {
                return;
            }

            string planetName = EaterPlanetUtility.GetPlanetDisplayName(planetActor);
            DebugUtility.Log(
                $"Planeta {planetName} entrou no detector de proximidade durante a perseguição.",
                DebugUtility.Colors.Info,
                Behavior,
                this);

            Transform planetTransform = markedPlanet.transform;
            if (ReferenceEquals(Behavior.CurrentTargetPlanet, planetTransform))
            {
                HaltChasingTarget(planetTransform);
                TryTransitionToEatingState("ChasingState.ProximityEnter");
            }
        }

        private void HandleProximityExit(DetectionExitEvent exitEvent)
        {
            if (!IsRelevantProximityEvent(exitEvent.Detector, exitEvent.DetectionType))
            {
                return;
            }

            if (!EaterPlanetUtility.TryResolveMarkedPlanet(exitEvent.Detectable, out IPlanetActor planetActor, out MarkPlanet markedPlanet))
            {
                return;
            }

            string planetName = EaterPlanetUtility.GetPlanetDisplayName(planetActor);
            DebugUtility.Log(
                $"Planeta {planetName} saiu do detector de proximidade durante a perseguição.",
                DebugUtility.Colors.Info,
                Behavior,
                this);

            Transform planetTransform = markedPlanet.transform;
            if (ReferenceEquals(_haltedTarget, planetTransform))
            {
                ResumeChasingTarget();
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
            if (!ReferenceEquals(_haltedTarget, target))
            {
                ResetMovementHalt();
                _haltedTarget = target;
            }

            _haltMovement = true;
            _orbitFreezeHandle.Request(target, Behavior != null && Behavior.ShouldLogStateTransitions, Behavior, this);
        }

        private void ResumeChasingTarget()
        {
            ResetMovementHalt();
        }

        private void ResetMovementHalt()
        {
            _orbitFreezeHandle.Release();
            _haltMovement = false;
            _haltedTarget = null;
        }

        private void TryTransitionToEatingState(string reason)
        {
            if (Behavior == null)
            {
                return;
            }

            Behavior.TryEnterEatingState(reason);
        }

        private void EvaluateImmediateProximity()
        {
            if (Behavior == null || !TryResolveProximityDependencies())
            {
                return;
            }

            Transform target = Behavior.CurrentTargetPlanet;
            if (target == null)
            {
                return;
            }

            if (!_detectionController.IsTransformWithinProximity(target))
            {
                return;
            }

            if (Behavior.ShouldLogStateTransitions)
            {
                DebugUtility.Log(
                    $"Planeta {target.name} já estava dentro do sensor ao iniciar perseguição.",
                    DebugUtility.Colors.Info,
                    Behavior,
                    this);
            }

            HaltChasingTarget(target);
            TryTransitionToEatingState("ChasingState.ImmediateProximity");
        }

        private void TryReturnToHungryState()
        {
            if (_requestedHungryFallback || Behavior == null)
            {
                return;
            }

            if (Behavior.TryEnterHungryState("ChasingState.NoMarkedPlanets"))
            {
                _requestedHungryFallback = true;
            }
        }
    }
}
