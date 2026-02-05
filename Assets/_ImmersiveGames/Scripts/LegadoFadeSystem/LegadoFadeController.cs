using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.Scripts.LegadoFadeSystem
{
    /// <summary>
    /// Executor de fade:
    /// - N�o conhece OldSceneTransitionProfile.
    /// - Recebe dura��o/curvas em tempo de execu��o via Configure.
    /// - Usa AnimationCurve para interpolar alpha do CanvasGroup.
    ///
    /// Este componente tamb�m garante ordena��o de attributeCanvas para o FadeScene.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public class LegadoFadeController : MonoBehaviour
    {
        [Header("Refer�ncias")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Render Order (FadeScene)")]
        [Tooltip("SortingOrder do attributeCanvas do Fade. Deve ficar abaixo do Loading HUD, mas acima de UI comum.")]
        [SerializeField] private int sortingOrder = 11000;

        // Configura��o de runtime (vem do LegadoFadeService).
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

            DebugUtility.LogVerbose<LegadoFadeController>(
                "Awake - CanvasGroup: " + (canvasGroup != null ? "OK" : "NULO"));

            // Garantir ordena��o do Canvas do Fade
            TryConfigureCanvasSorting();

            if (canvasGroup != null)
            {
                // Sempre come�a transparente
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            // Defaults internos (s� usados se ningu�m configurar nada).
            _fadeInCurve  = LinearCurve;
            _fadeOutCurve = LinearCurve;
        }

        private void TryConfigureCanvasSorting()
        {
            var canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                DebugUtility.LogWarning<LegadoFadeController>(
                    "[Fade] Nenhum Canvas encontrado no FadeScene. Ordena��o n�o ser� configurada.");
                return;
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            DebugUtility.LogVerbose<LegadoFadeController>(
                $"[Fade] Canvas sorting configurado. overrideSorting={canvas.overrideSorting}, sortingOrder={canvas.sortingOrder}");
        }

        /// <summary>
        /// Define a configura��o de fade a ser usada nas pr�ximas chamadas.
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
        /// Faz o fade at� o alpha alvo usando a dura��o apropriada (entrada ou sa�da),
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

            DebugUtility.LogVerbose<LegadoFadeController>(
                $"Iniciando Fade para alpha = {targetAlpha} (dur={duration})");

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);

                float evaluatedT = EvaluateCurve(curve, t);
                canvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, evaluatedT);

                await Task.Yield();
            }

            canvasGroup.alpha = targetAlpha;

            DebugUtility.LogVerbose<LegadoFadeController>(
                $"Fade conclu�do para alpha = {targetAlpha}");
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

