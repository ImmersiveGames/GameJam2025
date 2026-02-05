using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Bindings
{
    /// <summary>
    /// Controla o fade (CanvasGroup alpha) dentro da FadeScene.
    /// - Não conhece SceneTransitionProfile diretamente (config vem de fora).
    /// - Usa Time.unscaledDeltaTime para não ser afetado por pausas/timeScale.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class FadeController : MonoBehaviour
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

        private string _lastContextSignature;

        // Evento para integração com SceneFlow
        public event Action<string> OnFadeComplete;

        // Permite que adaptadores/SceneTransitionService definam explicitamente a signature antes do fade.
        public void SetContextSignature(string contextSignature)
        {
            if (!string.IsNullOrEmpty(contextSignature) && contextSignature != "no-signature")
                _lastContextSignature = contextSignature;
        }

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            DebugUtility.LogVerbose<FadeController>(
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
                DebugUtility.LogWarning<FadeController>(
                    "[Fade] Nenhum Canvas encontrado no FadeScene. Ordenação não será configurada.");
                return;
            }

            // Observação:
            // - Em Canvas raiz (sem parent Canvas), o Unity pode manter overrideSorting=false por design.
            // - Ainda assim, sortingOrder funciona normalmente para Canvas raiz.
            bool isRoot = canvas.isRootCanvas;

            if (!isRoot)
            {
                canvas.overrideSorting = true;
            }

            canvas.sortingOrder = sortingOrder;

            DebugUtility.LogVerbose<FadeController>(
                $"[Fade] Canvas sorting configurado. isRootCanvas={isRoot}, overrideSorting={canvas.overrideSorting}, sortingOrder={canvas.sortingOrder}");
        }

        public void Configure(FadeConfig config)
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

        // Compatibilidade: métodos existentes
        public Task FadeInAsync() => FadeInAsync("no-signature");
        public Task FadeOutAsync() => FadeOutAsync("no-signature");

        // Novas assinaturas com contextSignature (propagação)
        public Task FadeInAsync(string contextSignature) => FadeToAsync(1f, contextSignature);
        public Task FadeOutAsync(string contextSignature) => FadeToAsync(0f, contextSignature);

        private async Task FadeToAsync(float targetAlpha, string contextSignature)
        {
            if (canvasGroup == null) return;

            // Resolve signature: se chamado sem signature ("no-signature"), usar o último cacheado.
            string usedSignature = contextSignature;
            if (string.IsNullOrEmpty(usedSignature) || usedSignature == "no-signature")
            {
                usedSignature = _lastContextSignature ?? "no-signature";
            }
            else
            {
                _lastContextSignature = usedSignature;
            }

            float currentAlpha = canvasGroup.alpha;
            bool isFadeIn = targetAlpha > currentAlpha;

            float duration = isFadeIn ? _fadeInDuration : _fadeOutDuration;
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

                DebugUtility.LogVerbose<FadeController>($"[OBS][Fade] FadeComplete signature={usedSignature} targetAlpha={targetAlpha}");
                try { OnFadeComplete?.Invoke(usedSignature); } catch { }
                return;
            }

            float time = 0f;

            DebugUtility.LogVerbose<FadeController>($"[OBS][Fade] FadeStart signature={usedSignature} targetAlpha={targetAlpha} dur={duration}");

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

            DebugUtility.LogVerbose<FadeController>($"[OBS][Fade] FadeComplete signature={usedSignature} targetAlpha={targetAlpha}");
            try { OnFadeComplete?.Invoke(usedSignature); } catch { }
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
