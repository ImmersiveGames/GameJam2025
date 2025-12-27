using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
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

        [Header("Sorting")]
        [SerializeField] private int sortingOrder = 12050;

        public bool IsVisible => rootGroup != null && rootGroup.alpha > 0.001f;

        private void Awake()
        {
            if (rootGroup == null)
            {
                rootGroup = GetComponent<CanvasGroup>();
            }

            SetVisible(false);
            TryConfigureCanvasSorting();
            RegisterHudInGlobalDi();

            EventBus<SceneLoadingHudRegisteredEvent>.Raise(new SceneLoadingHudRegisteredEvent());

            DebugUtility.LogVerbose<SceneLoadingHudController>(
                "[HUD] SceneLoadingHudController inicializado, registrado no DI global e sinalizado como pronto.");
        }

        private void OnDestroy()
        {
            EventBus<SceneLoadingHudUnregisteredEvent>.Raise(new SceneLoadingHudUnregisteredEvent());

            DebugUtility.LogVerbose<SceneLoadingHudController>(
                "[HUD] SceneLoadingHudController destruído e sinalizado como desregistrado.");
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

        private void RegisterHudInGlobalDi()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneLoadingHud>(out var existing) && existing != null && existing != this)
            {
                DebugUtility.LogWarning<SceneLoadingHudController>(
                    "[HUD] ISceneLoadingHud já registrado no DI global. Sobrescrevendo com instância atual.");
            }

            DependencyManager.Provider.RegisterGlobal<ISceneLoadingHud>(this, allowOverride: true);
        }

        private void TryConfigureCanvasSorting()
        {
            var canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                DebugUtility.LogWarning<SceneLoadingHudController>(
                    "[HUD] Nenhum Canvas encontrado no HUD. Sorting não será configurado.");
                return;
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            DebugUtility.LogVerbose<SceneLoadingHudController>(
                $"[HUD] Canvas sorting configurado. overrideSorting={canvas.overrideSorting}, sortingOrder={canvas.sortingOrder}.");
        }
    }
}
