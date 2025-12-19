using System.Collections;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Supressão defensiva global dos avisos de chamadas repetidas antes do carregamento da cena.
    /// Restaura automaticamente apenas se nenhum runner estiver ativo.
    /// </summary>
    internal static class BaselineDebugBootstrap
    {
        internal static bool IsBaselineRunning { get; set; }
        internal static bool IsRunnerActive { get; private set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const string DriverName = "BaselineDebugBootstrapDriver";

        private static bool _hasSavedPrevious;
        private static bool _previousRepeatedVerbose = true;
        private static GameObject _driverObject;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsBaselineRunning = false;
            IsRunnerActive = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _hasSavedPrevious = false;
            _previousRepeatedVerbose = true;
            DestroyDriverIfExists();
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

            CreateDriver();
#endif
        }

        internal static void SetRunnerActive(bool active)
        {
            IsRunnerActive = active;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
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

        private sealed class BaselineDebugBootstrapDriver : MonoBehaviour
        {
            private IEnumerator Start()
            {
                yield return null;

                if (!_hasSavedPrevious)
                {
                    Destroy(gameObject);
                    yield break;
                }

                if (IsRunnerActive)
                {
                    DebugUtility.Log(typeof(BaselineDebugBootstrap),
                        "[Baseline] Repeated-call warning: skip restore (runner ativo).");
                    Destroy(gameObject);
                    yield break;
                }

                DebugUtility.SetRepeatedCallVerbose(_previousRepeatedVerbose);
                DebugUtility.Log(typeof(BaselineDebugBootstrap),
                    "[Baseline] Repeated-call warning restaurado pelo bootstrap driver (nenhum runner ativo).");

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
#endif
    }
}
