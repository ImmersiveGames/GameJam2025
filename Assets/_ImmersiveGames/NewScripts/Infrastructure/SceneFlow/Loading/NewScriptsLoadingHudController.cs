using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading
{
    /// <summary>
    /// Controller MonoBehaviour para HUD de loading dentro da LoadingHudScene.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class NewScriptsLoadingHudController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailsText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Slider progressSlider;

        [Header("Sorting")]
        [SerializeField] private int sortingOrder = 12050;

        private void Awake()
        {
            if (rootGroup == null)
            {
                rootGroup = GetComponent<CanvasGroup>();
            }

            SetVisible(false);
            TryConfigureCanvasSorting();
        }

        public void Show(string title, string details)
        {
            SetVisible(true);
            UpdateTexts(title, details);
        }

        public void SetProgress01(float progress01)
        {
            var value = Mathf.Clamp01(progress01);

            if (progressSlider != null)
            {
                progressSlider.value = value;
            }

            if (progressText != null)
            {
                var percent = Mathf.RoundToInt(value * 100f);
                progressText.text = $"{percent}%";
            }
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void UpdateTexts(string title, string details)
        {
            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }

            if (detailsText != null)
            {
                var hasDetails = !string.IsNullOrWhiteSpace(details);
                detailsText.text = hasDetails ? details : string.Empty;
                detailsText.gameObject.SetActive(hasDetails);
            }
        }

        private void SetVisible(bool visible)
        {
            if (rootGroup == null)
            {
                return;
            }

            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.blocksRaycasts = visible;
            rootGroup.interactable = visible;
        }

        private void TryConfigureCanvasSorting()
        {
            var canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                DebugUtility.LogWarning<NewScriptsLoadingHudController>(
                    "[Loading HUD] Nenhum Canvas encontrado no LoadingHudScene. Sorting não será configurado.");
                return;
            }

            if (!canvas.isRootCanvas)
            {
                canvas.overrideSorting = true;
            }

            canvas.sortingOrder = sortingOrder;

            DebugUtility.LogVerbose<NewScriptsLoadingHudController>(
                $"[Loading HUD] Canvas sorting configurado. isRootCanvas={canvas.isRootCanvas}, overrideSorting={canvas.overrideSorting}, sortingOrder={canvas.sortingOrder}.");
        }
    }
}
