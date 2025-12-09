using System;
using DG.Tweening;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Responsável apenas por aguardar o tempo de idle em órbita antes de liberar a perseguição.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinionOrbitWaitHandler : MonoBehaviour
    {
        private Tween _orbitWaitTween;

        private void OnDisable()
        {
            CancelOrbitWait();
        }

        public void BeginOrbitWait(float orbitIdleDelaySeconds, Action onCompleted)
        {
            CancelOrbitWait();

            if (orbitIdleDelaySeconds <= 0f)
            {
                onCompleted?.Invoke();
                return;
            }

            _orbitWaitTween = DOVirtual.DelayedCall(orbitIdleDelaySeconds, () => onCompleted?.Invoke())
                                      .SetRecyclable(true);
        }

        public void CancelOrbitWait()
        {
            if (_orbitWaitTween != null && _orbitWaitTween.IsActive())
            {
                _orbitWaitTween.Kill();
            }

            _orbitWaitTween = null;
        }
    }
}
