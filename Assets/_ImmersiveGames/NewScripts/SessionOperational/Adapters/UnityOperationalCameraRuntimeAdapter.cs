using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SessionOperational.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public static class UnityOperationalCameraRuntimeAdapter
    {
        public static Camera CurrentOperationalCamera { get; private set; }

        public static Camera EnsureOperationalCameraOrFail(
            RuntimeModeConfig runtimeModeConfig,
            string source,
            string reason)
        {
            return EnsureOperationalCameraOrFail(
                runtimeModeConfig,
                "<bootstrap>",
                "<bootstrap>",
                "<bootstrap>",
                0,
                source,
                reason);
        }

        private static Camera EnsureOperationalCameraOrFail(
            RuntimeModeConfig runtimeModeConfig,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            DebugUtility.Log(typeof(UnityOperationalCameraRuntimeAdapter),
                BuildLog("OperationalCameraRuntimeValidationStarted", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    "status='started'"),
                DebugUtility.Colors.Info);

            var config = CameraRuntimeConfigResolver.ResolveOrFail(runtimeModeConfig);
            var prefab = config.OperationalCameraPrefab;
            if (prefab == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    "operationalCameraPrefab canonico ausente.");
            }

            Camera[] prefabCameras = prefab.GetComponentsInChildren<Camera>(true);
            int prefabCameraCount = prefabCameras?.Length ?? 0;
            var prefabMarker = prefab.GetComponent<OperationalCameraRuntimeMarker>();
            var prefabPersistentRoot = prefab.GetComponent<PersistentRuntimeObject>();
            DebugUtility.Log(typeof(UnityOperationalCameraRuntimeAdapter),
                BuildLog("OperationalCameraPrefabObserved", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"prefab='{prefab.name}' cameraCount='{prefabCameraCount}' markerOnRoot='{(prefabMarker != null)}' persistentRoot='{(prefabPersistentRoot != null)}'"),
                DebugUtility.Colors.Info);

            if (prefabCameraCount != 1)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"operationalCameraPrefab invalido. cameraCount='{prefabCameraCount}' expected='1'.");
            }
            if (prefabMarker == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    "operationalCameraPrefab invalido. OperationalCameraRuntimeMarker obrigatorio ausente no root.");
            }
            if (prefabPersistentRoot == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    "operationalCameraPrefab invalido. PersistentRuntimeObject obrigatorio ausente no root.");
            }

            DebugUtility.Log(typeof(UnityOperationalCameraRuntimeAdapter),
                BuildLog("OperationalCameraPersistenceValidated", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"prefab='{prefab.name}' markerOnRoot='true' persistentRoot='true'"),
                DebugUtility.Colors.Info);

            OperationalCameraRuntimeMarker[] markers = UnityEngine.Object.FindObjectsByType<OperationalCameraRuntimeMarker>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            var observedCamera = ResolveActiveOperationalCameraOrFail(
                markers,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason);

            if (observedCamera == null)
            {
                var instance = UnityEngine.Object.Instantiate(prefab);
                instance.name = prefab.name;

                ValidateOperationalRootPersistenceOrFail(
                    instance.transform,
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    source,
                    reason,
                    "instancia criada");

                var createdMarker = instance.GetComponent<OperationalCameraRuntimeMarker>();
                if (createdMarker == null || !ReferenceEquals(createdMarker.transform, instance.transform))
                {
                    throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                        "instancia criada sem OperationalCameraRuntimeMarker valido no root.");
                }

                var createdCamera = createdMarker.GetComponentInChildren<Camera>(true);
                if (createdCamera == null)
                {
                    throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                        "operationalCameraPrefab instanciado sem Camera valida.");
                }
                UnityEngine.Object.DontDestroyOnLoad(instance);

                DebugUtility.Log(typeof(UnityOperationalCameraRuntimeAdapter),
                    BuildLog("OperationalCameraCreated", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                        $"camera='{createdCamera.name}' marker='{createdMarker.name}'"),
                    DebugUtility.Colors.Info);

                DebugUtility.Log(typeof(UnityOperationalCameraRuntimeAdapter),
                    BuildLog("OperationalCameraPersistenceReady", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                        $"cameraRoot='{instance.name}' persistentRoot='true'"),
                    DebugUtility.Colors.Success);

                CurrentOperationalCamera = createdCamera;
                DebugUtility.Log(typeof(UnityOperationalCameraRuntimeAdapter),
                    BuildLog("OperationalCameraReady", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                        $"camera='{createdCamera.name}'"),
                    DebugUtility.Colors.Success);
                return createdCamera;
            }

            ValidateOperationalRootPersistenceOrFail(
                observedCamera.transform.root,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason,
                "camera canonica observada");

            DebugUtility.Log(typeof(UnityOperationalCameraRuntimeAdapter),
                BuildLog("OperationalCameraPersistenceReady", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"cameraRoot='{observedCamera.transform.root.name}' persistentRoot='true'"),
                DebugUtility.Colors.Success);

            CurrentOperationalCamera = observedCamera;
            DebugUtility.Log(typeof(UnityOperationalCameraRuntimeAdapter),
                BuildLog("OperationalCameraReady", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"camera='{observedCamera.name}'"),
                DebugUtility.Colors.Success);
            return observedCamera;
        }

        private static Camera ResolveActiveOperationalCameraOrFail(
            OperationalCameraRuntimeMarker[] markers,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            int canonicalCount = 0;
            Camera lastObserved = null;

            if (markers != null)
            {
                foreach (var marker in markers)
                {
                    if (marker == null || !marker.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    var markerRoot = marker.transform.root;
                    if (!ReferenceEquals(marker.transform, markerRoot))
                    {
                        throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                            $"OperationalCameraRuntimeMarker invalido. marker='{marker.name}' deve estar no root persistente.");
                    }

                    Camera[] markerCameras = marker.GetComponentsInChildren<Camera>(true);
                    int markerCameraCount = markerCameras?.Length ?? 0;
                    if (markerCameraCount != 1)
                    {
                        throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                            $"OperationalCameraRuntimeMarker invalido. marker='{marker.name}' cameraCount='{markerCameraCount}' expected='1'.");
                    }

                    var candidate = markerCameras?[0];
                    if (candidate == null || !candidate.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    canonicalCount += 1;
                    lastObserved = candidate;
                }
            }

            DebugUtility.Log(typeof(UnityOperationalCameraRuntimeAdapter),
                BuildLog("OperationalCameraObserved", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"canonicalCount='{canonicalCount}'"),
                DebugUtility.Colors.Info);

            if (canonicalCount > 1)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"multiplas cameras operacionais canonicas ativas detectadas. count='{canonicalCount}'.");
            }

            return lastObserved;
        }

        private static void ValidateOperationalRootPersistenceOrFail(
            Transform rootTransform,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason,
            string context)
        {
            if (rootTransform == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"{context} com root invalido.");
            }

            var rootMarker = rootTransform.GetComponent<OperationalCameraRuntimeMarker>();
            if (rootMarker == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"{context} sem OperationalCameraRuntimeMarker no root.");
            }

            var persistentRoot = rootTransform.GetComponent<PersistentRuntimeObject>();
            if (persistentRoot == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"{context} sem PersistentRuntimeObject no root.");
            }
        }

        private static InvalidOperationException BuildFatal(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason,
            string detail)
        {
            string message = BuildLog(
                "SessionOperationalCameraRuntimeFailed",
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason,
                detail);
            DebugUtility.LogError(typeof(UnityOperationalCameraRuntimeAdapter), $"[FATAL][Config][SessionOperationalCameraRuntime] {message}");
            return new InvalidOperationException($"[FATAL][Config][SessionOperationalCameraRuntime] {message}");
        }

        private static string BuildLog(
            string eventName,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason,
            string extra)
        {
            return $"[OBS][SessionOperationalCameraRuntime] event='{eventName}' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{source}' reason='{reason}' {extra}.";
        }
    }
}
