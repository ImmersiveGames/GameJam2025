using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.EaterSystem.Detections;
using _ImmersiveGames.Scripts.PlanetSystems;
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

        public EaterChasingState() : base("Chasing")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            SubscribeToProximityEvents();
        }

        public override void OnExit()
        {
            UnsubscribeFromProximityEvents();
            base.OnExit();
        }

        public override void Update()
        {
            base.Update();

            Transform target = Behavior.CurrentTargetPlanet;
            if (target == null)
            {
                if (!_reportedMissingTarget && Behavior.ShouldLogStateTransitions)
                {
                    DebugUtility.LogVerbose<EaterChasingState>(
                        "Nenhum planeta marcado para perseguir.",
                        context: Behavior,
                        instance: this);
                    _reportedMissingTarget = true;
                }
                return;
            }

            _reportedMissingTarget = false;

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
                DebugUtility.LogWarning<EaterChasingState>(
                    "EaterDetectionController não encontrado para monitorar eventos de proximidade.",
                    Behavior,
                    this);
                return false;
            }

            _planetProximityDetectionType = _detectionController.PlanetProximityDetectionType;
            if (_planetProximityDetectionType == null)
            {
                DebugUtility.LogWarning<EaterChasingState>(
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

            if (!TryResolvePlanetActor(enterEvent.Detectable, out IPlanetActor planetActor))
            {
                return;
            }

            string planetName = GetPlanetDisplayName(planetActor);
            DebugUtility.Log<EaterChasingState>(
                $"Planeta {planetName} entrou no detector de proximidade durante a perseguição.",
                DebugUtility.Colors.Info,
                Behavior,
                this);
        }

        private void HandleProximityExit(DetectionExitEvent exitEvent)
        {
            if (!IsRelevantProximityEvent(exitEvent.Detector, exitEvent.DetectionType))
            {
                return;
            }

            if (!TryResolvePlanetActor(exitEvent.Detectable, out IPlanetActor planetActor))
            {
                return;
            }

            string planetName = GetPlanetDisplayName(planetActor);
            DebugUtility.Log<EaterChasingState>(
                $"Planeta {planetName} saiu do detector de proximidade durante a perseguição.",
                DebugUtility.Colors.Info,
                Behavior,
                this);
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
