using System.Threading.Tasks;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.FadeSystem
{
    /// <summary>
    /// Executor de fade:
    /// - Não conhece SceneTransitionProfile.
    /// - Recebe duração/curvas em tempo de execução via Configure.
    /// - Usa AnimationCurve para interpolar alpha do CanvasGroup.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public class FadeController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;

        // Configuração de runtime (vem do FadeService).
        private float _fadeInDuration  = 0.5f;
        private float _fadeOutDuration = 0.5f;
        private AnimationCurve _fadeInCurve;
        private AnimationCurve _fadeOutCurve;

        private static readonly AnimationCurve LinearCurve =
            AnimationCurve.Linear(0f, 0f, 1f, 1f);

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            DebugUtility.LogVerbose<FadeController>("Awake - CanvasGroup: " + (canvasGroup != null ? "OK" : "NULO"));

            if (canvasGroup != null)
            {
                // Sempre começa transparente
                canvasGroup.alpha = 0f;
            }

            // Defaults internos (só usados se ninguém configurar nada).
            _fadeInCurve  = LinearCurve;
            _fadeOutCurve = LinearCurve;
        }

        /// <summary>
        /// Define a configuração de fade a ser usada nas próximas chamadas.
        /// </summary>
        public void Configure(
            float fadeInDuration,
            float fadeOutDuration,
            AnimationCurve fadeInCurve,
            AnimationCurve fadeOutCurve)
        {
            _fadeInDuration  = fadeInDuration  > 0f ? fadeInDuration  : 0.5f;
            _fadeOutDuration = fadeOutDuration > 0f ? fadeOutDuration : 0.5f;

            _fadeInCurve  = fadeInCurve is { keys: { Length: > 0 } } ? fadeInCurve  : LinearCurve;
            _fadeOutCurve = fadeOutCurve is { keys: { Length: > 0 } } ? fadeOutCurve : LinearCurve;
        }

        public Task FadeInAsync()  => FadeToAsync(1f);
        public Task FadeOutAsync() => FadeToAsync(0f);

        /// <summary>
        /// Faz o fade até o alpha alvo usando a duração apropriada (entrada ou saída),
        /// com easing controlado por AnimationCurve.
        /// </summary>
        private async Task FadeToAsync(float targetAlpha)
        {
            if (canvasGroup == null)
                return;

            float currentAlpha = canvasGroup.alpha;
            bool isFadeIn = targetAlpha > currentAlpha;

            float duration = isFadeIn ? _fadeInDuration : _fadeOutDuration;
            AnimationCurve curve = isFadeIn ? _fadeInCurve : _fadeOutCurve;

            if (duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                return;
            }

            float time = 0f;

            DebugUtility.LogVerbose<FadeController>($"Iniciando Fade para alpha = {targetAlpha} (dur={duration})");

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);

                float evaluatedT = EvaluateCurve(curve, t);
                canvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, evaluatedT);

                await Task.Yield();
            }

            canvasGroup.alpha = targetAlpha;

            DebugUtility.LogVerbose<FadeController>($"Fade concluído para alpha = {targetAlpha}");
        }

        /// <summary>
        /// Avalia a curva, caindo para linear caso a curva seja nula ou vazia.
        /// </summary>
        private static float EvaluateCurve(AnimationCurve curve, float t)
        {
            if (curve?.keys == null || curve.keys.Length == 0)
                return t;

            return curve.Evaluate(t);
        }
    }
}
