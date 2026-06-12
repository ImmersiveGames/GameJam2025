using System;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    public readonly struct InputModesRuntimeResolvedConfig
    {
        public InputModesRuntimeResolvedConfig(
            int maxPlayerSlots,
            InputActionAsset uiActionsAsset,
            InputActionReference uiPoint,
            InputActionReference uiLeftClick,
            InputActionReference uiRightClick,
            InputActionReference uiMiddleClick,
            InputActionReference uiScrollWheel,
            InputActionReference uiMove,
            InputActionReference uiSubmit,
            InputActionReference uiCancel,
            InputActionReference uiTrackedDevicePosition,
            InputActionReference uiTrackedDeviceOrientation)
        {
            MaxPlayerSlots = maxPlayerSlots;
            UiActionsAsset = uiActionsAsset;
            UiPoint = uiPoint;
            UiLeftClick = uiLeftClick;
            UiRightClick = uiRightClick;
            UiMiddleClick = uiMiddleClick;
            UiScrollWheel = uiScrollWheel;
            UiMove = uiMove;
            UiSubmit = uiSubmit;
            UiCancel = uiCancel;
            UiTrackedDevicePosition = uiTrackedDevicePosition;
            UiTrackedDeviceOrientation = uiTrackedDeviceOrientation;
        }

        public int MaxPlayerSlots { get; }
        public InputActionAsset UiActionsAsset { get; }
        public InputActionReference UiPoint { get; }
        public InputActionReference UiLeftClick { get; }
        public InputActionReference UiRightClick { get; }
        public InputActionReference UiMiddleClick { get; }
        public InputActionReference UiScrollWheel { get; }
        public InputActionReference UiMove { get; }
        public InputActionReference UiSubmit { get; }
        public InputActionReference UiCancel { get; }
        public InputActionReference UiTrackedDevicePosition { get; }
        public InputActionReference UiTrackedDeviceOrientation { get; }
    }

    public static class InputModesRuntimeConfigResolver
    {
        public static InputModesRuntimeResolvedConfig ResolveOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][InputModesRuntime] RuntimeModeConfig obrigatorio ausente para resolver InputModes runtime config.");
            }

            if (!RuntimeConfigRegistry.TryGetSnapshot(out var snapshot) || snapshot == null)
            {
                throw new InvalidOperationException("[FATAL][Config][InputModesRuntime] RuntimeConfigRegistry snapshot obrigatorio ausente para InputModes runtime config.");
            }

            var inputModes = snapshot.InputModesRuntime
                ?? throw new InvalidOperationException("[FATAL][Config][InputModesRuntime] RuntimeConfigRegistry invariant breach: snapshot.InputModesRuntime obrigatorio ausente.");

            if (inputModes.OperationalInputRuntimeProfile == null)
            {
                throw new InvalidOperationException("[FATAL][Config][InputModesRuntime] RuntimeConfigRegistry invariant breach: operationalInputRuntimeProfile obrigatorio ausente.");
            }

            if (string.IsNullOrWhiteSpace(inputModes.OperationalInputRuntimeProfileId))
            {
                throw new InvalidOperationException("[FATAL][Config][InputModesRuntime] RuntimeConfigRegistry invariant breach: operationalInputRuntimeProfile.profileId obrigatorio ausente.");
            }

            if (inputModes.MaxPlayerSlots < 1)
            {
                throw new InvalidOperationException($"[FATAL][Config][InputModesRuntime] RuntimeConfigRegistry invariant breach: maxPlayerSlots invalido='{inputModes.MaxPlayerSlots}'.");
            }

            if (inputModes.UiActionsAsset == null)
            {
                throw new InvalidOperationException("[FATAL][Config][InputModesRuntime] RuntimeConfigRegistry invariant breach: uiActionsAsset obrigatorio ausente.");
            }

            ValidateActionReferenceOrFail(inputModes.UiActionsAsset, inputModes.UiPoint, "uiPoint");
            ValidateActionReferenceOrFail(inputModes.UiActionsAsset, inputModes.UiLeftClick, "uiLeftClick");
            ValidateActionReferenceOrFail(inputModes.UiActionsAsset, inputModes.UiRightClick, "uiRightClick");
            ValidateActionReferenceOrFail(inputModes.UiActionsAsset, inputModes.UiMiddleClick, "uiMiddleClick");
            ValidateActionReferenceOrFail(inputModes.UiActionsAsset, inputModes.UiScrollWheel, "uiScrollWheel");
            ValidateActionReferenceOrFail(inputModes.UiActionsAsset, inputModes.UiMove, "uiMove");
            ValidateActionReferenceOrFail(inputModes.UiActionsAsset, inputModes.UiSubmit, "uiSubmit");
            ValidateActionReferenceOrFail(inputModes.UiActionsAsset, inputModes.UiCancel, "uiCancel");
            ValidateActionReferenceOrFail(inputModes.UiActionsAsset, inputModes.UiTrackedDevicePosition, "uiTrackedDevicePosition");
            ValidateActionReferenceOrFail(inputModes.UiActionsAsset, inputModes.UiTrackedDeviceOrientation, "uiTrackedDeviceOrientation");

            return new InputModesRuntimeResolvedConfig(
                inputModes.MaxPlayerSlots,
                inputModes.UiActionsAsset,
                inputModes.UiPoint,
                inputModes.UiLeftClick,
                inputModes.UiRightClick,
                inputModes.UiMiddleClick,
                inputModes.UiScrollWheel,
                inputModes.UiMove,
                inputModes.UiSubmit,
                inputModes.UiCancel,
                inputModes.UiTrackedDevicePosition,
                inputModes.UiTrackedDeviceOrientation);
        }

        private static void ValidateActionReferenceOrFail(
            InputActionAsset expectedAsset,
            InputActionReference reference,
            string fieldName)
        {
            if (reference == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][InputModesRuntime] RuntimeConfigRegistry invariant breach: {fieldName} obrigatorio ausente.");
            }

            if (reference.action == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][InputModesRuntime] RuntimeConfigRegistry invariant breach: {fieldName}.action obrigatoria ausente.");
            }

            var actionAsset = reference.action.actionMap?.asset;
            if (!ReferenceEquals(actionAsset, expectedAsset))
            {
                throw new InvalidOperationException($"[FATAL][Config][InputModesRuntime] RuntimeConfigRegistry invariant breach: {fieldName} fora do uiActionsAsset canonico.");
            }
        }
    }
}
