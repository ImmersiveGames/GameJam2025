using System.Threading.Tasks;
using _ImmersiveGames.Scripts.SceneManagement.Transition;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SceneManagement.Hud
{
    /// <summary>
    /// Controlador da HUD de loading.
    /// - Ponte entre SceneTransitionService e SceneLoadingHudView.
    /// - API síncrona (ISceneLoadingHudService) e assíncrona (ISceneLoadingHudTaskService).
    ///
    /// Não usa corrotinas diretamente; coordenação via Tasks.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class SceneLoadingHudController :
        MonoBehaviour,
        ISceneLoadingHudService,
        ISceneLoadingHudTaskService
    {
        [Header("Referências")]
        [SerializeField] private SceneLoadingHudView view;

        [Header("Registro em DI")]
        [SerializeField] private bool registerAsGlobalService = true;

        [Header("Render Order (UIGlobalScene)")]
        [Tooltip("SortingOrder do canvas do Loading HUD. Deve ser MAIOR do que Fade e Terminal Overlay.")]
        [SerializeField] private int loadingSortingOrder = 12000;

        // Canvas que contém a HUD.
        private Canvas _canvas;

        private void Awake()
        {
            if (!gameObject.activeSelf)
            {
                DebugUtility.LogWarning<SceneLoadingHudController>(
                    "[HUD CTRL] GameObject do controller estava inativo. Ativando.");
                gameObject.SetActive(true);
            }

            if (view == null)
            {
                view = GetComponentInChildren<SceneLoadingHudView>(true);
            }

            if (view == null)
            {
                DebugUtility.LogError<SceneLoadingHudController>(
                    "[HUD CTRL] SceneLoadingHudView não encontrada. HUD será desativada.");
                enabled = false;
                return;
            }

            _canvas = view.GetComponentInParent<Canvas>(true);
            if (_canvas != null)
            {
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = loadingSortingOrder;

                DebugUtility.LogVerbose<SceneLoadingHudController>(
                    $"[HUD CTRL] Canvas configurado. overrideSorting={_canvas.overrideSorting}, sortingOrder={_canvas.sortingOrder}");
            }
            else
            {
                DebugUtility.LogWarning<SceneLoadingHudController>(
                    "[HUD CTRL] Nenhum Canvas encontrado na HUD. Verifique a cena UIGlobalScene.");
            }

            if (registerAsGlobalService)
            {
                var provider = DependencyManager.Provider;

                provider.RegisterGlobal<ISceneLoadingHudService>(this, allowOverride: false);
                provider.RegisterGlobal<ISceneLoadingHudTaskService>(this, allowOverride: false);

                DebugUtility.Log<SceneLoadingHudController>(
                    "[HUD CTRL] SceneLoadingHudController inicializado e registrado no DI.");
            }
        }

        private void EnsureCanvasAndViewActive()
        {
            if (_canvas != null)
            {
                if (!_canvas.gameObject.activeSelf)
                {
                    DebugUtility.LogWarning<SceneLoadingHudController>(
                        "[HUD CTRL] Canvas da HUD estava com gameObject inativo. Ativando.");
                    _canvas.gameObject.SetActive(true);
                }

                if (!_canvas.enabled)
                {
                    DebugUtility.LogWarning<SceneLoadingHudController>(
                        "[HUD CTRL] Canvas da HUD estava desabilitado. Habilitando.");
                    _canvas.enabled = true;
                }

                // Reforça order em runtime (caso algum outro sistema altere)
                if (!_canvas.overrideSorting || _canvas.sortingOrder != loadingSortingOrder)
                {
                    _canvas.overrideSorting = true;
                    _canvas.sortingOrder = loadingSortingOrder;

                    DebugUtility.LogVerbose<SceneLoadingHudController>(
                        $"[HUD CTRL] Canvas reconfigurado em runtime. overrideSorting={_canvas.overrideSorting}, sortingOrder={_canvas.sortingOrder}");
                }
            }

            if (view != null && !view.gameObject.activeSelf)
            {
                DebugUtility.LogWarning<SceneLoadingHudController>(
                    "[HUD CTRL] GameObject da view estava inativo. Ativando.");
                view.gameObject.SetActive(true);
            }
        }

        #region ISceneLoadingHudService (API síncrona/legada)

        public void ShowLoading(SceneTransitionContext context) => _ = ShowLoadingAsync(context);
        public void MarkScenesReady(SceneTransitionContext context) => _ = MarkScenesReadyAsync(context);
        public void HideLoading(SceneTransitionContext context) => _ = HideLoadingAsync(context);

        #endregion

        #region ISceneLoadingHudTaskService (API assíncrona usada pelo SceneTransitionService)

        public Task ShowLoadingAsync(SceneTransitionContext context)
        {
            if (view == null)
                return Task.CompletedTask;

            EnsureCanvasAndViewActive();

            var profile = context.transitionProfile;

            string title = (profile != null && !string.IsNullOrWhiteSpace(profile.LoadingTitle))
                ? profile.LoadingTitle
                : "Carregando";

            string description;
            if (profile != null && !string.IsNullOrWhiteSpace(profile.LoadingDescriptionTemplate))
            {
                string scenesText = (context.scenesToLoad != null && context.scenesToLoad.Count > 0)
                    ? string.Join(", ", context.scenesToLoad)
                    : "cenas";

                description = profile.LoadingDescriptionTemplate.Replace("{Scenes}", scenesText);
            }
            else
            {
                description = context.scenesToLoad != null && context.scenesToLoad.Count > 0
                    ? $"Carregando: {string.Join(", ", context.scenesToLoad)}"
                    : "Preparando cena...";
            }

            if (profile != null)
                view.ConfigureDurations(profile.HudFadeInDuration, profile.HudFadeOutDuration);
            else
                view.ConfigureDurations(0f, 0f);

            view.SetProgress(0f);

            return view.ShowLoadingPanelAsync(title, description, "0%");
        }

        public Task MarkScenesReadyAsync(SceneTransitionContext context)
        {
            if (view == null)
                return Task.CompletedTask;

            EnsureCanvasAndViewActive();

            var profile = context.transitionProfile;

            string title = (profile != null && !string.IsNullOrWhiteSpace(profile.FinishingTitle))
                ? profile.FinishingTitle
                : "Carregando";

            string description = (profile != null && !string.IsNullOrWhiteSpace(profile.FinishingDescription))
                ? profile.FinishingDescription
                : "Finalizando carregamento...";

            view.UpdateTexts(title, description, "100%");
            return Task.CompletedTask;
        }

        public Task HideLoadingAsync(SceneTransitionContext context)
        {
            if (view == null)
                return Task.CompletedTask;

            EnsureCanvasAndViewActive();
            return view.HideLoadingPanelAsync();
        }

        public void SetProgress(float value)
        {
            if (view == null)
                return;

            view.SetProgress(value);
        }

        #endregion
    }

    public interface ISceneLoadingHudTaskService
    {
        Task ShowLoadingAsync(SceneTransitionContext context);
        Task MarkScenesReadyAsync(SceneTransitionContext context);
        Task HideLoadingAsync(SceneTransitionContext context);
        void SetProgress(float value);
    }
}
