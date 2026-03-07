#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Core.Logging
{
    public sealed partial class DebugManagerConfig
    {
        [ContextMenu("Dev/Debug/Apply Settings")]
        private void ApplySettingsContextMenu()
        {
            ApplyConfiguration();
        }

        [ContextMenu("Dev/Debug/Print Current Debug State")]
        private void PrintStateContextMenu()
        {
            PrintCurrentDebugState();
        }
    }
}
#endif
