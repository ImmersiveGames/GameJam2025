// Assets/_ImmersiveGames/NewScripts/QA/ContentSwap/ContentSwapQaInstaller.cs
// Instalador do QA de ContentSwap em Editor/Development.
// Comentários em português; código em inglês.

#nullable enable
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.ContentSwap.QA
{
    /// <summary>
    /// Garante que exista um GameObject com o componente ContentSwapQaContextMenu no Play Mode.
    ///
    /// Regras:
    /// - O chamador deve instalar apenas em Editor/Development (este arquivo não referência UnityEditor).
    /// - O GameObject é criado como DontDestroyOnLoad para ser acessível em qualquer cena.
    /// </summary>
    public static class ContentSwapQaInstaller
    {
        private const string QaObjectName = "QA_ContentSwap";

        // Paleta de observabilidade usada no projeto.
        private const string ColorInfo = "#A8DEED";
        private const string ColorOk = "#4CAF50";
        private const string ColorVerbose = "#00BCD4";

        public static void EnsureInstalled()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var existing = Object.FindFirstObjectByType<ContentSwapQaContextMenu>(FindObjectsInactive.Include);
            if (existing != null)
            {
                EnsureName(existing.gameObject);
                DebugUtility.Log(typeof(ContentSwapQaInstaller), "[QA][ContentSwap] ContentSwapQaContextMenu já instalado (instância existente).", ColorVerbose);
                return;
            }

            var go = GameObject.Find(QaObjectName);
            if (go == null)
            {
                go = new GameObject(QaObjectName);
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(ContentSwapQaInstaller), "[QA][ContentSwap] ContentSwapQaContextMenu instalado (DontDestroyOnLoad).", ColorInfo);
            }
            else
            {
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(ContentSwapQaInstaller), "[QA][ContentSwap] GameObject QA_ContentSwap existente; reaproveitando e marcando DontDestroyOnLoad.", ColorVerbose);
            }

            var menu = go.GetComponent<ContentSwapQaContextMenu>();
            if (menu == null)
            {
                go.AddComponent<ContentSwapQaContextMenu>();
                DebugUtility.Log(typeof(ContentSwapQaInstaller), "[QA][ContentSwap] ContentSwapQaContextMenu ausente; componente adicionado.", ColorInfo);
            }

            DebugUtility.Log(typeof(ContentSwapQaInstaller), "[QA][ContentSwap] Para acessar o ContextMenu, selecione o GameObject 'QA_ContentSwap' no Hierarchy (DontDestroyOnLoad).", ColorOk);
        }

        private static void EnsureName(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (go.name != QaObjectName)
            {
                go.name = QaObjectName;
            }
        }
    }
}
