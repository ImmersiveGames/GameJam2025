using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using TMPro;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading
{
    /// <summary>
    /// Controller mínimo do HUD de loading (NewScripts).
    /// Responsável apenas por mostrar/ocultar via CanvasGroup.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class NewScriptsLoadingHudController : MonoBehaviour
    {
        private const string DefaultLabel = "Loading...";

        [Header("References")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private TMP_Text loadingText;

        private void Awake()
        {
            if (rootGroup == null)
            {
                rootGroup = GetComponent<CanvasGroup>();
            }

            SetVisible(false);
            ApplyLabel(null);

            DebugUtility.LogVerbose<NewScriptsLoadingHudController>(
                "[LoadingHUD] Controller inicializado (CanvasGroup pronto).");
        }

        public void Show(string phase)
        {
            SetVisible(true);
            ApplyLabel(phase);
        }

        public void Hide(string phase)
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (rootGroup == null)
            {
                DebugUtility.LogWarning<NewScriptsLoadingHudController>(
                    "[LoadingHUD] CanvasGroup não configurado. Não foi possível alterar visibilidade.");
                return;
            }

            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.interactable = visible;
            rootGroup.blocksRaycasts = visible;
        }

        private void ApplyLabel(string phase)
        {
            if (loadingText == null)
            {
                return;
            }

            loadingText.text = DefaultLabel;
        }
    }
}
