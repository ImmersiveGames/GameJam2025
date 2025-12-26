using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading
{
    /// <summary>
    /// Controller MonoBehaviour para HUD de loading no UIGlobalScene.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneLoadingHudController : MonoBehaviour, ISceneLoadingHud
    {
        [Header("References")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailsText;
        [SerializeField] private Slider progressSlider;

        public bool IsVisible => rootGroup != null && rootGroup.alpha > 0.001f;

        private void Awake()
        {
            if (rootGroup == null)
            {
                rootGroup = GetComponent<CanvasGroup>();
            }

            SetVisible(false);

            if (DependencyManager.Provider.TryGetGlobal<SceneFlowLoadingService>(out var service) && service != null)
            {
                service.AttachHud(this);

                DebugUtility.LogVerbose<SceneLoadingHudController>(
                    "[HUD] SceneLoadingHudController inicializado e anexado ao SceneFlowLoadingService.");
            }
            else
            {
                DebugUtility.LogWarning<SceneLoadingHudController>(
                    "[HUD] SceneFlowLoadingService indisponível no DI global. HUD não será anexado.");
            }
        }

        public void Show(string title, string details = null)
        {
            SetVisible(true);

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

        public void SetProgress01(float progress01)
        {
            if (progressSlider == null)
            {
                return;
            }

            progressSlider.value = Mathf.Clamp01(progress01);
        }

        public void Hide()
        {
            SetVisible(false);
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
    }
}
