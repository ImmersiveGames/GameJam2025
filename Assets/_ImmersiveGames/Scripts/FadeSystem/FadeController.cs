using System.Collections;
using UnityEngine;

namespace _ImmersiveGames.Scripts.FadeSystem
{
    public class FadeController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Fade Durations")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            Debug.Log("[FadeController] Awake - CanvasGroup: " + (canvasGroup != null ? "OK" : "NULO"));

            if (canvasGroup != null)
            {
                // Sempre começa transparente
                canvasGroup.alpha = 0f;
            }
        }

        public IEnumerator FadeIn()
        {
            return FadeTo(1f);
        }

        public IEnumerator FadeOut()
        {
            return FadeTo(0f);
        }

        /// <summary>
        /// Faz o fade até o alpha alvo usando a duração apropriada (entrada ou saída).
        /// </summary>
        public IEnumerator FadeTo(float targetAlpha)
        {
            if (canvasGroup == null)
                yield break;

            float currentAlpha = canvasGroup.alpha;
            float duration = targetAlpha > currentAlpha ? fadeInDuration : fadeOutDuration;

            if (duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                yield break;
            }

            float time = 0f;

            Debug.Log($"[FadeController] Iniciando Fade para alpha = {targetAlpha} (dur={duration})");

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                canvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;

            Debug.Log($"[FadeController] Fade concluído para alpha = {targetAlpha}");
        }
    }
}
