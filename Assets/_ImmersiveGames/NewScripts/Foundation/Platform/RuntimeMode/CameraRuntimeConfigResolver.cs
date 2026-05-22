using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    public readonly struct CameraRuntimeResolvedConfig
    {
        public CameraRuntimeResolvedConfig(GameObject operationalCameraPrefab)
        {
            OperationalCameraPrefab = operationalCameraPrefab ?? throw new ArgumentNullException(nameof(operationalCameraPrefab));
        }

        public GameObject OperationalCameraPrefab { get; }
    }

    public static class CameraRuntimeConfigResolver
    {
        public static CameraRuntimeResolvedConfig ResolveOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][CameraRuntime] RuntimeModeConfig obrigatorio ausente para resolver camera runtime config.");
            }

            if (!RuntimeConfigRegistry.TryGetSnapshot(out IRuntimeConfigSnapshotReadOnly snapshot) || snapshot == null)
            {
                throw new InvalidOperationException("[FATAL][Config][CameraRuntime] RuntimeConfigRegistry snapshot obrigatorio ausente para camera runtime config.");
            }

            ICameraRuntimeConfigGroupReadOnly cameraRuntime = snapshot.CameraRuntime
                ?? throw new InvalidOperationException("[FATAL][Config][CameraRuntime] RuntimeConfigRegistry invariant breach: snapshot.CameraRuntime obrigatorio ausente.");

            GameObject operationalCameraPrefab = cameraRuntime.OperationalCameraPrefab;
            if (operationalCameraPrefab == null)
            {
                throw new InvalidOperationException("[FATAL][Config][CameraRuntime] RuntimeConfigRegistry invariant breach: operationalCameraPrefab obrigatorio ausente.");
            }

            Camera[] cameras = operationalCameraPrefab.GetComponentsInChildren<Camera>(true);
            int cameraCount = cameras?.Length ?? 0;
            if (cameraCount != 1)
            {
                throw new InvalidOperationException($"[FATAL][Config][CameraRuntime] RuntimeConfigRegistry invariant breach: operationalCameraPrefab deve conter exatamente uma Camera. observed='{cameraCount}'.");
            }

            return new CameraRuntimeResolvedConfig(operationalCameraPrefab);
        }
    }
}
