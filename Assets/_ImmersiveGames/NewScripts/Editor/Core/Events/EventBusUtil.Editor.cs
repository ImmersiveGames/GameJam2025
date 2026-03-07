#if UNITY_EDITOR
using UnityEditor;

namespace _ImmersiveGames.NewScripts.Core.Events
{
    public static partial class EventBusUtil
    {
        [InitializeOnLoadMethod]
        private static void InitializeEditor()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                ClearAllBuses();
            }
        }
    }
}
#endif
