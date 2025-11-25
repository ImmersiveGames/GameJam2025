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
        private CountdownTimer _hungryCountdown;
        private bool _pendingHungryTransition;
        private float _hungryDelayDeadline;
        private bool _hungryDelayActive;

        public EaterWanderingState() : base("Wandering")
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _hungryDelayActive = false;
            _hungryDelayDeadline = 0f;
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
            EvaluateHungryCountdown();
        }

        protected override float EvaluateSpeed()
        {
            return Behavior.GetRandomRoamingSpeed();
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

            var normalized = toAnchor.normalized;
            var blended = Vector3.Slerp(direction, normalized, bias);
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
            _hungryDelayActive = false;
            _hungryDelayDeadline = 0f;

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

            EnsureHungryCountdown(duration);

            float safeDuration = Mathf.Max(duration, 0.05f);
            _hungryCountdown.Stop();
            _hungryCountdown.Reset(safeDuration);
            _hungryCountdown.Start();

            _hungryDelayActive = true;
            _hungryDelayDeadline = Time.time + safeDuration;
        }

        private void StopHungryCountdown()
        {
            if (_hungryCountdown == null)
            {
                _hungryDelayActive = false;
                _hungryDelayDeadline = 0f;
                return;
            }

            _hungryCountdown.Stop();
            _hungryCountdown = null;
            _hungryDelayActive = false;
            _hungryDelayDeadline = 0f;
        }

        private void EnsureHungryCountdown(float duration)
        {
            if (_hungryCountdown != null)
            {
                return;
            }

            float safeDuration = Mathf.Max(duration, 0.05f);
            _hungryCountdown = new CountdownTimer(safeDuration);
            _hungryCountdown.Stop();
        }

        private void EvaluateHungryCountdown()
        {
            if (_pendingHungryTransition)
            {
                return;
            }

            bool waitingWithTimer = _hungryCountdown != null;
            bool timerFinished = waitingWithTimer && !_hungryCountdown.IsRunning && _hungryCountdown.IsFinished;
            bool deadlineReached = _hungryDelayActive && Time.time >= _hungryDelayDeadline;

            if (!timerFinished && !deadlineReached)
            {
                return;
            }

            _pendingHungryTransition = true;
            StopHungryCountdown();
        }
    }
}
