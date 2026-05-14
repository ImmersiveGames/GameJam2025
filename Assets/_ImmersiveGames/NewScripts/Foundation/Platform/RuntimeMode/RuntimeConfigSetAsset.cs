using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.SaveRuntime.Authoring;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    [CreateAssetMenu(
        fileName = "RuntimeConfigSet",
        menuName = "ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/RuntimeConfigSetAsset",
        order = 23)]
    public sealed class RuntimeConfigSetAsset : ScriptableObject
    {
        [Header("Config Groups")]
        [SerializeField] private RuntimePolicyConfigGroup runtimePolicy = new();
        [SerializeField] private SessionOperationalRuntimeConfigGroup sessionOperationalRuntime = new();
        [SerializeField] private AudioRuntimeConfigGroup audioRuntime = new();
        [SerializeField] private SaveRuntimeConfigGroup saveRuntime = new();
        [SerializeField] private InputModesRuntimeConfigGroup inputModesRuntime = new();

        public RuntimePolicyConfigGroup RuntimePolicy => runtimePolicy;
        public SessionOperationalRuntimeConfigGroup SessionOperationalRuntime => sessionOperationalRuntime;
        public AudioRuntimeConfigGroup AudioRuntime => audioRuntime;
        public SaveRuntimeConfigGroup SaveRuntime => saveRuntime;
        public InputModesRuntimeConfigGroup InputModesRuntime => inputModesRuntime;

        public bool TryValidate(out string errorMessage)
        {
            if (runtimePolicy == null)
            {
                errorMessage = "runtimePolicy is required.";
                return false;
            }
            if (!runtimePolicy.TryValidate(out errorMessage))
            {
                return false;
            }

            if (sessionOperationalRuntime == null)
            {
                errorMessage = "sessionOperationalRuntime is required.";
                return false;
            }
            if (!sessionOperationalRuntime.TryValidate(out errorMessage))
            {
                return false;
            }

            if (audioRuntime == null)
            {
                errorMessage = "audioRuntime is required.";
                return false;
            }

            if (saveRuntime == null)
            {
                errorMessage = "saveRuntime is required.";
                return false;
            }
            if (!saveRuntime.TryValidate(out errorMessage))
            {
                return false;
            }

            if (inputModesRuntime == null)
            {
                errorMessage = "inputModesRuntime is required.";
                return false;
            }
            if (!inputModesRuntime.TryValidate(out errorMessage))
            {
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }

    [Serializable]
    public sealed class RuntimePolicyConfigGroup
    {
        [SerializeField] private RuntimePersistentScenesPolicyAsset runtimePersistentScenesPolicy;
        [SerializeField] private RuntimeModeConfig.DegradedReporterSettings reporter = new();
        [SerializeField] private RuntimeModeConfig.StrictnessSettings strictness = new();

        public RuntimePersistentScenesPolicyAsset RuntimePersistentScenesPolicy => runtimePersistentScenesPolicy;
        public RuntimeModeConfig.DegradedReporterSettings Reporter => reporter;
        public RuntimeModeConfig.StrictnessSettings Strictness => strictness;

        public bool TryValidate(out string errorMessage)
        {
            if (runtimePersistentScenesPolicy == null)
            {
                errorMessage = "runtimePersistentScenesPolicy is required for RuntimePolicyConfigGroup.";
                return false;
            }

            if (reporter == null)
            {
                errorMessage = "reporter is required for RuntimePolicyConfigGroup.";
                return false;
            }

            if (strictness == null)
            {
                errorMessage = "strictness is required for RuntimePolicyConfigGroup.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }

    [Serializable]
    public sealed class SessionOperationalRuntimeConfigGroup
    {
        [SerializeField] private OperationalRouteAsset startupRouteDefinition;
        [SerializeField] private SessionOperationalRouteLoadingMode defaultLoadingMode = SessionOperationalRouteLoadingMode.None;
        [SerializeField] private RuntimeLoadingProfileAsset defaultLoadingProfile;

        public OperationalRouteAsset StartupRouteDefinition => startupRouteDefinition;
        public SessionOperationalRouteLoadingMode DefaultLoadingMode => defaultLoadingMode;
        public RuntimeLoadingProfileAsset DefaultLoadingProfile => defaultLoadingProfile;

        public bool TryValidate(out string errorMessage)
        {
            if (startupRouteDefinition == null)
            {
                errorMessage = "sessionOperationalRuntime.startupRouteDefinition is required.";
                return false;
            }

            if (!startupRouteDefinition.IsValid)
            {
                errorMessage = "sessionOperationalRuntime.startupRouteDefinition is invalid.";
                return false;
            }

            if (defaultLoadingMode == SessionOperationalRouteLoadingMode.RuntimeDefault)
            {
                errorMessage = "sessionOperationalRuntime.defaultLoadingMode cannot be RuntimeDefault.";
                return false;
            }

            if (defaultLoadingMode == SessionOperationalRouteLoadingMode.Profile)
            {
                if (defaultLoadingProfile == null)
                {
                    errorMessage = "sessionOperationalRuntime.defaultLoadingProfile is required when defaultLoadingMode=Profile.";
                    return false;
                }

                if (!defaultLoadingProfile.TryValidate(out string profileValidationError))
                {
                    errorMessage = $"sessionOperationalRuntime.defaultLoadingProfile is invalid. detail='{profileValidationError}'.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }
    }

    [Serializable]
    public sealed class AudioRuntimeConfigGroup
    {
        [SerializeField] private AudioDefaultsAsset audioDefaults;

        public AudioDefaultsAsset AudioDefaults => audioDefaults;

        public bool TryValidate(out string errorMessage)
        {
            if (audioDefaults == null)
            {
                errorMessage = "audioDefaults is required for AudioRuntimeConfigGroup.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }

    [Serializable]
    public sealed class SaveRuntimeConfigGroup
    {
        [SerializeField] private SaveConfigAsset saveConfig;

        public SaveConfigAsset SaveConfig => saveConfig;

        public bool TryValidate(out string errorMessage)
        {
            if (saveConfig == null)
            {
                errorMessage = "saveRuntime.saveConfig is required.";
                return false;
            }

            try
            {
                saveConfig.ValidateOrThrow();
            }
            catch (Exception ex)
            {
                errorMessage = $"saveRuntime.saveConfig is invalid. detail='{ex.Message}'.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }

    [Serializable]
    public sealed class InputModesRuntimeConfigGroup
    {
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
            if (maxPlayerSlots < 1)
            {
                errorMessage = "inputModesRuntime.maxPlayerSlots must be >= 1.";
                return false;
            }

            if (uiActionsAsset == null)
            {
                errorMessage = "inputModesRuntime.uiActionsAsset is required.";
                return false;
            }

            if (!TryValidateActionReference(uiActionsAsset, uiPoint, "uiPoint", out errorMessage))
            {
                return false;
            }

            if (!TryValidateActionReference(uiActionsAsset, uiLeftClick, "uiLeftClick", out errorMessage))
            {
                return false;
            }

            if (!TryValidateActionReference(uiActionsAsset, uiRightClick, "uiRightClick", out errorMessage))
            {
                return false;
            }

            if (!TryValidateActionReference(uiActionsAsset, uiMiddleClick, "uiMiddleClick", out errorMessage))
            {
                return false;
            }

            if (!TryValidateActionReference(uiActionsAsset, uiScrollWheel, "uiScrollWheel", out errorMessage))
            {
                return false;
            }

            if (!TryValidateActionReference(uiActionsAsset, uiMove, "uiMove", out errorMessage))
            {
                return false;
            }

            if (!TryValidateActionReference(uiActionsAsset, uiSubmit, "uiSubmit", out errorMessage))
            {
                return false;
            }

            if (!TryValidateActionReference(uiActionsAsset, uiCancel, "uiCancel", out errorMessage))
            {
                return false;
            }

            if (!TryValidateActionReference(uiActionsAsset, uiTrackedDevicePosition, "uiTrackedDevicePosition", out errorMessage))
            {
                return false;
            }

            if (!TryValidateActionReference(uiActionsAsset, uiTrackedDeviceOrientation, "uiTrackedDeviceOrientation", out errorMessage))
            {
                return false;
            }

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
                errorMessage = $"inputModesRuntime.{fieldName} is required.";
                return false;
            }

            if (reference.action == null)
            {
                errorMessage = $"inputModesRuntime.{fieldName} has null action.";
                return false;
            }

            InputActionAsset actionAsset = reference.action.actionMap?.asset;
            if (!ReferenceEquals(actionAsset, expectedAsset))
            {
                errorMessage = $"inputModesRuntime.{fieldName} must belong to inputModesRuntime.uiActionsAsset.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
