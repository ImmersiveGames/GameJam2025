#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_DEV
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Dev
{
    public static class WorldResetRequestHotkeyDevBootstrap
    {
        private const string HotkeyObjectName = "[WorldLifecycle] Reset Hotkey (Dev)";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Object.FindAnyObjectByType<WorldResetRequestHotkeyBridge>() != null)
            {
                return;
            }

            var go = new GameObject(HotkeyObjectName)
            {
                hideFlags = HideFlags.DontSave
            };
            Object.DontDestroyOnLoad(go);
            go.AddComponent<WorldResetRequestHotkeyBridge>();

            DebugUtility.LogVerbose(typeof(WorldResetRequestHotkeyDevBootstrap),
                "[WorldLifecycle] Hotkey DEV instalado (Shift+R) para RequestResetAsync.",
                DebugUtility.Colors.Info);
        }
    }
}
#endif