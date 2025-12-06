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
                // Começa transparente por padrão
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
        public IEnumerator FadeTo(float target)
        {
            if (canvasGroup == null)
                yield break;

            float current = canvasGroup.alpha;

            // Se estamos aumentando o alpha, usamos a duração de FadeIn;
            // se estamos diminuindo, usamos a duração de FadeOut.
            float duration = target > current ? fadeInDuration : fadeOutDuration;

            // Proteção para duração zero ou negativa: aplica direto.
            if (duration <= 0f)
            {
                canvasGroup.alpha = target;
                yield break;
            }

            float start = current;
            float time = 0f;

            Debug.Log($"[FadeController] Iniciando Fade para alpha = {target} (dur={duration})");

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                canvasGroup.alpha = Mathf.Lerp(start, target, t);
                yield return null;
            }

            canvasGroup.alpha = target;

            Debug.Log($"[FadeController] Fade concluído para alpha = {target}");
        }
    }
}
