using System.Collections;
using UnityEngine;

namespace _ImmersiveGames.Scripts.FadeSystem
{
    public class FadeController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

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

        public IEnumerator FadeTo(float target)
        {
            if (canvasGroup == null)
            {
                Debug.LogError("[FadeController] CanvasGroup nulo. Abortando fade.");
                yield break;
            }

            float start = canvasGroup.alpha;
            float time = 0f;

            Debug.Log($"[FadeController] Iniciando Fade para alpha = {target}");

            while (time < fadeDuration)
            {
                time += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, time / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = target;

            Debug.Log($"[FadeController] Fade concluído para alpha = {target}");
        }
    }
}