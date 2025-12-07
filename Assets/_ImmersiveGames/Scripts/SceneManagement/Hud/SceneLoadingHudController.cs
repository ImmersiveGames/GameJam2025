using System.Threading.Tasks;
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
    [DebugLevel(DebugLevel.Verbose)]
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

            // NÃO vamos mexer no activeSelf da view aqui,
            // porque ela pode estar configurada na cena para iniciar desativada.
            // Em vez disso, vamos garantir que ela fique ativa quando formos exibir o loading.

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

            // Este é o ponto em que garantimos que tudo esteja ativo.
            EnsureCanvasAndViewActive();

            string title = "Carregando";
            string description = context.scenesToLoad != null && context.scenesToLoad.Count > 0
                ? $"Carregando: {string.Join(", ", context.scenesToLoad)}"
                : "Preparando cena...";

            DebugUtility.LogVerbose<SceneLoadingHudController>(
                "[HUD CTRL] ShowLoadingAsync chamado. " +
                $"view.activeInHierarchy={view.gameObject.activeInHierarchy}");

            // Agora usamos a versão com fade-in (Task).
            return view.ShowLoadingPanelAsync(title, description, "0%");
        }

        public Task MarkScenesReadyAsync(SceneTransitionContext context)
        {
            if (view == null)
                return Task.CompletedTask;

            // Se por algum motivo alguém desativou no meio da transição, reativa aqui também.
            EnsureCanvasAndViewActive();

            DebugUtility.LogVerbose<SceneLoadingHudController>(
                "[HUD CTRL] MarkScenesReadyAsync chamado.");

            view.UpdateTexts("Carregando", "Finalizando carregamento...", "100%");
            return Task.CompletedTask;
        }

        public Task HideLoadingAsync(SceneTransitionContext context)
        {
            if (view == null)
                return Task.CompletedTask;

            // Mesmo escondendo, garantimos que o objeto exista/esteja ativo
            // (HideLoadingPanelAsync vai apenas animar alpha e rootContainer).
            EnsureCanvasAndViewActive();

            DebugUtility.LogVerbose<SceneLoadingHudController>(
                "[HUD CTRL] HideLoadingAsync chamado.");

            // Agora usamos a versão com fade-out (Task).
            return view.HideLoadingPanelAsync();
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
    }
}
