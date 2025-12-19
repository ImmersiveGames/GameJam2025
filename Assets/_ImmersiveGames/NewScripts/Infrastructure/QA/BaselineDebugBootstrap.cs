using System.Collections;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    internal static class BaselineDebugBootstrap
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const string DriverName = "BaselineDebugBootstrapDriver";

        private static bool _initialized;
        private static bool _ownershipTaken;
        private static bool _restored;
        private static bool _hasSavedPrevious;
        private static bool _previousRepeatedVerbose = true;
        private static GameObject _driverObject;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _initialized = false;
            _ownershipTaken = false;
            _restored = false;
            _hasSavedPrevious = false;
            _previousRepeatedVerbose = true;
            DestroyDriverIfExists();
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void DisableRepeatedCallWarningsPreScene()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _previousRepeatedVerbose = DebugUtility.GetRepeatedCallVerbose();
            _hasSavedPrevious = true;

            DebugUtility.SetRepeatedCallVerbose(false);
            DebugUtility.Log(typeof(BaselineDebugBootstrap),
                "[Baseline] Repeated-call warning desabilitado no bootstrap (pre-scene-load).");

            CreateDriver();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal static bool TryTakeOwnership(out bool previousVerbose)
        {
            previousVerbose = _previousRepeatedVerbose;

            if (!_hasSavedPrevious)
            {
                previousVerbose = DebugUtility.GetRepeatedCallVerbose();
                _previousRepeatedVerbose = previousVerbose;
                _hasSavedPrevious = true;
            }

            if (_ownershipTaken)
            {
                return false;
            }

            _ownershipTaken = true;
            return true;
        }

        internal static void RestoreIfNeeded(bool previousVerbose)
        {
            if (_restored)
            {
                return;
            }

            DebugUtility.SetRepeatedCallVerbose(previousVerbose);
            _restored = true;
            _ownershipTaken = false;
            DestroyDriverIfExists();
        }

        private static void CreateDriver()
        {
            if (_driverObject != null)
            {
                return;
            }

            _driverObject = new GameObject(DriverName)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            Object.DontDestroyOnLoad(_driverObject);
            _driverObject.AddComponent<BaselineDebugBootstrapDriver>();
        }

        private static void DestroyDriverIfExists()
        {
            if (_driverObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(_driverObject);
            }
            else
            {
                Object.DestroyImmediate(_driverObject);
            }

            _driverObject = null;
        }

        private static void RestoreIfNotOwned()
        {
            if (_ownershipTaken || _restored || !_hasSavedPrevious)
            {
                return;
            }

            DebugUtility.SetRepeatedCallVerbose(_previousRepeatedVerbose);
            _restored = true;
        }

        private sealed class BaselineDebugBootstrapDriver : MonoBehaviour
        {
            private IEnumerator Start()
            {
                yield return null;
                RestoreIfNotOwned();
                Destroy(gameObject);
            }

            private void OnDestroy()
            {
                if (ReferenceEquals(_driverObject, gameObject))
                {
                    _driverObject = null;
                }
            }
        }
#else
        internal static bool TryTakeOwnership(out bool previousVerbose)
        {
            previousVerbose = DebugUtility.GetRepeatedCallVerbose();
            return false;
        }

        internal static void RestoreIfNeeded(bool previousVerbose)
        {
            // No-op em builds Release.
        }
#endif
    }
}
