using System;
using DG.Tweening;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Responsável apenas por aguardar o tempo de idle em órbita antes de liberar a perseguição.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
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

            _orbitWaitTween = DOVirtual.DelayedCall(orbitIdleDelaySeconds, () => onCompleted?.Invoke());
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
