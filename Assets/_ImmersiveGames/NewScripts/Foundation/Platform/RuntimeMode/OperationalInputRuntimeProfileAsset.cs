using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    [CreateAssetMenu(
        fileName = "OperationalInputRuntimeProfile",
        menuName = "ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/OperationalInputRuntimeProfileAsset",
        order = 24)]
    public sealed class OperationalInputRuntimeProfileAsset : ScriptableObject
    {
        [SerializeField] private string profileId;
        [SerializeField] private int maxPlayerSlots = 1;
        [SerializeField] private InputActionAsset uiActionsAsset;
        [SerializeField] private InputActionReference uiPoint;
        [SerializeField] private InputActionReference uiLeftClick;
        [SerializeField] private InputActionReference uiRightClick;
        [SerializeField] private InputActionReference uiMiddleClick;
        [SerializeField] private InputActionReference uiScrollWheel;
        [SerializeField] private InputActionReference uiMove;
        [SerializeField] private InputActionReference uiSubmit;
        [SerializeField] private InputActionReference uiCancel;
        [SerializeField] private InputActionReference uiTrackedDevicePosition;
        [SerializeField] private InputActionReference uiTrackedDeviceOrientation;

        public string ProfileId => Normalize(profileId);
        public int MaxPlayerSlots => maxPlayerSlots;
        public InputActionAsset UiActionsAsset => uiActionsAsset;
        public InputActionReference UiPoint => uiPoint;
        public InputActionReference UiLeftClick => uiLeftClick;
        public InputActionReference UiRightClick => uiRightClick;
        public InputActionReference UiMiddleClick => uiMiddleClick;
        public InputActionReference UiScrollWheel => uiScrollWheel;
        public InputActionReference UiMove => uiMove;
        public InputActionReference UiSubmit => uiSubmit;
        public InputActionReference UiCancel => uiCancel;
        public InputActionReference UiTrackedDevicePosition => uiTrackedDevicePosition;
        public InputActionReference UiTrackedDeviceOrientation => uiTrackedDeviceOrientation;

        public bool TryValidate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(ProfileId))
            {
                errorMessage = "operationalInputRuntimeProfile.profileId is required.";
                return false;
            }

            if (maxPlayerSlots < 1)
            {
                errorMessage = "operationalInputRuntimeProfile.maxPlayerSlots must be >= 1.";
                return false;
            }

            if (uiActionsAsset == null)
            {
                errorMessage = "operationalInputRuntimeProfile.uiActionsAsset is required.";
                return false;
            }

            if (!TryValidateActionReference(uiActionsAsset, uiPoint, "uiPoint", out errorMessage)) return false;
            if (!TryValidateActionReference(uiActionsAsset, uiLeftClick, "uiLeftClick", out errorMessage)) return false;
            if (!TryValidateActionReference(uiActionsAsset, uiRightClick, "uiRightClick", out errorMessage)) return false;
            if (!TryValidateActionReference(uiActionsAsset, uiMiddleClick, "uiMiddleClick", out errorMessage)) return false;
            if (!TryValidateActionReference(uiActionsAsset, uiScrollWheel, "uiScrollWheel", out errorMessage)) return false;
            if (!TryValidateActionReference(uiActionsAsset, uiMove, "uiMove", out errorMessage)) return false;
            if (!TryValidateActionReference(uiActionsAsset, uiSubmit, "uiSubmit", out errorMessage)) return false;
            if (!TryValidateActionReference(uiActionsAsset, uiCancel, "uiCancel", out errorMessage)) return false;
            if (!TryValidateActionReference(uiActionsAsset, uiTrackedDevicePosition, "uiTrackedDevicePosition", out errorMessage)) return false;
            if (!TryValidateActionReference(uiActionsAsset, uiTrackedDeviceOrientation, "uiTrackedDeviceOrientation", out errorMessage)) return false;

            errorMessage = string.Empty;
            return true;
        }

        private static bool TryValidateActionReference(
            InputActionAsset expectedAsset,
            InputActionReference reference,
            string fieldName,
            out string errorMessage)
        {
            if (reference == null)
            {
                errorMessage = $"operationalInputRuntimeProfile.{fieldName} is required.";
                return false;
            }

            if (reference.action == null)
            {
                errorMessage = $"operationalInputRuntimeProfile.{fieldName}.action is required.";
                return false;
            }

            InputActionAsset actionAsset = reference.action.actionMap?.asset;
            if (!ReferenceEquals(actionAsset, expectedAsset))
            {
                errorMessage = $"operationalInputRuntimeProfile.{fieldName} must belong to operationalInputRuntimeProfile.uiActionsAsset.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
