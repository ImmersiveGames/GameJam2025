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
        private Tween _chaseTween;

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
            Action onCompleted)
        {
            CancelChase();

            if (targetTransform == null)
            {
                DebugUtility.LogVerbose<MinionChaseHandler>(
                    $"[Chase] {name} iniciado sem alvo concreto. Role: {targetRole} | Label: '{targetLabel}'.");

                _chaseTween = transform.DOMove(transform.position + transform.forward * 5f, 2f)
                                       .SetEase(Ease.Linear)
                                       .OnComplete(() =>
                                       {
                                           DebugUtility.LogVerbose<MinionChaseHandler>(
                                               $"[Chase] {name} concluiu perseguição fake sem alvo concreto.");
                                           onCompleted?.Invoke();
                                           _chaseTween = null;
                                       });
                return;
            }

            DebugUtility.LogVerbose<MinionChaseHandler>(
                $"[Chase] {name} iniciou perseguição real a '{targetLabel}' (Role: {targetRole}) em posição {targetTransform.position}.");

            if (chaseStrategy != null)
            {
                _chaseTween = chaseStrategy.CreateChaseTween(
                    transform,
                    targetTransform,
                    chaseSpeed,
                    targetLabel);
            }
            else
            {
                float distance = Vector3.Distance(transform.position, targetTransform.position);
                float duration = distance / Mathf.Max(0.01f, chaseSpeed);

                _chaseTween = transform.DOMove(targetTransform.position, duration)
                                       .SetEase(Ease.Linear);
            }

            if (_chaseTween != null)
            {
                _chaseTween.OnUpdate(() =>
                {
                    var dir = (targetTransform.position - transform.position);
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        dir.Normalize();
                        transform.forward = Vector3.Lerp(transform.forward, dir, 0.2f);
                    }
                });

                _chaseTween.OnComplete(() =>
                {
                    DebugUtility.LogVerbose<MinionChaseHandler>(
                        $"[Chase] {name} concluiu Tween de perseguição a '{targetLabel}'. Posição final: {transform.position}.");

                    onCompleted?.Invoke();
                    _chaseTween = null;
                });
            }
            else
            {
                DebugUtility.LogVerbose<MinionChaseHandler>(
                    $"[Chase] {name} não conseguiu criar Tween de perseguição para alvo '{targetLabel}'.");
                onCompleted?.Invoke();
            }
        }

        public void CancelChase()
        {
            if (_chaseTween != null && _chaseTween.IsActive())
            {
                _chaseTween.Kill();
            }

            _chaseTween = null;
        }
    }
}
