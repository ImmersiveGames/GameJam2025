using _ImmersiveGames.Scripts.PlanetSystems;
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
        private bool _hasTargetContact;

        public EaterChasingState() : base("Chasing")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (Behavior != null)
            {
                Behavior.EventProximityContactChanged += HandleProximityContactChanged;
                _hasTargetContact = Behavior.HasProximityContactForTarget;
            }
        }

        public override void OnExit()
        {
            if (Behavior != null)
            {
                Behavior.EventProximityContactChanged -= HandleProximityContactChanged;
            }

            _hasTargetContact = false;
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

            bool hasProximityContact = _hasTargetContact || Behavior.HasProximityContactForTarget;
            if (hasProximityContact)
            {
                Behavior.RotateTowards(toTarget, Time.deltaTime);
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
            float sampleSpeed = travelDistance / Mathf.Max(Time.deltaTime, Mathf.Epsilon);
            Behavior.RecordMovement(direction, sampleSpeed);
        }

        private void HandleProximityContactChanged(PlanetsMaster planet, bool active)
        {
            if (Behavior == null)
            {
                return;
            }

            _hasTargetContact = Behavior.HasProximityContactForTarget;

            if (!Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            if (planet == null || !Behavior.IsCurrentTarget(planet))
            {
                DebugUtility.LogVerbose<EaterChasingState>(
                    "Evento de proximidade recebido para planeta não-alvo.",
                    context: Behavior,
                    instance: this);
                return;
            }

            string planetName = !string.IsNullOrEmpty(planet.ActorName) ? planet.ActorName : planet.name;
            string state = active ? "alcançado" : "perdido";
            DebugUtility.Log<EaterChasingState>(
                $"Sensor de proximidade {state} para o alvo {planetName}.",
                DebugUtility.Colors.CrucialInfo,
                context: Behavior,
                instance: this);
        }
    }
}
