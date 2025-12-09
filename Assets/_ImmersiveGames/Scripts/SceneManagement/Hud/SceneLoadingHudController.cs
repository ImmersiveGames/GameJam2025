using System.Threading.Tasks;
using _ImmersiveGames.Scripts.SceneManagement.Configs;
using _ImmersiveGames.Scripts.SceneManagement.Transition;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SceneManagement.Hud
{
    /// <summary>
    /// Controlador da HUD de loading.
    /// - Faz a ponte entre SceneTransitionService e SceneLoadingHudView.
    /// - Expõe API síncrona (ISceneLoadingHudService) e assíncrona (ISceneLoadingHudTaskService).
    ///
    /// Não usa corrotinas diretamente; a coordenação é feita via Tasks.
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

        // Canvas que contém a HUD, guardado para podermos reativar quando necessário.
        private Canvas _canvas;

        private void Awake()
        {
            // Garante que o GameObject do controller está ativo
            if (!gameObject.activeSelf)
            {
                DebugUtility.LogWarning<SceneLoadingHudController>(
                    "[HUD CTRL] GameObject do controller estava inativo. Ativando.");
                gameObject.SetActive(true);
            }

            if (view == null)
            {
                // Busca a view mesmo em filhos inativos
                view = GetComponentInChildren<SceneLoadingHudView>(true);
            }

            if (view == null)
            {
                DebugUtility.LogError<SceneLoadingHudController>(
                    "[HUD CTRL] SceneLoadingHudView não encontrada. HUD será desativada.");
                enabled = false;
                return;
            }

            // Descobre e guarda o Canvas pai (mesmo que esteja inativo)
            _canvas = view.GetComponentInParent<Canvas>(true);
            if (_canvas != null)
            {
                // Garante que a HUD fique acima de outros canvases (como o fade)
                _canvas.overrideSorting = true;
                if (_canvas.sortingOrder < 5000)
                    _canvas.sortingOrder = 5000;

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

                provider.RegisterGlobal<ISceneLoadingHudService>(
                    this,
                    allowOverride: false);

                provider.RegisterGlobal<ISceneLoadingHudTaskService>(
                    this,
                    allowOverride: false);

                DebugUtility.Log<SceneLoadingHudController>(
                    "[HUD CTRL] SceneLoadingHudController inicializado e registrado no DI.");
            }
        }

        /// <summary>
        /// Garante que o Canvas e a View estejam ativos/habilitados no momento da exibição.
        /// Isso resolve o caso em que o UI Global ou o painel foram desativados em algum momento.
        /// </summary>
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
            }

            if (view != null && !view.gameObject.activeSelf)
            {
                DebugUtility.LogWarning<SceneLoadingHudController>(
                    "[HUD CTRL] GameObject da view estava inativo. Ativando.");
                view.gameObject.SetActive(true);
            }
        }

        #region ISceneLoadingHudService (API síncrona/legada)

        public void ShowLoading(SceneTransitionContext context)
        {
            _ = ShowLoadingAsync(context);
        }

        public void MarkScenesReady(SceneTransitionContext context)
        {
            _ = MarkScenesReadyAsync(context);
        }

        public void HideLoading(SceneTransitionContext context)
        {
            _ = HideLoadingAsync(context);
        }

        #endregion

        #region ISceneLoadingHudTaskService (API assíncrona usada pelo SceneTransitionService)

        public Task ShowLoadingAsync(SceneTransitionContext context)
        {
            if (view == null)
                return Task.CompletedTask;

            EnsureCanvasAndViewActive();

            var profile = context.transitionProfile;
            string title;
            string description;

            if (profile != null && !string.IsNullOrWhiteSpace(profile.LoadingTitle))
            {
                title = profile.LoadingTitle;
            }
            else
            {
                title = "Carregando";
            }

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
            {
                view.ConfigureDurations(profile.HudFadeInDuration, profile.HudFadeOutDuration);
            }
            else
            {
                // Usa defaults internos da view.
                view.ConfigureDurations(0f, 0f);
            }

            // Inicializa sempre com 0%.
            view.SetProgress(0f);

            DebugUtility.LogVerbose<SceneLoadingHudController>(
                "[HUD CTRL] ShowLoadingAsync chamado. " +
                $"view.activeInHierarchy={view.gameObject.activeInHierarchy}");

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

            DebugUtility.LogVerbose<SceneLoadingHudController>(
                "[HUD CTRL] MarkScenesReadyAsync chamado.");

            view.UpdateTexts(title, description, "100%");
            return Task.CompletedTask;
        }

        public Task HideLoadingAsync(SceneTransitionContext context)
        {
            if (view == null)
                return Task.CompletedTask;

            EnsureCanvasAndViewActive();

            DebugUtility.LogVerbose<SceneLoadingHudController>(
                "[HUD CTRL] HideLoadingAsync chamado.");

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

    /// <summary>
    /// Serviço Task-based para coordenar o HUD de loading com o SceneTransitionService.
    /// </summary>
    public interface ISceneLoadingHudTaskService
    {
        Task ShowLoadingAsync(SceneTransitionContext context);
        Task MarkScenesReadyAsync(SceneTransitionContext context);
        Task HideLoadingAsync(SceneTransitionContext context);

        /// <summary>
        /// Atualiza o progresso normalizado (0..1) da transição.
        /// Implementações podem simplesmente ignorar se não suportarem barra de progresso.
        /// </summary>
        void SetProgress(float value);
    }
}
