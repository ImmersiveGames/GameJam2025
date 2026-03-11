#if UNITY_EDITOR
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Dev.Bindings;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.ContentSwap.Editor
{
    public static class ContentSwapQaMenuItems
    {
        [MenuItem("ImmersiveGames/NewScripts/QA/ContentSwap/Select QA_ContentSwap Object", priority = 1310)]
        private static void SelectQaObject()
        {
            GameObject obj = GameObject.Find("QA_ContentSwap");
            if (obj != null)
            {
                Selection.activeObject = obj;
                return;
            }

            DebugUtility.Log(typeof(ContentSwapDevContextMenu),
                "[QA][ContentSwap] QA_ContentSwap nao encontrado no Hierarchy (Play Mode).",
                "#FFC107");
        }
    }
}
#endif
