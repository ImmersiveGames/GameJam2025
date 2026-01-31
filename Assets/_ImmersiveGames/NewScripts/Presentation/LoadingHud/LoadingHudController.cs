using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using TMPro;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Presentation.LoadingHud
{
    /// <summary>
    /// Controller do HUD de loading (NewScripts).
    /// Responsável apenas por mostrar/ocultar via CanvasGroup.
    ///
    /// Observação:
    /// - Este MonoBehaviour deve existir na cena "LoadingHudScene".
    /// - O serviço (LoadingHudService) resolve o controller via FindAnyObjectByType.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LoadingHudController : MonoBehaviour
    {
        private const string DefaultLabel = "Loading...";

        [Header("References")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private TMP_Text loadingText;

        private bool _isVisible;

        private void Awake()
        {
            if (rootGroup == null)
            {
                rootGroup = GetComponent<CanvasGroup>();
            }

            SetVisible(false);
            ApplyLabel(null);

            DebugUtility.LogVerbose<LoadingHudController>(
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
                DebugUtility.LogWarning<LoadingHudController>(
                    "[LoadingHUD] CanvasGroup não configurado. Não foi possível alterar visibilidade.");
                return;
            }

            if (_isVisible == visible)
            {
                return; // idempotência
            }

            _isVisible = visible;
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

            // Comentário: por ora mantemos label fixa; phase pode ser usada futuramente para debug visual.
            loadingText.text = DefaultLabel;
        }
    }
}
