using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Supressão defensiva global dos avisos de chamadas repetidas antes do carregamento da cena.
    /// </summary>
    internal static class BaselineDebugBootstrap
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static bool _hasSavedPrevious;
        private static bool _previousRepeatedVerbose = true;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _hasSavedPrevious = false;
            _previousRepeatedVerbose = true;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void DisableRepeatedCallWarningsPreScene()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_hasSavedPrevious)
            {
                return;
            }

            _previousRepeatedVerbose = DebugUtility.GetRepeatedCallVerbose();
            _hasSavedPrevious = true;

            DebugUtility.SetRepeatedCallVerbose(false);
            DebugUtility.Log(typeof(BaselineDebugBootstrap),
                "[Baseline] Repeated-call warning desabilitado no bootstrap (pre-scene-load).");
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RestoreRepeatedCallWarningsAfterScene()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_hasSavedPrevious)
            {
                return;
            }

            DebugUtility.SetRepeatedCallVerbose(_previousRepeatedVerbose);
            DebugUtility.Log(typeof(BaselineDebugBootstrap),
                "[Baseline] Repeated-call warning restaurado após bootstrap (post-scene-load).");
#endif
        }
    }
}
