#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Baseline.Editor
{
    public static class Baseline2SmokeMenu
    {
        [MenuItem("NewScripts/QA/Baseline 2.0/Run (Manual Play Click)")]
        public static void RunManual()
        {
            PlayerPrefs.SetInt(_ImmersiveGames.NewScripts.QA.Baseline.Baseline2SmokeRunner.RunKey, 1);
            PlayerPrefs.SetInt("NewScripts.Baseline2Smoke.ManualPlay", 1);
            PlayerPrefs.SetFloat("NewScripts.Baseline2Smoke.AutoNavigateTimeoutSeconds", 25f);
            PlayerPrefs.Save();

            Debug.Log("[Baseline2Smoke] Run requested: MANUAL (will wait for Menu Play click). Entering Play Mode...");

#if UNITY_2020_2_OR_NEWER
            if (!EditorApplication.isPlaying) EditorApplication.EnterPlaymode();
#else
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
#endif
        }

        [MenuItem("NewScripts/QA/Baseline 2.0/Run (Auto Navigate)")]
        public static void RunAuto()
        {
            PlayerPrefs.SetInt(_ImmersiveGames.NewScripts.QA.Baseline.Baseline2SmokeRunner.RunKey, 1);
            PlayerPrefs.SetInt("NewScripts.Baseline2Smoke.ManualPlay", 0);
            PlayerPrefs.SetFloat("NewScripts.Baseline2Smoke.AutoNavigateTimeoutSeconds", 25f);
            PlayerPrefs.Save();

            Debug.Log("[Baseline2Smoke] Run requested: AUTO (will call IGameNavigationService). Entering Play Mode...");

#if UNITY_2020_2_OR_NEWER
            if (!EditorApplication.isPlaying) EditorApplication.EnterPlaymode();
#else
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
#endif
        }
    }
}
#endif
