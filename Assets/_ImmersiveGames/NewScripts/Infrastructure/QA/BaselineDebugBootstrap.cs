using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    internal static class BaselineDebugBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void DisableRepeatedCallWarningsPreScene()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugUtility.SetRepeatedCallVerbose(false);
            DebugUtility.Log(typeof(BaselineDebugBootstrap),
                "[Baseline] Repeated-call warning desabilitado no bootstrap (pre-scene-load).");
#endif
        }
    }
}
