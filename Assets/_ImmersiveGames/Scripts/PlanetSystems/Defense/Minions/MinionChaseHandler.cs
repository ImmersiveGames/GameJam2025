using System;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions.Strategy;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions
{
    /// <summary>
    /// Responsável apenas pela perseguição do alvo após a fase de órbita.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinionChaseHandler : MonoBehaviour
    {
        public enum ChaseStopReason
        {
            LostTarget,
            Completed,
            Cancelled
        }

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

        // Configuração de rotação aplicada externamente pelo controller/perfil.
        private bool _snapFacingOnChaseStart = true;
        private float _rotationLerpFactor = 0.2f;

        private void OnDisable()
        {
            CancelChase();
        }

        public void ConfigureRotation(bool snapFacingOnChaseStart, float rotationLerpFactor)
        {
            _snapFacingOnChaseStart = snapFacingOnChaseStart;
            _rotationLerpFactor = Mathf.Clamp01(rotationLerpFactor);
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

            if (_snapFacingOnChaseStart && _targetTransform != null)
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
                    if (direction.sqrMagnitude > 0.0001f && _rotationLerpFactor > 0f)
                    {
                        transform.forward = Vector3.Lerp(
                            transform.forward,
                            direction.normalized,
                            _rotationLerpFactor);
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
