using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    public static class UnityOperationalInputRuntimeAdapter
    {
        public static void PrepareOrFail(
            RuntimeModeConfig runtimeModeConfig,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            var config = InputModesRuntimeConfigResolver.ResolveOrFail(runtimeModeConfig);
            var slotsContext = SessionPlayerSlotsRuntimeResolver.ResolveOrFail(
                runtimeModeConfig,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason);

            var eventSystem = EnsureCanonicalEventSystemOnPersistentRoot(
                slotsContext.PersistentRoot,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason);

            EnsureCanonicalInputSystemUiInputModule(
                slotsContext.PersistentRoot,
                eventSystem,
                config,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason);
        }

        private static EventSystem EnsureCanonicalEventSystemOnPersistentRoot(
            Transform persistentRoot,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            DebugUtility.LogVerbose(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("SessionEventSystemEnsureStarted", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"persistentRoot='{persistentRoot.name}'"),
                DebugUtility.Colors.Info);

            EventSystem[] eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            int observedCount = eventSystems?.Length ?? 0;
            DebugUtility.LogVerbose(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("EventSystemObserved", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"observedCount='{observedCount}' persistentRoot='{persistentRoot.name}'"),
                DebugUtility.Colors.Info);

            if (eventSystems == null || eventSystems.Length == 0)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.transform.SetParent(persistentRoot, worldPositionStays: false);
                var created = eventSystemGo.AddComponent<EventSystem>();

                DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                    BuildLog("EventSystemCreated", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                        $"eventSystem='{created.name}' persistentRoot='{persistentRoot.name}'"),
                    DebugUtility.Colors.Info);

                DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                    BuildLog("SessionEventSystemReady", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                        $"eventSystem='{created.name}' persistentRoot='{persistentRoot.name}'"),
                    DebugUtility.Colors.Success);
                return created;
            }

            if (eventSystems.Length > 1)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"EventSystem duplicado detectado. count='{eventSystems.Length}'.");
            }

            var observed = eventSystems[0];
            var observedRoot = observed.transform.root;
            if (!ReferenceEquals(observedRoot, persistentRoot))
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"EventSystem fora do root persistente. eventSystem='{observed.name}' observedRoot='{observedRoot.name}' expectedRoot='{persistentRoot.name}'.");
            }

            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("SessionEventSystemReady", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"eventSystem='{observed.name}' persistentRoot='{persistentRoot.name}'"),
                DebugUtility.Colors.Success);
            return observed;
        }

        private static void EnsureCanonicalInputSystemUiInputModule(
            Transform persistentRoot,
            EventSystem eventSystem,
            InputModesRuntimeResolvedConfig config,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (eventSystem == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    "EventSystem invalido para preparar o InputSystemUIInputModule canonico.");
            }

            DebugUtility.LogVerbose(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("SessionInputModuleEnsureStarted", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"eventSystem='{eventSystem.name}' persistentRoot='{persistentRoot.name}'"),
                DebugUtility.Colors.Info);

            var standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneInputModule != null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"StandaloneInputModule nao suportado no EventSystem canonicamente persistente. eventSystem='{eventSystem.name}'.");
            }

            InputSystemUIInputModule[] modules = UnityEngine.Object.FindObjectsByType<InputSystemUIInputModule>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            int observedCount = modules?.Length ?? 0;
            DebugUtility.LogVerbose(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("InputSystemUIInputModuleObserved", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"observedCount='{observedCount}' eventSystem='{eventSystem.name}'"),
                DebugUtility.Colors.Info);

            if (modules is { Length: > 1 })
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"InputSystemUIInputModule duplicado detectado. count='{modules.Length}'.");
            }

            var moduleOnEventSystem = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (moduleOnEventSystem != null)
            {
                BindCanonicalUiInputActionsOrFail(
                    moduleOnEventSystem,
                    config,
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    source,
                    reason);

                DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                    BuildLog("SessionInputModuleReady", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                        $"inputModule='{moduleOnEventSystem.name}' eventSystem='{eventSystem.name}'"),
                    DebugUtility.Colors.Success);
                return;
            }

            if (modules is { Length: 1 })
            {
                var observed = modules[0];
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"InputSystemUIInputModule fora do EventSystem persistente. inputModule='{observed.name}' moduleRoot='{observed.transform.root.name}' eventSystem='{eventSystem.name}' expectedRoot='{persistentRoot.name}'.");
            }

            var created = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("InputSystemUIInputModuleCreated", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"inputModule='{created.name}' eventSystem='{eventSystem.name}'"),
                DebugUtility.Colors.Info);

            BindCanonicalUiInputActionsOrFail(
                created,
                config,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason);

            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("SessionInputModuleReady", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"inputModule='{created.name}' eventSystem='{eventSystem.name}'"),
                DebugUtility.Colors.Success);
        }

        private static void BindCanonicalUiInputActionsOrFail(
            InputSystemUIInputModule module,
            InputModesRuntimeResolvedConfig config,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (module == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    "InputSystemUIInputModule invalido para binding de UI actions.");
            }

            DebugUtility.LogVerbose(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("SessionUiInputActionsBindingStarted", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"inputModule='{module.name}'"),
                DebugUtility.Colors.Info);

            if (config.UiActionsAsset == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    "uiActionsAsset canonico ausente.");
            }

            DebugUtility.LogVerbose(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("UiInputActionsAssetObserved", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"asset='{config.UiActionsAsset.name}'"),
                DebugUtility.Colors.Info);

            RequireCanonicalReferenceOrFail(config.UiActionsAsset, config.UiPoint, "uiPoint", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireCanonicalReferenceOrFail(config.UiActionsAsset, config.UiLeftClick, "uiLeftClick", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireCanonicalReferenceOrFail(config.UiActionsAsset, config.UiRightClick, "uiRightClick", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireCanonicalReferenceOrFail(config.UiActionsAsset, config.UiMiddleClick, "uiMiddleClick", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireCanonicalReferenceOrFail(config.UiActionsAsset, config.UiScrollWheel, "uiScrollWheel", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireCanonicalReferenceOrFail(config.UiActionsAsset, config.UiMove, "uiMove", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireCanonicalReferenceOrFail(config.UiActionsAsset, config.UiSubmit, "uiSubmit", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireCanonicalReferenceOrFail(config.UiActionsAsset, config.UiCancel, "uiCancel", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireCanonicalReferenceOrFail(config.UiActionsAsset, config.UiTrackedDevicePosition, "uiTrackedDevicePosition", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireCanonicalReferenceOrFail(config.UiActionsAsset, config.UiTrackedDeviceOrientation, "uiTrackedDeviceOrientation", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);

            DebugUtility.LogVerbose(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("UiInputActionReferencesRequired", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"asset='{config.UiActionsAsset.name}' actionCount='10'"),
                DebugUtility.Colors.Info);

            module.UnassignActions();
            DebugUtility.LogVerbose(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("InputSystemUIInputModuleUnassignedDefaults", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"inputModule='{module.name}'"),
                DebugUtility.Colors.Info);

            module.actionsAsset = config.UiActionsAsset;
            module.point = config.UiPoint;
            module.leftClick = config.UiLeftClick;
            module.rightClick = config.UiRightClick;
            module.middleClick = config.UiMiddleClick;
            module.scrollWheel = config.UiScrollWheel;
            module.move = config.UiMove;
            module.submit = config.UiSubmit;
            module.cancel = config.UiCancel;
            module.trackedDevicePosition = config.UiTrackedDevicePosition;
            module.trackedDeviceOrientation = config.UiTrackedDeviceOrientation;

            DebugUtility.LogVerbose(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("InputSystemUIInputModuleBound", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"inputModule='{module.name}' asset='{config.UiActionsAsset.name}' actionCount='10'"),
                DebugUtility.Colors.Info);

            ConfirmPostBindOrFail(module, config, routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);

            DebugUtility.LogVerbose(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("InputSystemUIInputModulePostBindConfirmed", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"inputModule='{module.name}' asset='{config.UiActionsAsset.name}' actionCount='10'"),
                DebugUtility.Colors.Success);

            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("SessionUiInputActionsReady", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"inputModule='{module.name}' asset='{config.UiActionsAsset.name}'"),
                DebugUtility.Colors.Success);
        }

        private static void ConfirmPostBindOrFail(
            InputSystemUIInputModule module,
            InputModesRuntimeResolvedConfig config,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (!ReferenceEquals(module.actionsAsset, config.UiActionsAsset))
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    "Post-bind invalid: module.actionsAsset mismatch.");
            }

            RequireBoundReferenceOrFail(module.point, config.UiPoint, config.UiActionsAsset, "point", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireBoundReferenceOrFail(module.leftClick, config.UiLeftClick, config.UiActionsAsset, "leftClick", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireBoundReferenceOrFail(module.rightClick, config.UiRightClick, config.UiActionsAsset, "rightClick", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireBoundReferenceOrFail(module.middleClick, config.UiMiddleClick, config.UiActionsAsset, "middleClick", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireBoundReferenceOrFail(module.scrollWheel, config.UiScrollWheel, config.UiActionsAsset, "scrollWheel", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireBoundReferenceOrFail(module.move, config.UiMove, config.UiActionsAsset, "move", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireBoundReferenceOrFail(module.submit, config.UiSubmit, config.UiActionsAsset, "submit", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireBoundReferenceOrFail(module.cancel, config.UiCancel, config.UiActionsAsset, "cancel", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireBoundReferenceOrFail(module.trackedDevicePosition, config.UiTrackedDevicePosition, config.UiActionsAsset, "trackedDevicePosition", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            RequireBoundReferenceOrFail(module.trackedDeviceOrientation, config.UiTrackedDeviceOrientation, config.UiActionsAsset, "trackedDeviceOrientation", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
        }

        private static void RequireBoundReferenceOrFail(
            InputActionReference observed,
            InputActionReference expected,
            InputActionAsset expectedAsset,
            string fieldName,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (!ReferenceEquals(observed, expected))
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"Post-bind invalid: module.{fieldName} mismatch.");
            }

            if (observed == null || observed.action == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"Post-bind invalid: module.{fieldName}.action ausente.");
            }

            var observedAsset = observed.action.actionMap?.asset;
            if (!ReferenceEquals(observedAsset, expectedAsset))
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"Post-bind invalid: module.{fieldName} fora do uiActionsAsset canonico.");
            }
        }

        private static void RequireCanonicalReferenceOrFail(
            InputActionAsset expectedAsset,
            InputActionReference reference,
            string fieldName,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (reference == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"{fieldName} obrigatorio ausente.");
            }

            if (reference.action == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"{fieldName}.action obrigatoria ausente.");
            }

            var actionAsset = reference.action.actionMap?.asset;
            if (!ReferenceEquals(actionAsset, expectedAsset))
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"{fieldName} fora do uiActionsAsset canonico.");
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
                "OperationalInputRuntimeEnsureFailed",
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason,
                detail);

            DebugUtility.LogError(typeof(UnityOperationalInputRuntimeAdapter), $"[FATAL][Config][OperationalInputRuntime] {message}");
            return new InvalidOperationException($"[FATAL][Config][OperationalInputRuntime] {message}");
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
            return $"event='{eventName}' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{source}' reason='{reason}' {extra}.";
        }

    }
}
