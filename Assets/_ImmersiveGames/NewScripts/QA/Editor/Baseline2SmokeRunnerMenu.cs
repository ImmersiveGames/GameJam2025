#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Editor
{
    public static class Baseline2SmokeRunnerMenu
    {
        private const string RunKey = "NewScripts.Baseline2Smoke.RunRequested";

        [MenuItem("NewScripts/QA/Run Baseline 2.0 Smoke (Production Flow)")]
        public static void RunBaseline2Smoke()
        {
            PlayerPrefs.SetInt(RunKey, 1);
            PlayerPrefs.Save();

            if (!EditorApplication.isPlaying)
                EditorApplication.isPlaying = true;

            Debug.Log("[Baseline2Smoke] Run requested. Entering Play Mode...");
        }
    }
}
#endif
