using UnityEngine;
using UnityEngine.SceneManagement;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.SceneManagement.Bootstrap
{
    /// <summary>
    /// Garante que a cena UIGlobalScene seja carregada uma única vez,
    /// antes de qualquer fluxo de transição (Menu, Gameplay, etc.).
    /// Não é descarregada pelas transições normais.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class UIGlobalBootstrapper : MonoBehaviour
    {
        [SerializeField] private string uiGlobalSceneName = "UIGlobalScene";

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(uiGlobalSceneName))
            {
                DebugUtility.LogWarning<UIGlobalBootstrapper>(
                    "uiGlobalSceneName não configurado. Cena de UI global não será carregada.");
                return;
            }

            var scene = SceneManager.GetSceneByName(uiGlobalSceneName);
            if (scene.isLoaded)
            {
                DebugUtility.LogVerbose<UIGlobalBootstrapper>(
                    $"Cena de UI global '{uiGlobalSceneName}' já está carregada.");
                return;
            }

            DebugUtility.Log<UIGlobalBootstrapper>(
                $"Carregando cena de UI global '{uiGlobalSceneName}' em modo Additive...",
                DebugUtility.Colors.Info);

            SceneManager.LoadScene(uiGlobalSceneName, LoadSceneMode.Additive);
        }
    }
}
