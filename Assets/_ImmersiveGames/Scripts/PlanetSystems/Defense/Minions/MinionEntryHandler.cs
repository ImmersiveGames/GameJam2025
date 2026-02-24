using System;
using _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions.Strategy;
using _ImmersiveGames.NewScripts.Core.Logging;
using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions
{
    /// <summary>
    /// Isola a responsabilidade de animação de entrada do minion, do centro do planeta até a órbita.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinionEntryHandler : MonoBehaviour
    {
        private Sequence _entrySequence;

        private void OnDisable()
        {
            CancelEntry();
        }

        public void BeginEntry(
            Vector3 planetCenter,
            Vector3 orbitPosition,
            Vector3 finalScale,
            float entryDurationSeconds,
            float initialScaleFactor,
            MinionEntryStrategySo entryStrategy,
            Action onEntryCompleted)
        {
            CancelEntry();

            if (entryStrategy != null)
            {
                _entrySequence = entryStrategy.BuildEntrySequence(
                    transform,
                    planetCenter,
                    orbitPosition,
                    finalScale,
                    entryDurationSeconds,
                    initialScaleFactor,
                    0f, // Idle agora é responsabilidade do MinionOrbitWaitHandler
                    () => CompleteEntry(finalScale, orbitPosition, onEntryCompleted));

                if (_entrySequence != null)
                {
                    return;
                }

                DebugUtility.LogWarning<MinionEntryHandler>(
                    $"[Entry] Estratégia '{entryStrategy.name}' retornou Sequence nula. Usando fallback DEFAULT.");
            }

            BuildDefaultEntrySequence(
                planetCenter,
                orbitPosition,
                finalScale,
                entryDurationSeconds,
                initialScaleFactor,
                () => CompleteEntry(finalScale, orbitPosition, onEntryCompleted));
        }

        public void CancelEntry()
        {
            if (_entrySequence != null && _entrySequence.IsActive())
            {
                _entrySequence.Kill();
            }

            _entrySequence = null;
        }

        private void CompleteEntry(Vector3 finalScale, Vector3 orbitPosition, Action onEntryCompleted)
        {
            transform.position = orbitPosition;
            transform.localScale = finalScale;
            onEntryCompleted?.Invoke();
        }

        private void BuildDefaultEntrySequence(
            Vector3 planetCenter,
            Vector3 orbitPosition,
            Vector3 finalScale,
            float entryDurationSeconds,
            float initialScaleFactor,
            Action onCompleted)
        {
            var tinyScale = finalScale * initialScaleFactor;

            transform.position = planetCenter;
            transform.localScale = tinyScale;

            _entrySequence = DOTween.Sequence();

            _entrySequence.Append(
                transform.DOMove(orbitPosition, entryDurationSeconds)
                         .SetEase(Ease.OutQuad));

            _entrySequence.Join(
                transform.DOScale(finalScale, entryDurationSeconds)
                         .From(tinyScale));

            _entrySequence.OnComplete(() => { onCompleted?.Invoke(); });
        }
    }
}

