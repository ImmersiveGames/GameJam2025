using System;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using DG.Tweening;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Responsável apenas pela perseguição do alvo após a fase de órbita.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class MinionChaseHandler : MonoBehaviour
    {
        public enum ChaseStopReason
        {
            LostTarget,
            Completed,
            Cancelled
        }

        [Header("Rotação / Facing")]
        [Tooltip("Fator de interpolação da rotação durante a perseguição (0 = não gira, 1 = vira instantaneamente).")]
        [SerializeField, Range(0f, 1f)]
        private float rotationLerpFactor = 0.2f;

        [Tooltip("Se verdadeiro, quando a perseguição começa o minion já alinha o forward diretamente para o alvo.")]
        [SerializeField]
        private bool snapFacingOnChaseStart = true;

        private Tween _chaseTween;
        private Transform _targetTransform;
        private string _targetLabel;
        private DefenseRole _targetRole;
        private float _chaseSpeed;
        private MinionChaseStrategySo _chaseStrategy;
        private Action<ChaseStopReason> _onChaseStopped;
        private Func<Transform> _reacquireTarget;
        private bool _isChasing;
        private Vector3 _lastTargetPosition;

        private void OnDisable()
        {
            CancelChase();
        }

        public void BeginChase(
            Transform targetTransform,
            string targetLabel,
            DefenseRole targetRole,
            float chaseSpeed,
            MinionChaseStrategySo chaseStrategy,
            Func<Transform> targetResolver,
            Action<ChaseStopReason> onChaseStopped)
        {
            CancelChase();

            _targetTransform = targetTransform;
            _targetLabel = targetLabel;
            _targetRole = targetRole;
            _chaseSpeed = Mathf.Max(0.01f, chaseSpeed);
            _chaseStrategy = chaseStrategy;
            _onChaseStopped = onChaseStopped;
            _reacquireTarget = targetResolver;
            _isChasing = true;

            if (snapFacingOnChaseStart && _targetTransform != null)
            {
                var dir = _targetTransform.position - transform.position;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    transform.forward = dir.normalized;
                }
            }

            DebugUtility.LogVerbose<MinionChaseHandler>(
                $"[Chase] {name} iniciou perseguição ativa a '{_targetLabel}' (Role: {_targetRole}).");

            RestartChaseTween();
        }

        private void RestartChaseTween()
        {
            if (!_isChasing)
            {
                return;
            }

            CancelChaseTweenOnly();

            if (!TryEnsureTarget())
            {
                StopChase(ChaseStopReason.LostTarget);
                return;
            }

            var currentTarget = _targetTransform;
            if (currentTarget == null)
            {
                StopChase(ChaseStopReason.LostTarget);
                return;
            }

            _lastTargetPosition = currentTarget.position;

            if (_chaseStrategy != null)
            {
                _chaseTween = _chaseStrategy.CreateChaseTween(
                    transform,
                    currentTarget,
                    _chaseSpeed,
                    _targetLabel);
            }
            else
            {
                _chaseTween = transform.DOMove(currentTarget.position, _chaseSpeed)
                                       .SetSpeedBased(true)
                                       .SetEase(Ease.Linear)
                                       .SetRecyclable(true);
            }

            if (_chaseTween == null)
            {
                DebugUtility.LogVerbose<MinionChaseHandler>(
                    $"[Chase] {name} não conseguiu criar Tween para alvo '{_targetLabel}'. Finalizando perseguição.");
                StopChase(ChaseStopReason.Cancelled);
                return;
            }

            _chaseTween.SetRecyclable(true);

            _chaseTween.OnUpdate(() =>
            {
                if (!_isChasing)
                {
                    return;
                }

                if (_targetTransform == null && !TryEnsureTarget())
                {
                    StopChase(ChaseStopReason.LostTarget);
                    return;
                }

                if (_targetTransform != null)
                {
                    if ((_targetTransform.position - _lastTargetPosition).sqrMagnitude > 0.0001f)
                    {
                        RestartChaseTween();
                        return;
                    }

                    var direction = (_targetTransform.position - transform.position);
                    if (direction.sqrMagnitude > 0.0001f && rotationLerpFactor > 0f)
                    {
                        transform.forward = Vector3.Lerp(
                            transform.forward,
                            direction.normalized,
                            rotationLerpFactor);
                    }
                }
            });

            _chaseTween.OnComplete(() =>
            {
                if (!_isChasing)
                {
                    return;
                }

                DebugUtility.LogVerbose<MinionChaseHandler>(
                    $"[Chase] {name} concluiu etapa de perseguição para '{_targetLabel}'. Reiniciando enquanto ativo.");

                RestartChaseTween();
            });
        }

        private bool TryEnsureTarget()
        {
            if (_targetTransform != null)
            {
                return true;
            }

            _targetTransform = _reacquireTarget?.Invoke();
            return _targetTransform != null;
        }

        public void CancelChase()
        {
            _isChasing = false;
            CancelChaseTweenOnly();
            ClearRuntimeState();
        }

        private void CancelChaseTweenOnly()
        {
            if (_chaseTween != null && _chaseTween.IsActive())
            {
                _chaseTween.Kill();
            }

            _chaseTween = null;
        }

        private void StopChase(ChaseStopReason reason)
        {
            _isChasing = false;
            CancelChaseTweenOnly();
            _onChaseStopped?.Invoke(reason);
            ClearRuntimeState();
        }

        private void ClearRuntimeState()
        {
            _targetTransform = null;
            _targetLabel = null;
            _targetRole = DefenseRole.Unknown;
            _chaseStrategy = null;
            _onChaseStopped = null;
            _reacquireTarget = null;
            _lastTargetPosition = Vector3.zero;
        }
    }
}
