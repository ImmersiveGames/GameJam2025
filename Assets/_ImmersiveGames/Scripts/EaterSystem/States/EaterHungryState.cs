using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado em que o eater busca manter-se mais próximo dos jogadores, mas ainda com movimento aleatório.
    /// </summary>
    internal sealed class EaterHungryState : EaterMoveState
    {
        private bool _listeningDesires;
        private bool _listeningMarkedPlanets;
        private bool _pendingChasingTransition;
        private bool _desiresSuspended;
        private EventBinding<PlanetMarkingChangedEvent> _planetMarkingChangedBinding;

        public EaterHungryState() : base("Hungry")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _pendingChasingTransition = false;
            _desiresSuspended = false;
            if (Behavior != null)
            {
                Behavior.EventDesireChanged += HandleDesireChanged;
                _listeningDesires = true;

                // Retoma o ciclo mantendo o último desejo selecionado (quando houver),
                // garantindo que o atraso inicial e os temporizadores reiniciem a partir
                // do estado previamente armazenado.
                Behavior.BeginDesires("HungryState.OnEnter");
            }

            SubscribeToMarkedPlanets();
            EvaluateChasingOpportunity("HungryState.OnEnter");
            Behavior?.ResumeAutoFlow("HungryState.OnEnter");
            RestartMovement(); // Garante que o deslocamento seja retomado ao entrar no estado de fome.
        }

        public override void OnExit()
        {
            if (_listeningDesires && Behavior != null)
            {
                Behavior.EventDesireChanged -= HandleDesireChanged;
                _listeningDesires = false;
            }

            UnsubscribeFromMarkedPlanets();
            _pendingChasingTransition = false;
            _desiresSuspended = false;
            Behavior?.PauseAutoFlow("HungryState.OnExit");
            base.OnExit();
        }

        protected override float DirectionInterval => Mathf.Max(base.DirectionInterval * 0.5f, 0.1f);

        protected override float EvaluateSpeed()
        {
            float min = Config?.MinSpeed ?? 0f;
            float max = Config?.MaxSpeed ?? min;
            float baseSpeed = Mathf.Max(min, max);
            return baseSpeed * 0.75f;
        }

        protected override Vector3 AdjustDirection(Vector3 direction)
        {
            if (!Behavior.TryGetClosestPlayerAnchor(out var anchor, out float distance))
            {
                return direction;
            }

            var toAnchor = anchor - Transform.position;
            if (toAnchor.sqrMagnitude <= Mathf.Epsilon)
            {
                return direction;
            }

            float maxDistance = Config?.WanderingMaxDistanceFromPlayer ?? 0f;
            float minDistance = Config?.WanderingMinDistanceFromPlayer ?? 0f;

            if (minDistance > 0f && distance < minDistance)
            {
                return (-toAnchor).normalized;
            }

            if (maxDistance > 0f && distance > maxDistance)
            {
                return toAnchor.normalized;
            }

            float attraction = Mathf.Clamp01(Config?.HungryPlayerAttraction ?? 0.75f);
            if (maxDistance > 0f && distance > 0f)
            {
                float normalizedDistance = Mathf.Clamp01(distance / maxDistance);
                attraction = Mathf.Clamp01(attraction + (1f - attraction) * (1f - normalizedDistance));
            }

            var normalized = toAnchor.normalized;
            var blended = Vector3.Slerp(direction, normalized, attraction);
            return blended.sqrMagnitude > Mathf.Epsilon ? blended.normalized : normalized;
        }

        protected override void OnDirectionChosen(Vector3 direction, float speed, bool force)
        {
            base.OnDirectionChosen(direction, speed, force);

            if (!Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            DebugUtility.LogVerbose(
                $"Nova direção faminta: {direction} | velocidade={speed:F2}",
                DebugUtility.Colors.CrucialInfo,
                context: Behavior,
                instance: this);
        }

        private void HandleDesireChanged(EaterDesireInfo info)
        {
            if (!info.HasDesire)
            {
                _pendingChasingTransition = false;
                return;
            }

            EvaluateChasingOpportunity("HungryState.DesireChanged");

            if (!Behavior.ShouldLogStateTransitions || !info.TryGetResource(out var resource))
            {
                return;
            }

            string availability = info.IsAvailable ? "disponível" : "indisponível";
            DebugUtility.Log(
                $"Novo desejo selecionado: {resource} ({availability}, planetas={info.AvailableCount}, duração={info.Duration:F2}s)",
                DebugUtility.Colors.CrucialInfo,
                context: Behavior,
                instance: this);
        }

        internal bool ConsumeChasingTransitionRequest()
        {
            if (!_pendingChasingTransition)
            {
                return false;
            }

            _pendingChasingTransition = false;
            return true;
        }

        private void EvaluateChasingOpportunity(string reason)
        {
            if (_pendingChasingTransition || Behavior == null)
            {
                return;
            }

            var desireInfo = Behavior.GetCurrentDesireInfo();
            if (!desireInfo.HasDesire)
            {
                return;
            }

            var target = Behavior.CurrentTargetPlanet;
            if (target == null)
            {
                return;
            }

            RequestChasingTransition(reason, desireInfo, target);
        }

        private void RequestChasingTransition(string reason, EaterDesireInfo desireInfo, Transform target)
        {
            if (_pendingChasingTransition)
            {
                return;
            }

            SuspendDesires(reason);
            _pendingChasingTransition = true;

            if (!Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            string planetName = target != null ? target.name : "desconhecido";
            string resourceLabel = desireInfo.TryGetResource(out var resource)
                ? resource.ToString()
                : "recurso indefinido";

            DebugUtility.Log(
                $"Desejo ativo ({resourceLabel}) alinhado a planeta marcado ({planetName}). Solicitando transição para perseguição ({reason}).",
                DebugUtility.Colors.CrucialInfo,
                context: Behavior,
                instance: this);
        }

        private void SubscribeToMarkedPlanets()
        {
            if (_listeningMarkedPlanets)
            {
                return;
            }

            _planetMarkingChangedBinding ??= new EventBinding<PlanetMarkingChangedEvent>(HandlePlanetMarkingChanged);
            EventBus<PlanetMarkingChangedEvent>.Register(_planetMarkingChangedBinding);
            _listeningMarkedPlanets = true;
        }

        private void UnsubscribeFromMarkedPlanets()
        {
            if (!_listeningMarkedPlanets)
            {
                return;
            }

            if (_planetMarkingChangedBinding != null)
            {
                EventBus<PlanetMarkingChangedEvent>.Unregister(_planetMarkingChangedBinding);
            }

            _listeningMarkedPlanets = false;
        }

        private void HandlePlanetMarkingChanged(PlanetMarkingChangedEvent @event)
        {
            if (@event.NewMarkedPlanet == null)
            {
                return;
            }

            EvaluateChasingOpportunity("HungryState.PlanetMarked");
        }

        private void SuspendDesires(string reason)
        {
            if (_desiresSuspended || Behavior == null)
            {
                return;
            }

            bool suspended = Behavior.SuspendDesires(reason);
            if (suspended)
            {
                _desiresSuspended = true;
            }
        }
    }
}
