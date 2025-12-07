using System.Threading.Tasks;
using UnityEngine;

namespace _ImmersiveGames.Scripts.FadeSystem
{
    public class FadeController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Fade Durations")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("Fade Curves")]
        [Tooltip("Curva de easing usada no FadeIn (0->1). Se nula ou vazia, usa lerp linear.")]
        [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Curva de easing usada no FadeOut (1->0). Se nula ou vazia, usa lerp linear.")]
        [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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

        public Task FadeInAsync()  => FadeToAsync(1f);
        public Task FadeOutAsync() => FadeToAsync(0f);

        /// <summary>
        /// Faz o fade até o alpha alvo usando a duração apropriada (entrada ou saída),
        /// com easing controlado por AnimationCurve.
        /// </summary>
        public async Task FadeToAsync(float targetAlpha)
        {
            if (canvasGroup == null)
                return;

            float currentAlpha = canvasGroup.alpha;
            bool isFadeIn = targetAlpha > currentAlpha;

            float duration = isFadeIn ? fadeInDuration : fadeOutDuration;
            AnimationCurve curve = isFadeIn ? fadeInCurve : fadeOutCurve;

            if (duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                return;
            }

            float time = 0f;

            Debug.Log($"[FadeController] Iniciando Fade para alpha = {targetAlpha} (dur={duration})");

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);

                float evaluatedT = EvaluateCurve(curve, t);
                canvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, evaluatedT);

                await Task.Yield();
            }

            canvasGroup.alpha = targetAlpha;

            Debug.Log($"[FadeController] Fade concluído para alpha = {targetAlpha}");
        }

        /// <summary>
        /// Avalia a curva, caindo para linear caso a curva seja nula ou vazia.
        /// </summary>
        private static float EvaluateCurve(AnimationCurve curve, float t)
        {
            if (curve == null || curve.keys == null || curve.keys.Length == 0)
                return t;

            return curve.Evaluate(t);
        }
    }
}
