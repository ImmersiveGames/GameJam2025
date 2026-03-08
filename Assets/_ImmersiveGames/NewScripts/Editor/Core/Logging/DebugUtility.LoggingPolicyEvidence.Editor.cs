#if UNITY_EDITOR
using UnityEditor;

namespace _ImmersiveGames.NewScripts.Core.Logging
{
    internal static class DebugUtilityLoggingPolicyEvidenceMenu
    {
        [MenuItem("ImmersiveGames/NewScripts/Dev/Force LoggingPolicy Reapply Evidence")]
        private static void ForceLoggingPolicyReapplyEvidence()
        {
            DebugUtility.Dev_ForceReapplyLastLoggingPolicyForEvidence();
        }
    }
}
#endif
