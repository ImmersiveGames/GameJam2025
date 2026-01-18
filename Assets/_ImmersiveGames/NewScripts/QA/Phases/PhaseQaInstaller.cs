// Assets/_ImmersiveGames/NewScripts/QA/Phases/PhaseQaInstaller.cs
#nullable enable
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Phases
{
    /// <summary>
    /// Instala o QA de Phases automaticamente (somente Editor/Development).
    /// Objetivo: garantir disponibilidade do Context Menu para evidências do ADR-0016
    /// sem depender de um GO presente em uma cena específica.
    /// </summary>
    public static class PhaseQaInstaller
    {
        private const string QaObjectName = "[QA] PhaseQA";
        private static bool _installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            EnsureInstalled();
#endif
        }

        public static void EnsureInstalled()
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            return;
#else
            if (_installed)
            {
                return;
            }

            _installed = true;

            // Se já existe em cena (ex.: você colocou manualmente na GameplayScene), não duplica.
            var existing = Object.FindObjectOfType<PhaseQaContextMenu>();
            if (existing != null)
            {
                DebugUtility.LogVerbose(typeof(PhaseQaInstaller),
                    $"[QA][Phase] PhaseQaContextMenu já existe em cena (GO='{existing.gameObject.name}'). Installer ignorado.",
                    DebugUtility.Colors.Info);
                return;
            }

            var go = new GameObject(QaObjectName);
            Object.DontDestroyOnLoad(go);
            go.AddComponent<PhaseQaContextMenu>();

            DebugUtility.Log(typeof(PhaseQaInstaller),
                $"[QA][Phase] Instalado automaticamente: {QaObjectName} (DontDestroyOnLoad).",
                DebugUtility.Colors.Success);
#endif
        }
    }
}
