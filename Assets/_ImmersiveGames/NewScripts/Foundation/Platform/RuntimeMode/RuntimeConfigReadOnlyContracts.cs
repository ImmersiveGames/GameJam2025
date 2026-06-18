using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging.Config;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Config;
using _ImmersiveGames.NewScripts.SaveRuntime.Authoring;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    public interface IRuntimeConfigSnapshotReadOnly
    {
        RuntimeConfigSetAsset SourceAsset { get; }
        IRuntimePolicyConfigGroupReadOnly RuntimePolicy { get; }
        ISessionOperationalRuntimeConfigGroupReadOnly SessionOperationalRuntime { get; }
        IAudioRuntimeConfigGroupReadOnly AudioRuntime { get; }
        IPreferencesRuntimeConfigGroupReadOnly PreferencesRuntime { get; }
        ISaveRuntimeConfigGroupReadOnly SaveRuntime { get; }
        IInputModesRuntimeConfigGroupReadOnly InputModesRuntime { get; }
        ICameraRuntimeConfigGroupReadOnly CameraRuntime { get; }
    }

    public interface IRuntimePolicyConfigGroupReadOnly
    {
        RuntimePersistentScenesPolicyAsset RuntimePersistentScenesPolicy { get; }
        LoggingConfigAsset LoggingConfig { get; }
        DegradedDedupStrategy ReporterDedupStrategy { get; }
        float ReporterCooldownSeconds { get; }
        float ReporterEmitSummaryEverySeconds { get; }
        int ReporterMaxUniqueKeys { get; }
        bool ReporterLogFirstOccurrence { get; }
        bool ReporterIncludeCountInLog { get; }
        bool StrictnessDegradedAsError { get; }
        bool StrictnessDegradedAsException { get; }
    }

    public interface ISessionOperationalRuntimeConfigGroupReadOnly
    {
        OperationalRouteAsset StartupRouteDefinition { get; }
        SessionOperationalRouteLoadingMode DefaultLoadingMode { get; }
        RuntimeLoadingProfileAsset DefaultLoadingProfile { get; }
    }

    public interface IAudioRuntimeConfigGroupReadOnly { }

    public interface IPreferencesRuntimeConfigGroupReadOnly
    {
        AudioDefaultsAsset AudioDefaults { get; }
        VideoDefaultsAsset VideoDefaults { get; }
    }

    public interface ISaveRuntimeConfigGroupReadOnly
    {
        SaveConfigAsset SaveConfig { get; }
    }

    public interface IInputModesRuntimeConfigGroupReadOnly
    {
        OperationalInputRuntimeProfileAsset OperationalInputRuntimeProfile { get; }
        string OperationalInputRuntimeProfileId { get; }
        int MaxPlayerSlots { get; }
        InputActionAsset UiActionsAsset { get; }
        InputActionReference UiPoint { get; }
        InputActionReference UiLeftClick { get; }
        InputActionReference UiRightClick { get; }
        InputActionReference UiMiddleClick { get; }
        InputActionReference UiScrollWheel { get; }
        InputActionReference UiMove { get; }
        InputActionReference UiSubmit { get; }
        InputActionReference UiCancel { get; }
        InputActionReference UiTrackedDevicePosition { get; }
        InputActionReference UiTrackedDeviceOrientation { get; }
    }

    public interface ICameraRuntimeConfigGroupReadOnly
    {
        UnityEngine.GameObject OperationalCameraPrefab { get; }
    }
}
