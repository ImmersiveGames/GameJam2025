using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
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
        ISaveRuntimeConfigGroupReadOnly SaveRuntime { get; }
        IInputModesRuntimeConfigGroupReadOnly InputModesRuntime { get; }
    }

    public interface IRuntimePolicyConfigGroupReadOnly
    {
        RuntimePersistentScenesPolicyAsset RuntimePersistentScenesPolicy { get; }
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

    public interface IAudioRuntimeConfigGroupReadOnly
    {
        AudioDefaultsAsset AudioDefaults { get; }
    }

    public interface ISaveRuntimeConfigGroupReadOnly
    {
        SaveConfigAsset SaveConfig { get; }
    }

    public interface IInputModesRuntimeConfigGroupReadOnly
    {
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
}
