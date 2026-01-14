#nullable enable
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEditor;

namespace _ImmersiveGames.NewScripts.QA.Pregame.Editor
{
    public static class PregameQaMenuItems
    {
        [MenuItem("Tools/NewScripts/QA/Pregame/Complete (Force)")]
        private static void CompletePregame()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                DebugUtility.LogWarning(typeof(PregameQaMenuItems),
                    "[QA][Pregame] MenuItem requer Play Mode.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPregameControlService>(out var service) || service == null)
            {
                DebugUtility.LogWarning(typeof(PregameQaMenuItems),
                    "[QA][Pregame] IPregameControlService indisponível; Complete ignorado.");
                return;
            }

            service.CompletePregame("QA/MenuItem/Complete");
            DebugUtility.Log(typeof(PregameQaMenuItems),
                "[QA][Pregame] MenuItem CompletePregame solicitado.",
                DebugUtility.Colors.Info);
        }

        [MenuItem("Tools/NewScripts/QA/Pregame/Skip (Force)")]
        private static void SkipPregame()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                DebugUtility.LogWarning(typeof(PregameQaMenuItems),
                    "[QA][Pregame] MenuItem requer Play Mode.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPregameControlService>(out var service) || service == null)
            {
                DebugUtility.LogWarning(typeof(PregameQaMenuItems),
                    "[QA][Pregame] IPregameControlService indisponível; Skip ignorado.");
                return;
            }

            service.SkipPregame("QA/MenuItem/Skip");
            DebugUtility.Log(typeof(PregameQaMenuItems),
                "[QA][Pregame] MenuItem SkipPregame solicitado.",
                DebugUtility.Colors.Info);
        }
    }
}
