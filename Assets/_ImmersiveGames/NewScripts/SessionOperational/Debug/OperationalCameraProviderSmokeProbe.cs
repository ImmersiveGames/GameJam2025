using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionOperational.Debug
{
    public sealed class OperationalCameraProviderSmokeProbe : MonoBehaviour
    {
        [ContextMenu("Session Operational/Operational Camera Provider/Run Smoke")]
        private void RunSmoke()
        {
            DependencyManager dependencyManager = DependencyManager.Instance;

            if (dependencyManager == null)
            {
                UnityEngine.Debug.LogError(
                    "[OBS][OperationalCameraProvider][SmokeProbe] SmokeFailed reason='dependency_manager_instance_missing'.");

                return;
            }

            if (!dependencyManager.TryGetGlobal<IOperationalCameraProvider>(
                    out IOperationalCameraProvider provider))
            {
                UnityEngine.Debug.LogError(
                    "[OBS][OperationalCameraProvider][SmokeProbe] SmokeFailed reason='operational_camera_provider_not_registered'.");

                return;
            }

            if (!provider.TryGetCurrent(
                    out OperationalCameraHandle handle,
                    out string reason))
            {
                UnityEngine.Debug.LogError(
                    $"[OBS][OperationalCameraProvider][SmokeProbe] SmokeFailed " +
                    $"reason='{reason}' " +
                    $"providerType='{provider.GetType().Name}'.");

                return;
            }

            if (handle == null)
            {
                UnityEngine.Debug.LogError(
                    $"[OBS][OperationalCameraProvider][SmokeProbe] SmokeFailed " +
                    $"reason='operational_camera_handle_null' " +
                    $"providerType='{provider.GetType().Name}'.");

                return;
            }

            if (handle.UnityCamera == null)
            {
                UnityEngine.Debug.LogError(
                    $"[OBS][OperationalCameraProvider][SmokeProbe] SmokeFailed " +
                    $"reason='unity_camera_null' " +
                    $"providerType='{provider.GetType().Name}' " +
                    $"handleSource='{handle.Source}' " +
                    $"handleReason='{handle.Reason}'.");

                return;
            }

            UnityEngine.Debug.Log(
                $"[OBS][OperationalCameraProvider][SmokeProbe] SmokeSucceeded " +
                $"providerType='{provider.GetType().Name}' " +
                $"camera='{handle.UnityCamera.name}' " +
                $"hasCinemachineBrain='{handle.HasCinemachineBrain}' " +
                $"cinemachineBrainType='{handle.CinemachineBrain?.GetType().Name ?? "<none>"}' " +
                $"handleSource='{handle.Source}' " +
                $"handleReason='{handle.Reason}' " +
                $"reason='{reason}'.");
        }
    }
}
