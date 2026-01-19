// Assets/_ImmersiveGames/NewScripts/QA/Phases/PhaseQaInstaller.cs
// Instalador do QA de Phases (Baseline 2.2) em Editor/Development.
// Comentários em português; código em inglês.

#nullable enable
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Phases
{
    /// <summary>
    /// Garante que exista um GameObject com o componente PhaseQaContextMenu no Play Mode.
    ///
    /// Regras:
    /// - O chamador deve instalar apenas em Editor/Development (este arquivo não referencia UnityEditor).
    /// - O GameObject é criado como DontDestroyOnLoad para ser acessível em qualquer cena.
    /// </summary>
    public static class PhaseQaInstaller
    {
        private const string QaObjectName = "QA_Phase";

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

            var existing = Object.FindObjectOfType<PhaseQaContextMenu>(true);
            if (existing != null)
            {
                EnsureName(existing.gameObject);
                DebugUtility.Log(typeof(PhaseQaInstaller),"[QA][Phase] PhaseQaContextMenu já instalado (instância existente).", ColorVerbose);
                return;
            }

            var go = GameObject.Find(QaObjectName);
            if (go == null)
            {
                go = new GameObject(QaObjectName);
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(PhaseQaInstaller),"[QA][Phase] PhaseQaContextMenu instalado (DontDestroyOnLoad).", ColorInfo);
            }
            else
            {
                Object.DontDestroyOnLoad(go);
                DebugUtility.Log(typeof(PhaseQaInstaller),"[QA][Phase] GameObject QA_Phase existente; reaproveitando e marcando DontDestroyOnLoad.", ColorVerbose);
            }

            var menu = go.GetComponent<PhaseQaContextMenu>();
            if (menu == null)
            {
                go.AddComponent<PhaseQaContextMenu>();
                DebugUtility.Log(typeof(PhaseQaInstaller),"[QA][Phase] PhaseQaContextMenu ausente; componente adicionado.", ColorInfo);
            }

            DebugUtility.Log(typeof(PhaseQaInstaller),"[QA][Phase] Para acessar o ContextMenu, selecione o GameObject 'QA_Phase' no Hierarchy (DontDestroyOnLoad).", ColorOk);
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
