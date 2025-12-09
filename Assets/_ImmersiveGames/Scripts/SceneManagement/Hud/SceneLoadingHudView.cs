using System.Threading.Tasks;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.SceneManagement.Hud
{
    /// <summary>
    /// View da HUD de loading de cena.
    /// Responsável apenas por:
    /// - Controlar CanvasGroup (alpha, interatividade).
    /// - Atualizar textos.
    /// - Mostrar / ocultar o painel via CanvasGroup.
    ///
    /// NÃO desativa o GameObject que contém este componente.
    /// Visibilidade é controlada apenas por alpha / raycasts.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SceneLoadingHudView : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private GameObject rootContainer;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text progressLabel;

        [Header("Barra de Progresso (Opcional)")]
        [Tooltip("Slider opcional para exibir o progresso de carregamento (0..1). Se nulo, apenas o texto de progresso será atualizado.")]
        [SerializeField] private Slider progressSlider;

        [Header("Configuração Inicial")]
        [SerializeField] private bool startHidden = true;

        private const float DefaultFadeInDuration  = 0.35f;
        private const float DefaultFadeOutDuration = 0.35f;

        private float _fadeInDuration  = DefaultFadeInDuration;
        private float _fadeOutDuration = DefaultFadeOutDuration;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (rootContainer == null)
                rootContainer = gameObject;

            if (startHidden)
                InitializeHidden();
            else
                InitializeVisible();
        }

        /// <summary>
        /// Deixa a HUD invisível, sem desativar o GameObject.
        /// </summary>
        private void InitializeHidden()
        {
            SetTexts(string.Empty, string.Empty, string.Empty);
            SetVisible(false);
            SetProgressInternal(0f);
        }

        /// <summary>
        /// Inicializa visível.
        /// </summary>
        public void InitializeVisible()
        {
            SetVisible(true);
            SetProgressInternal(0f);
        }

        /// <summary>
        /// Configura as durações de fade de entrada/saída da HUD.
        /// Valores menores ou iguais a zero fazem a view usar os defaults internos.
        /// </summary>
        public void ConfigureDurations(float fadeInSeconds, float fadeOutSeconds)
        {
            _fadeInDuration  = fadeInSeconds  > 0f ? fadeInSeconds  : DefaultFadeInDuration;
            _fadeOutDuration = fadeOutSeconds > 0f ? fadeOutSeconds : DefaultFadeOutDuration;

            DebugUtility.LogVerbose<SceneLoadingHudView>(
                $"[HUD VIEW] ConfigureDurations chamado. fadeIn={_fadeInDuration:0.000}, fadeOut={_fadeOutDuration:0.000}");
        }

        /// <summary>
        /// Mostra o painel de loading imediatamente (sem animação).
        /// Mantido para uso legado/local em editor ou testes.
        /// </summary>
        public void ShowLoadingPanel(string title, string description, string progress)
        {
            DebugUtility.LogVerbose<SceneLoadingHudView>("[HUD VIEW] ShowLoadingPanel (instantâneo) chamado.");
            SetTexts(title, description, progress);
            SetVisible(true);
        }

        /// <summary>
        /// Mostra o painel com animação de fade-in (Task-based, sem corrotina).
        /// Usado pelo controlador assíncrono.
        /// </summary>
        public Task ShowLoadingPanelAsync(string title, string description, string progress)
        {
            DebugUtility.LogVerbose<SceneLoadingHudView>("[HUD VIEW] ShowLoadingPanelAsync chamado.");
            SetTexts(title, description, progress);
            return FadeAlphaAsync(
                targetAlpha: 1f,
                duration: _fadeInDuration,
                ensureActiveAtStart: true,
                deactivateRootOnZero: false);
        }

        /// <summary>
        /// Atualiza apenas os textos (por exemplo, para "Finalizando...").
        /// </summary>
        public void UpdateTexts(string title, string description, string progress)
        {
            DebugUtility.LogVerbose<SceneLoadingHudView>("[HUD VIEW] UpdateTexts chamado.");
            SetTexts(title, description, progress);
        }

        /// <summary>
        /// Atualiza o valor de progresso (0..1), afetando texto e slider (se houver).
        /// </summary>
        public void SetProgress(float normalized)
        {
            SetProgressInternal(Mathf.Clamp01(normalized));
        }

        /// <summary>
        /// Esconde o painel de loading imediatamente (sem animação).
        /// Mantido para uso legado/local em editor ou testes.
        /// </summary>
        public void HideLoadingPanel()
        {
            DebugUtility.LogVerbose<SceneLoadingHudView>("[HUD VIEW] HideLoadingPanel (instantâneo) chamado.");
            SetVisible(false);
        }

        /// <summary>
        /// Esconde o painel com animação de fade-out (Task-based, sem corrotina).
        /// Usado pelo controlador assíncrono.
        /// </summary>
        public Task HideLoadingPanelAsync()
        {
            DebugUtility.LogVerbose<SceneLoadingHudView>("[HUD VIEW] HideLoadingPanelAsync chamado.");
            return FadeAlphaAsync(
                targetAlpha: 0f,
                duration: _fadeOutDuration,
                ensureActiveAtStart: false,
                deactivateRootOnZero: true);
        }

        #region Internals

        private void SetTexts(string title, string description, string progress)
        {
            if (titleLabel != null)
                titleLabel.text = title ?? string.Empty;

            if (descriptionLabel != null)
                descriptionLabel.text = description ?? string.Empty;

            if (progressLabel != null)
                progressLabel.text = progress ?? string.Empty;
        }

        private void SetVisible(bool visible)
        {
            // NUNCA desativamos o GameObject deste componente.
            // Se rootContainer for outro objeto (filho), ele pode ser ativado/desativado.
            if (rootContainer != null && rootContainer != gameObject)
                rootContainer.SetActive(visible);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            DebugUtility.LogVerbose<SceneLoadingHudView>(
                $"[HUD VIEW] SetVisible({visible}) | rootContainer.activeSelf={(rootContainer != null ? rootContainer.activeSelf.ToString() : "null")} | alpha={(canvasGroup != null ? canvasGroup.alpha.ToString("0.00") : "n/a")}");
        }

        private void SetProgressInternal(float value)
        {
            if (progressSlider != null)
                progressSlider.value = value;

            if (progressLabel != null)
            {
                int percent = Mathf.RoundToInt(value * 100f);
                progressLabel.text = $"{percent}%";
            }

            DebugUtility.LogVerbose<SceneLoadingHudView>(
                $"[HUD VIEW] SetProgressInternal({value:0.00})");
        }

        /// <summary>
        /// Fade genérico baseado em Task + Time.unscaledDeltaTime.
        /// Não usa corrotina, e mantém o GameObject vivo o tempo todo.
        /// </summary>
        private async Task FadeAlphaAsync(
            float targetAlpha,
            float duration,
            bool ensureActiveAtStart,
            bool deactivateRootOnZero)
        {
            if (canvasGroup == null)
                return;

            if (rootContainer == null)
                rootContainer = gameObject;

            if (ensureActiveAtStart && rootContainer != null && !rootContainer.activeSelf)
                rootContainer.SetActive(true);

            float startAlpha = canvasGroup.alpha;
            float time = 0f;

            if (duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                bool finalVisibleInstant = targetAlpha > 0f;
                canvasGroup.interactable = finalVisibleInstant;
                canvasGroup.blocksRaycasts = finalVisibleInstant;

                if (!finalVisibleInstant && deactivateRootOnZero &&
                    rootContainer != null && rootContainer != gameObject)
                {
                    rootContainer.SetActive(false);
                }

                DebugUtility.LogVerbose<SceneLoadingHudView>(
                    $"[HUD VIEW] FadeAlphaAsync instantâneo para alpha={targetAlpha:0.00}.");
                return;
            }

            DebugUtility.LogVerbose<SceneLoadingHudView>(
                $"[HUD VIEW] FadeAlphaAsync iniciado. targetAlpha={targetAlpha:0.00}, duration={duration:0.00}, startAlpha={startAlpha:0.00}");

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

                await Task.Yield();
            }

            canvasGroup.alpha = targetAlpha;
            bool finalVisible = targetAlpha > 0f;
            canvasGroup.interactable = finalVisible;
            canvasGroup.blocksRaycasts = finalVisible;

            if (!finalVisible && deactivateRootOnZero &&
                rootContainer != null && rootContainer != gameObject)
            {
                rootContainer.SetActive(false);
            }

            DebugUtility.LogVerbose<SceneLoadingHudView>(
                $"[HUD VIEW] FadeAlphaAsync concluído. alphaFinal={canvasGroup.alpha:0.00}, visible={finalVisible}");
        }

        #endregion
    }
}
