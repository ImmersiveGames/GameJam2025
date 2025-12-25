using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Fade
{
    /// <summary>
    /// Controla o fade (CanvasGroup alpha) dentro da FadeScene.
    /// - Não conhece SceneTransitionProfile diretamente (config vem de fora).
    /// - Usa Time.unscaledDeltaTime para não ser afetado por pausas/timeScale.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class NewScriptsFadeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Render Order (FadeScene)")]
        [Tooltip("SortingOrder do Canvas do Fade. Deve ficar acima de UI comum e abaixo de HUDs especiais (ex.: loading).")]
        [SerializeField] private int sortingOrder = 11000;

        private float _fadeInDuration = 0.5f;
        private float _fadeOutDuration = 0.5f;
        private AnimationCurve _fadeInCurve;
        private AnimationCurve _fadeOutCurve;

        private static readonly AnimationCurve LinearCurve =
            AnimationCurve.Linear(0f, 0f, 1f, 1f);

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            DebugUtility.LogVerbose<NewScriptsFadeController>(
                "[Fade] Awake - CanvasGroup: " + (canvasGroup != null ? "OK" : "NULL"));

            TryConfigureCanvasSorting();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            _fadeInCurve = LinearCurve;
            _fadeOutCurve = LinearCurve;
        }

        private void TryConfigureCanvasSorting()
        {
            var canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                DebugUtility.LogWarning<NewScriptsFadeController>(
                    "[Fade] Nenhum Canvas encontrado no FadeScene. Ordenação não será configurada.");
                return;
            }

            // Observação:
            // - Em Canvas raiz (sem parent Canvas), o Unity pode manter overrideSorting=false por design.
            // - Ainda assim, sortingOrder funciona normalmente para Canvas raiz.
            var isRoot = canvas.isRootCanvas;

            if (!isRoot)
            {
                canvas.overrideSorting = true;
            }

            canvas.sortingOrder = sortingOrder;

            DebugUtility.LogVerbose<NewScriptsFadeController>(
                $"[Fade] Canvas sorting configurado. isRootCanvas={isRoot}, overrideSorting={canvas.overrideSorting}, sortingOrder={canvas.sortingOrder}");
        }

        public void Configure(NewScriptsFadeConfig config)
        {
            _fadeInDuration = config.FadeInDuration > 0f ? config.FadeInDuration : 0.5f;
            _fadeOutDuration = config.FadeOutDuration > 0f ? config.FadeOutDuration : 0.5f;

            _fadeInCurve = config.FadeInCurve != null && config.FadeInCurve.keys != null && config.FadeInCurve.keys.Length > 0
                ? config.FadeInCurve
                : LinearCurve;

            _fadeOutCurve = config.FadeOutCurve != null && config.FadeOutCurve.keys != null && config.FadeOutCurve.keys.Length > 0
                ? config.FadeOutCurve
                : LinearCurve;
        }

        public Task FadeInAsync() => FadeToAsync(1f);
        public Task FadeOutAsync() => FadeToAsync(0f);

        private async Task FadeToAsync(float targetAlpha)
        {
            if (canvasGroup == null)
            {
                return;
            }

            var currentAlpha = canvasGroup.alpha;
            var isFadeIn = targetAlpha > currentAlpha;

            var duration = isFadeIn ? _fadeInDuration : _fadeOutDuration;
            var curve = isFadeIn ? _fadeInCurve : _fadeOutCurve;

            // Bloqueia input enquanto escurece.
            if (isFadeIn)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = false;
            }

            if (duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                if (targetAlpha <= 0f)
                {
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                }
                return;
            }

            float time = 0f;

            DebugUtility.LogVerbose<NewScriptsFadeController>(
                $"[Fade] Iniciando Fade para alpha={targetAlpha} (dur={duration})");

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);

                float evaluatedT = EvaluateCurve(curve, t);
                canvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, evaluatedT);

                await Task.Yield();
            }

            canvasGroup.alpha = targetAlpha;

            if (targetAlpha <= 0f)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            DebugUtility.LogVerbose<NewScriptsFadeController>(
                $"[Fade] Fade concluído para alpha={targetAlpha}");
        }

        private static float EvaluateCurve(AnimationCurve curve, float t)
        {
            if (curve == null || curve.keys == null || curve.keys.Length == 0)
            {
                return t;
            }

            return curve.Evaluate(t);
        }
    }
}
