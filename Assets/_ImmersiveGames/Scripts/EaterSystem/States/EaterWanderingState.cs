using _ImmersiveGames.Scripts.Utils.DebugSystems;
using ImprovedTimers;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Estado responsável por manter o eater vagando de maneira aleatória,
    /// respeitando limites mínimos e máximos de distância em relação aos jogadores.
    /// </summary>
    internal sealed class EaterWanderingState : EaterMoveState
    {
        private const float MinimumHungryInterval = 0.05f;

        private CountdownTimer _hungryCountdown;
        private bool _pendingHungryTransition;

        public EaterWanderingState() : base("Wandering")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Behavior?.SuspendDesires("WanderingState.OnEnter");
            RestartHungryCountdown();
            RestartMovement(); // Reinicia o deslocamento sempre que o estado de passeio é reativado.
        }

        public override void OnExit()
        {
            base.OnExit();
            StopHungryCountdown();
        }

        public override void Update()
        {
            base.Update();
            UpdateHungryCountdown();
        }

        protected override float EvaluateSpeed()
        {
            return Behavior.GetRandomRoamingSpeed();
        }

        protected override Vector3 AdjustDirection(Vector3 direction)
        {
            if (!Behavior.TryGetClosestPlayerAnchor(out Vector3 anchor, out float distance))
            {
                return direction;
            }

            Vector3 toAnchor = anchor - Transform.position;
            if (toAnchor.sqrMagnitude <= Mathf.Epsilon)
            {
                return direction;
            }

            float maxDistance = Config?.WanderingMaxDistanceFromPlayer ?? 0f;
            float minDistance = Config?.WanderingMinDistanceFromPlayer ?? 0f;

            if (maxDistance > 0f && distance > maxDistance)
            {
                return toAnchor.normalized;
            }

            if (minDistance > 0f && distance < minDistance)
            {
                return (-toAnchor).normalized;
            }

            float bias = Mathf.Clamp01(Config?.WanderingReturnBias ?? 0f);
            if (bias <= 0f)
            {
                return direction;
            }

            Vector3 normalized = toAnchor.normalized;
            Vector3 blended = Vector3.Slerp(direction, normalized, bias);
            return blended.sqrMagnitude > Mathf.Epsilon ? blended.normalized : normalized;
        }

        protected override void OnDirectionChosen(Vector3 direction, float speed, bool force)
        {
            base.OnDirectionChosen(direction, speed, force);

            if (!Behavior.ShouldLogStateTransitions)
            {
                return;
            }

            DebugUtility.Log(
                $"Nova direção de passeio: {direction} | velocidade={speed:F2}",
                DebugUtility.Colors.CrucialInfo,
                context: Behavior,
                instance: this);
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

        private void RestartHungryCountdown()
        {
            _pendingHungryTransition = false;

            if (Config == null)
            {
                StopHungryCountdown();
                return;
            }

            float duration = Mathf.Max(Config.WanderingHungryDelay, 0f);
            if (duration <= Mathf.Epsilon)
            {
                StopHungryCountdown();
                _pendingHungryTransition = true;
                return;
            }

            float safeDuration = Mathf.Max(duration, MinimumHungryInterval);
            StopHungryCountdown();
            _hungryCountdown = new CountdownTimer(safeDuration);
            _hungryCountdown.Start();
        }

        private void StopHungryCountdown()
        {
            if (_hungryCountdown == null)
            {
                return;
            }

            _hungryCountdown.Stop();
            _hungryCountdown = null;
        }

        private void UpdateHungryCountdown()
        {
            if (_pendingHungryTransition || _hungryCountdown == null)
            {
                return;
            }

            if (_hungryCountdown.IsRunning)
            {
                // Garante que o cronômetro avance a cada frame enquanto o eater está vagando.
                _hungryCountdown.Tick();
            }

            if (!_hungryCountdown.IsFinished)
            {
                return;
            }

            _pendingHungryTransition = true;
            StopHungryCountdown();
        }
    }
}
