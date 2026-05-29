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
            InputModesRuntimeResolvedConfig config = InputModesRuntimeConfigResolver.ResolveOrFail(runtimeModeConfig);
            SessionPlayerSlotsValidationContext slotsContext = SessionPlayerSlotsValidator.ValidateOrFail(
                runtimeModeConfig,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason);

            EventSystem eventSystem = ValidateOrCreateEventSystemOnPersistentRoot(
                slotsContext.PersistentRoot,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason);

            ValidateOrCreateInputSystemUiInputModule(
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

        private static EventSystem ValidateOrCreateEventSystemOnPersistentRoot(
            Transform persistentRoot,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("SessionEventSystemValidationStarted", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"persistentRoot='{persistentRoot.name}'"),
                DebugUtility.Colors.Info);

            EventSystem[] eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            int observedCount = eventSystems?.Length ?? 0;
            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("EventSystemObserved", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"observedCount='{observedCount}' persistentRoot='{persistentRoot.name}'"),
                DebugUtility.Colors.Info);

            if (eventSystems == null || eventSystems.Length == 0)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.transform.SetParent(persistentRoot, worldPositionStays: false);
                EventSystem created = eventSystemGo.AddComponent<EventSystem>();

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

            EventSystem observed = eventSystems[0];
            Transform observedRoot = observed.transform.root;
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

        private static void ValidateOrCreateInputSystemUiInputModule(
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
                    "EventSystem invalido para validacao do InputSystemUIInputModule.");
            }

            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("SessionInputModuleValidationStarted", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"eventSystem='{eventSystem.name}' persistentRoot='{persistentRoot.name}'"),
                DebugUtility.Colors.Info);

            StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneInputModule != null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"StandaloneInputModule nao suportado no EventSystem canonicamente persistente. eventSystem='{eventSystem.name}'.");
            }

            InputSystemUIInputModule[] modules = UnityEngine.Object.FindObjectsByType<InputSystemUIInputModule>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            int observedCount = modules?.Length ?? 0;
            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("InputSystemUIInputModuleObserved", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"observedCount='{observedCount}' eventSystem='{eventSystem.name}'"),
                DebugUtility.Colors.Info);

            if (modules != null && modules.Length > 1)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"InputSystemUIInputModule duplicado detectado. count='{modules.Length}'.");
            }

            InputSystemUIInputModule moduleOnEventSystem = eventSystem.GetComponent<InputSystemUIInputModule>();
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

            if (modules != null && modules.Length == 1)
            {
                InputSystemUIInputModule observed = modules[0];
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"InputSystemUIInputModule fora do EventSystem persistente. inputModule='{observed.name}' moduleRoot='{observed.transform.root.name}' eventSystem='{eventSystem.name}' expectedRoot='{persistentRoot.name}'.");
            }

            InputSystemUIInputModule created = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

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

            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("SessionUiInputActionsValidationStarted", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"inputModule='{module.name}'"),
                DebugUtility.Colors.Info);

            if (config.UiActionsAsset == null)
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    "uiActionsAsset canonico ausente.");
            }

            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("UiInputActionsAssetObserved", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"asset='{config.UiActionsAsset.name}'"),
                DebugUtility.Colors.Info);

            ValidateCanonicalReferenceOrFail(config.UiActionsAsset, config.UiPoint, "uiPoint", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateCanonicalReferenceOrFail(config.UiActionsAsset, config.UiLeftClick, "uiLeftClick", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateCanonicalReferenceOrFail(config.UiActionsAsset, config.UiRightClick, "uiRightClick", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateCanonicalReferenceOrFail(config.UiActionsAsset, config.UiMiddleClick, "uiMiddleClick", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateCanonicalReferenceOrFail(config.UiActionsAsset, config.UiScrollWheel, "uiScrollWheel", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateCanonicalReferenceOrFail(config.UiActionsAsset, config.UiMove, "uiMove", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateCanonicalReferenceOrFail(config.UiActionsAsset, config.UiSubmit, "uiSubmit", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateCanonicalReferenceOrFail(config.UiActionsAsset, config.UiCancel, "uiCancel", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateCanonicalReferenceOrFail(config.UiActionsAsset, config.UiTrackedDevicePosition, "uiTrackedDevicePosition", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateCanonicalReferenceOrFail(config.UiActionsAsset, config.UiTrackedDeviceOrientation, "uiTrackedDeviceOrientation", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);

            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("UiInputActionReferencesValidated", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"asset='{config.UiActionsAsset.name}' actionCount='10'"),
                DebugUtility.Colors.Info);

            module.UnassignActions();
            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
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

            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("InputSystemUIInputModuleBound", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"inputModule='{module.name}' asset='{config.UiActionsAsset.name}' actionCount='10'"),
                DebugUtility.Colors.Info);

            ValidatePostBindOrFail(module, config, routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);

            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("InputSystemUIInputModulePostBindValidated", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"inputModule='{module.name}' asset='{config.UiActionsAsset.name}' actionCount='10'"),
                DebugUtility.Colors.Success);

            DebugUtility.Log(typeof(UnityOperationalInputRuntimeAdapter),
                BuildLog("SessionUiInputActionsReady", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"inputModule='{module.name}' asset='{config.UiActionsAsset.name}'"),
                DebugUtility.Colors.Success);
        }

        private static void ValidatePostBindOrFail(
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

            ValidateBoundReferenceOrFail(module.point, config.UiPoint, config.UiActionsAsset, "point", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateBoundReferenceOrFail(module.leftClick, config.UiLeftClick, config.UiActionsAsset, "leftClick", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateBoundReferenceOrFail(module.rightClick, config.UiRightClick, config.UiActionsAsset, "rightClick", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateBoundReferenceOrFail(module.middleClick, config.UiMiddleClick, config.UiActionsAsset, "middleClick", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateBoundReferenceOrFail(module.scrollWheel, config.UiScrollWheel, config.UiActionsAsset, "scrollWheel", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateBoundReferenceOrFail(module.move, config.UiMove, config.UiActionsAsset, "move", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateBoundReferenceOrFail(module.submit, config.UiSubmit, config.UiActionsAsset, "submit", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateBoundReferenceOrFail(module.cancel, config.UiCancel, config.UiActionsAsset, "cancel", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateBoundReferenceOrFail(module.trackedDevicePosition, config.UiTrackedDevicePosition, config.UiActionsAsset, "trackedDevicePosition", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
            ValidateBoundReferenceOrFail(module.trackedDeviceOrientation, config.UiTrackedDeviceOrientation, config.UiActionsAsset, "trackedDeviceOrientation", routeIdentity, routeOperationId, transitionId, routeSequence, source, reason);
        }

        private static void ValidateBoundReferenceOrFail(
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

            InputActionAsset observedAsset = observed.action.actionMap?.asset;
            if (!ReferenceEquals(observedAsset, expectedAsset))
            {
                throw BuildFatal(routeIdentity, routeOperationId, transitionId, routeSequence, source, reason,
                    $"Post-bind invalid: module.{fieldName} fora do uiActionsAsset canonico.");
            }
        }

        private static void ValidateCanonicalReferenceOrFail(
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

            InputActionAsset actionAsset = reference.action.actionMap?.asset;
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
                "OperationalInputRuntimeValidationFailed",
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
            return $"[OBS][OperationalInputRuntime] event='{eventName}' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{source}' reason='{reason}' {extra}.";
        }

    }
}
