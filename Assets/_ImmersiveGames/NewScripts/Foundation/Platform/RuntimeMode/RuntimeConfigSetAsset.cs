using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging.Config;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Config;
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
        [SerializeField] private PreferencesRuntimeConfigGroup preferencesRuntime = new();
        [SerializeField] private SaveRuntimeConfigGroup saveRuntime = new();
        [SerializeField] private InputModesRuntimeConfigGroup inputModesRuntime = new();
        [SerializeField] private CameraRuntimeConfigGroup cameraRuntime = new();

        public RuntimePolicyConfigGroup RuntimePolicy => runtimePolicy;
        public SessionOperationalRuntimeConfigGroup SessionOperationalRuntime => sessionOperationalRuntime;
        public AudioRuntimeConfigGroup AudioRuntime => audioRuntime;
        public PreferencesRuntimeConfigGroup PreferencesRuntime => preferencesRuntime;
        public SaveRuntimeConfigGroup SaveRuntime => saveRuntime;
        public InputModesRuntimeConfigGroup InputModesRuntime => inputModesRuntime;
        public CameraRuntimeConfigGroup CameraRuntime => cameraRuntime;

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

            if (preferencesRuntime == null)
            {
                errorMessage = "preferencesRuntime is required.";
                return false;
            }
            if (!preferencesRuntime.TryValidate(out errorMessage))
            {
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

            if (cameraRuntime == null)
            {
                errorMessage = "cameraRuntime is required.";
                return false;
            }
            if (!cameraRuntime.TryValidate(out errorMessage))
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
        [SerializeField] private LoggingConfigAsset loggingConfig;
        [SerializeField] private RuntimeModeConfig.DegradedReporterSettings reporter = new();
        [SerializeField] private RuntimeModeConfig.StrictnessSettings strictness = new();

        public RuntimePersistentScenesPolicyAsset RuntimePersistentScenesPolicy => runtimePersistentScenesPolicy;
        public LoggingConfigAsset LoggingConfig => loggingConfig;
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

            if (loggingConfig == null)
            {
                errorMessage = "loggingConfig is required for RuntimePolicyConfigGroup.";
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
        public bool TryValidate(out string errorMessage)
        {
            errorMessage = string.Empty;
            return true;
        }
    }

    [Serializable]
    public sealed class PreferencesRuntimeConfigGroup
    {
        [SerializeField] private AudioDefaultsAsset audioDefaults;
        [SerializeField] private VideoDefaultsAsset videoDefaults;

        public AudioDefaultsAsset AudioDefaults => audioDefaults;
        public VideoDefaultsAsset VideoDefaults => videoDefaults;

        public bool TryValidate(out string errorMessage)
        {
            if (audioDefaults == null)
            {
                errorMessage = "preferencesRuntime.audioDefaults is required.";
                return false;
            }

            if (videoDefaults == null)
            {
                errorMessage = "preferencesRuntime.videoDefaults is required.";
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
        [SerializeField] private OperationalInputRuntimeProfileAsset operationalInputRuntimeProfile;

        public OperationalInputRuntimeProfileAsset OperationalInputRuntimeProfile => operationalInputRuntimeProfile;
        public int MaxPlayerSlots => operationalInputRuntimeProfile != null ? operationalInputRuntimeProfile.MaxPlayerSlots : 0;
        public InputActionAsset UiActionsAsset => operationalInputRuntimeProfile != null ? operationalInputRuntimeProfile.UiActionsAsset : null;
        public InputActionReference UiPoint => operationalInputRuntimeProfile != null ? operationalInputRuntimeProfile.UiPoint : null;
        public InputActionReference UiLeftClick => operationalInputRuntimeProfile != null ? operationalInputRuntimeProfile.UiLeftClick : null;
        public InputActionReference UiRightClick => operationalInputRuntimeProfile != null ? operationalInputRuntimeProfile.UiRightClick : null;
        public InputActionReference UiMiddleClick => operationalInputRuntimeProfile != null ? operationalInputRuntimeProfile.UiMiddleClick : null;
        public InputActionReference UiScrollWheel => operationalInputRuntimeProfile != null ? operationalInputRuntimeProfile.UiScrollWheel : null;
        public InputActionReference UiMove => operationalInputRuntimeProfile != null ? operationalInputRuntimeProfile.UiMove : null;
        public InputActionReference UiSubmit => operationalInputRuntimeProfile != null ? operationalInputRuntimeProfile.UiSubmit : null;
        public InputActionReference UiCancel => operationalInputRuntimeProfile != null ? operationalInputRuntimeProfile.UiCancel : null;
        public InputActionReference UiTrackedDevicePosition => operationalInputRuntimeProfile != null ? operationalInputRuntimeProfile.UiTrackedDevicePosition : null;
        public InputActionReference UiTrackedDeviceOrientation => operationalInputRuntimeProfile != null ? operationalInputRuntimeProfile.UiTrackedDeviceOrientation : null;

        public bool TryValidate(out string errorMessage)
        {
            if (operationalInputRuntimeProfile == null)
            {
                errorMessage = "inputModesRuntime.operationalInputRuntimeProfile is required.";
                return false;
            }

            return operationalInputRuntimeProfile.TryValidate(out errorMessage);
        }
    }

    [Serializable]
    public sealed class CameraRuntimeConfigGroup
    {
        [SerializeField] private GameObject operationalCameraPrefab;

        public GameObject OperationalCameraPrefab => operationalCameraPrefab;

        public bool TryValidate(out string errorMessage)
        {
            if (operationalCameraPrefab == null)
            {
                errorMessage = "cameraRuntime.operationalCameraPrefab is required.";
                return false;
            }

            Camera[] cameras = operationalCameraPrefab.GetComponentsInChildren<Camera>(true);
            int cameraCount = cameras?.Length ?? 0;
            if (cameraCount != 1)
            {
                errorMessage = $"cameraRuntime.operationalCameraPrefab must contain exactly one Camera. observed='{cameraCount}'.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
