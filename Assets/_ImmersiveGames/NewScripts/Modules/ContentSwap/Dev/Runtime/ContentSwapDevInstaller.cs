#if UNITY_EDITOR || DEVELOPMENT_BUILD
// Assets/_ImmersiveGames/NewScripts/QA/ContentSwap/ContentSwapDevInstaller.cs
// Instalador do QA de ContentSwap em Editor/Development.
// ComentÃ¡rios em portuguÃªs; cÃ³digo em inglÃªs.

#nullable enable
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Dev.Bindings;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.ContentSwap.Dev.Runtime
{
    /// <summary>
    /// Garante que exista um GameObject com o componente ContentSwapDevContextMenu no Play Mode.
    ///
    /// Regras:
    /// - O chamador deve instalar apenas em Editor/Development (este arquivo nÃ£o referÃªncia UnityEditor).
    /// - O GameObject Ã© criado como DontDestroyOnLoad para ser acessÃ­vel em qualquer cena.
    /// </summary>
    public static class ContentSwapDevInstaller
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

            var existing = Object.FindFirstObjectByType<ContentSwapDevContextMenu>(FindObjectsInactive.Include);
            if (existing != null)
            {
                EnsureName(existing.gameObject);
                DebugUtility.Log(typeof(ContentSwapDevInstaller), "[QA][ContentSwap] ContentSwapDevContextMenu jÃ¡ instalado (instÃ¢ncia existente).", ColorVerbose);
                return;
            }

            var go = GameObject.Find(QaObjectName);
            if (go == null)
            {
                go = new GameObject(QaObjectName);
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(ContentSwapDevInstaller), "[QA][ContentSwap] ContentSwapDevContextMenu instalado (DontDestroyOnLoad).", ColorInfo);
            }
            else
            {
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(ContentSwapDevInstaller), "[QA][ContentSwap] GameObject QA_ContentSwap existente; reaproveitando e marcando DontDestroyOnLoad.", ColorVerbose);
            }

            var menu = go.GetComponent<ContentSwapDevContextMenu>();
            if (menu == null)
            {
                go.AddComponent<ContentSwapDevContextMenu>();
                DebugUtility.Log(typeof(ContentSwapDevInstaller), "[QA][ContentSwap] ContentSwapDevContextMenu ausente; componente adicionado.", ColorInfo);
            }

            DebugUtility.Log(typeof(ContentSwapDevInstaller), "[QA][ContentSwap] Para acessar o ContextMenu, selecione o GameObject 'QA_ContentSwap' no Hierarchy (DontDestroyOnLoad).", ColorOk);
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
#endif

