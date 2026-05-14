using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.SaveRuntime.Authoring;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    public sealed class RuntimeConfigSnapshot : IRuntimeConfigSnapshotReadOnly
    {
        public RuntimeConfigSnapshot(RuntimeConfigSetAsset sourceAsset)
        {
            SourceAsset = sourceAsset ?? throw new ArgumentNullException(nameof(sourceAsset));

            RuntimePolicy = new RuntimePolicyConfigGroupSnapshot(sourceAsset.RuntimePolicy);
            SessionOperationalRuntime = new SessionOperationalRuntimeConfigGroupSnapshot(sourceAsset.SessionOperationalRuntime);
            AudioRuntime = new AudioRuntimeConfigGroupSnapshot(sourceAsset.AudioRuntime);
            SaveRuntime = new SaveRuntimeConfigGroupSnapshot(sourceAsset.SaveRuntime);
            InputModesRuntime = new InputModesRuntimeConfigGroupSnapshot(sourceAsset.InputModesRuntime);
        }

        public RuntimeConfigSetAsset SourceAsset { get; }
        public IRuntimePolicyConfigGroupReadOnly RuntimePolicy { get; }
        public ISessionOperationalRuntimeConfigGroupReadOnly SessionOperationalRuntime { get; }
        public IAudioRuntimeConfigGroupReadOnly AudioRuntime { get; }
        public ISaveRuntimeConfigGroupReadOnly SaveRuntime { get; }
        public IInputModesRuntimeConfigGroupReadOnly InputModesRuntime { get; }

        private sealed class RuntimePolicyConfigGroupSnapshot : IRuntimePolicyConfigGroupReadOnly
        {
            public RuntimePolicyConfigGroupSnapshot(RuntimePolicyConfigGroup source)
            {
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source));
                }

                RuntimePersistentScenesPolicy = source.RuntimePersistentScenesPolicy;

                RuntimeModeConfig.DegradedReporterSettings reporter = source.Reporter ?? new RuntimeModeConfig.DegradedReporterSettings();
                ReporterDedupStrategy = reporter.dedupStrategy;
                ReporterCooldownSeconds = reporter.cooldownSeconds;
                ReporterEmitSummaryEverySeconds = reporter.emitSummaryEverySeconds;
                ReporterMaxUniqueKeys = reporter.maxUniqueKeys;
                ReporterLogFirstOccurrence = reporter.logFirstOccurrence;
                ReporterIncludeCountInLog = reporter.includeCountInLog;

                RuntimeModeConfig.StrictnessSettings strictness = source.Strictness ?? new RuntimeModeConfig.StrictnessSettings();
                StrictnessDegradedAsError = strictness.degradedAsError;
                StrictnessDegradedAsException = strictness.degradedAsException;
            }

            public RuntimePersistentScenesPolicyAsset RuntimePersistentScenesPolicy { get; }
            public DegradedDedupStrategy ReporterDedupStrategy { get; }
            public float ReporterCooldownSeconds { get; }
            public float ReporterEmitSummaryEverySeconds { get; }
            public int ReporterMaxUniqueKeys { get; }
            public bool ReporterLogFirstOccurrence { get; }
            public bool ReporterIncludeCountInLog { get; }
            public bool StrictnessDegradedAsError { get; }
            public bool StrictnessDegradedAsException { get; }
        }

        private sealed class SessionOperationalRuntimeConfigGroupSnapshot : ISessionOperationalRuntimeConfigGroupReadOnly
        {
            public SessionOperationalRuntimeConfigGroupSnapshot(SessionOperationalRuntimeConfigGroup source)
            {
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source));
                }

                StartupRouteDefinition = source.StartupRouteDefinition;
                DefaultLoadingMode = source.DefaultLoadingMode;
                DefaultLoadingProfile = source.DefaultLoadingProfile;
            }

            public OperationalRouteAsset StartupRouteDefinition { get; }
            public SessionOperationalRouteLoadingMode DefaultLoadingMode { get; }
            public RuntimeLoadingProfileAsset DefaultLoadingProfile { get; }
        }

        private sealed class AudioRuntimeConfigGroupSnapshot : IAudioRuntimeConfigGroupReadOnly
        {
            public AudioRuntimeConfigGroupSnapshot(AudioRuntimeConfigGroup source)
            {
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source));
                }

                AudioDefaults = source.AudioDefaults;
            }

            public AudioDefaultsAsset AudioDefaults { get; }
        }

        private sealed class SaveRuntimeConfigGroupSnapshot : ISaveRuntimeConfigGroupReadOnly
        {
            public SaveRuntimeConfigGroupSnapshot(SaveRuntimeConfigGroup source)
            {
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source));
                }

                SaveConfig = source.SaveConfig;
            }

            public SaveConfigAsset SaveConfig { get; }
        }

        private sealed class InputModesRuntimeConfigGroupSnapshot : IInputModesRuntimeConfigGroupReadOnly
        {
            public InputModesRuntimeConfigGroupSnapshot(InputModesRuntimeConfigGroup source)
            {
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source));
                }

                MaxPlayerSlots = source.MaxPlayerSlots;
                UiActionsAsset = source.UiActionsAsset;
                UiPoint = source.UiPoint;
                UiLeftClick = source.UiLeftClick;
                UiRightClick = source.UiRightClick;
                UiMiddleClick = source.UiMiddleClick;
                UiScrollWheel = source.UiScrollWheel;
                UiMove = source.UiMove;
                UiSubmit = source.UiSubmit;
                UiCancel = source.UiCancel;
                UiTrackedDevicePosition = source.UiTrackedDevicePosition;
                UiTrackedDeviceOrientation = source.UiTrackedDeviceOrientation;
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
    }
}
