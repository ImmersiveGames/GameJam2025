#if UNITY_EDITOR
using UnityEditor;

namespace _ImmersiveGames.NewScripts.Core.Logging
{
    public static partial class HardFailFastH1
    {
        static partial void RequestEditorStopPlayMode()
        {
            EditorApplication.isPlaying = false;
        }
    }
}
#endif
