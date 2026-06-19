using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Attributes.UI;
using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.InputModes.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Adapters;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/SessionActivity/Session Activity Composition Installer")]
    public sealed class SessionActivityCompositionInstaller : MonoBehaviour
    {
        private SessionActivityHost _host;
        private SessionActivityCatalog _catalog;
        private SessionActivityPipeline _pipeline;
        private bool _globalsRegistered;

        public void Compose(
            SessionActivityHost host,
            SessionActivityCatalog catalog,
            string sessionStateId)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (string.IsNullOrWhiteSpace(sessionStateId))
            {
                throw new ArgumentException("sessionStateId is required.", nameof(sessionStateId));
            }

            if (_pipeline != null)
            {
                throw new InvalidOperationException("SessionActivityCompositionInstaller was already composed.");
            }

            EnsureDependencyManagerOrFail();
            var activityCameraPreparationExecutor = ResolveActivityCameraPreparationExecutorOrFail();
            var canonicalPlayerInputActionsAsset = ResolveCanonicalPlayerInputActionsAssetOrFail();
            var poolService = ResolvePoolServiceOrFail();
            var globalAudioService = ResolveGlobalAudioServiceOrFail();

            UnitySessionActivityWindowSceneAdapter windowSceneAdapter = new();
            UnityActivityContentSceneAdapter activityContentSceneAdapter = new();
            UnityActivityContentSceneReleaseAdapter activityContentSceneReleaseAdapter = new();
            UnitySessionActivityPendingOperationRunner pendingOperationRunner = new(
                windowSceneAdapter,
                activityContentSceneAdapter,
                activityContentSceneReleaseAdapter);
            ActorAttributeEventStream actorAttributeEventStream = new();
            LoadedSceneActorAttributeUiBindingRequestProvider actorAttributeUiBindingRequestProvider = new();

            _host = host;
            _catalog = catalog;
            _pipeline = new SessionActivityPipeline(
                _catalog,
                sessionStateId,
                new PauseOverlayAdapter(),
                new InputModeAdapter(),
                new SessionActivityTransitionAdapter(),
                new SessionActivityTransitionLoadingAdapter(),
                windowSceneAdapter,
                pendingOperationRunner,
                actorAttributeEventStream);

            var activityEntryPipeline = new ActivityEntryPipeline(
                _pipeline.EntryRuntimeBridge,
                _pipeline.EntryPlayerActorMaterializationAdapter,
                _pipeline.EntryPlayerActorParticipationAdapter,
                _pipeline.EntryActorResetAdapter,
                _pipeline.EntryActorPresentationRuntimeBridge,
                _pipeline.EntryActorParticipationRuntimeBridge,
                _pipeline.EntryPermissionTargetRuntimeBridge,
                _pipeline.EntryMovementBindingRuntimeBridge,
                _pipeline.EntryCameraBindingRuntimeBridge,
                _pipeline.EntryActorSceneRegistry,
                _pipeline.EntryActorPlayerRegistry,
                _pipeline.EntrySessionActorRuntimeStore,
                _pipeline.ActivityContentRuntimeState,
                _pipeline.EntryMovementBindingAdapter,
                activityCameraPreparationExecutor,
                pendingOperationRunner,
                _pipeline,
                new ActivityPauseContentAdapter(),
                canonicalPlayerInputActionsAsset,
                _pipeline.ActivityActorExitRuntimeState,
                actorAttributeEventStream,
                actorAttributeUiBindingRequestProvider,
                poolService,
                globalAudioService);
            // SA-19B1 — BindEntryPipeline seam removed (real removal step).
            // Old method deleted. Using transitional AttachEntryPipeline for now.
            // See SA-19B0-Bridge-Surface-Freeze.md and SA-19 plan.
            // Next: restructure so that EntryPipeline is created with narrow contracts and attached at construction time (SA-19B2).
            _pipeline.AttachEntryPipeline(activityEntryPipeline);
            EnsurePauseToggleInputAdapterOrFail(canonicalPlayerInputActionsAsset);

            _globalsRegistered = true;
            try
            {
                RegisterGlobalsOrFail();
                _host.BindComposition(_catalog, _pipeline);
            }
            catch
            {
                UnregisterGlobalsIfNeeded();
                throw;
            }

            DebugUtility.Log(typeof(SessionActivityCompositionInstaller),
                $"composition completed sessionStateId='{sessionStateId}' catalog='{_catalog.Summary}'.",
                DebugUtility.Colors.Info);
        }

        private void OnDestroy()
        {
            if (!DependencyManager.HasInstance)
            {
                _globalsRegistered = false;
                return;
            }

            UnregisterGlobalsIfNeeded();
        }

        private static void EnsureDependencyManagerOrFail()
        {
            if (DependencyManager.HasInstance)
            {
                return;
            }

            throw new InvalidOperationException("[FATAL][Config][SessionActivityPipeline] DependencyManager instance is required before composing SessionActivity.");
        }

        private IActivityCameraPreparationExecutor ResolveActivityCameraPreparationExecutorOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActivityCameraPreparationExecutor>(out var executor) &&
                executor != null)
            {
                return executor;
            }

            throw new InvalidOperationException("[FATAL][Config][SessionActivityPipeline] Required dependency missing type='IActivityCameraPreparationExecutor'.");
        }

        private static IPoolService ResolvePoolServiceOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPoolService>(out var poolService) &&
                poolService != null)
            {
                return poolService;
            }

            throw new InvalidOperationException("[FATAL][Config][SessionActivityPipeline] Required dependency missing type='IPoolService'.");
        }

        private static IGlobalAudioService ResolveGlobalAudioServiceOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGlobalAudioService>(out var globalAudioService) &&
                globalAudioService != null)
            {
                return globalAudioService;
            }

            throw new InvalidOperationException("[FATAL][Config][SessionActivityPipeline] Required dependency missing type='IGlobalAudioService'.");
        }

        private static InputActionAsset ResolveCanonicalPlayerInputActionsAssetOrFail()
        {
            if (!RuntimeConfigRegistry.TryGetSnapshot(out var snapshot) || snapshot == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionActivityPipeline] RuntimeConfigRegistry snapshot obrigatorio ausente para PlayerInput canonical actions.");
            }

            var inputModesRuntime = snapshot.InputModesRuntime
                ?? throw new InvalidOperationException("[FATAL][Config][SessionActivityPipeline] RuntimeConfigRegistry invariant breach: snapshot.InputModesRuntime obrigatorio ausente.");

            if (inputModesRuntime.OperationalInputRuntimeProfile == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionActivityPipeline] RuntimeConfigRegistry invariant breach: operationalInputRuntimeProfile obrigatorio ausente.");
            }

            if (string.IsNullOrWhiteSpace(inputModesRuntime.OperationalInputRuntimeProfileId))
            {
                throw new InvalidOperationException("[FATAL][Config][SessionActivityPipeline] RuntimeConfigRegistry invariant breach: operationalInputRuntimeProfile.profileId obrigatorio ausente.");
            }

            var canonicalActionsAsset = inputModesRuntime.UiActionsAsset;
            if (canonicalActionsAsset == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionActivityPipeline] RuntimeConfigRegistry invariant breach: uiActionsAsset canonico obrigatorio ausente.");
            }

            if (canonicalActionsAsset.FindActionMap(InputModesDefaults.PlayerActionMapName, false) == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionActivityPipeline] RuntimeConfigRegistry invariant breach: uiActionsAsset canonico sem ActionMap '{InputModesDefaults.PlayerActionMapName}'.");
            }

            return canonicalActionsAsset;
        }

        private void EnsurePauseToggleInputAdapterOrFail(InputActionAsset canonicalPlayerInputActionsAsset)
        {
            if (canonicalPlayerInputActionsAsset == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionActivityPipeline] canonicalPlayerInputActionsAsset obrigatorio ausente para PauseToggle input adapter.");
            }

            var adapter = _host.GetComponent<SessionActivityPauseToggleInputAdapter>();
            if (adapter == null)
            {
                adapter = _host.gameObject.AddComponent<SessionActivityPauseToggleInputAdapter>();
            }

            adapter.Initialize(_host, canonicalPlayerInputActionsAsset);
        }

        private void RegisterGlobalsOrFail()
        {
            RegisterGlobal(_catalog);
            RegisterGlobal(_pipeline);
            RegisterGlobal<ISessionActivityEntryHandoffReceiver>(_pipeline);
            RegisterGlobal<ISessionActivitySnapshotPayloadProvider>(_pipeline);
            RegisterGlobal<ISessionActivityRouteExitTeardownBoundary>(_host);
            RegisterGlobal<ISessionActivityVisualReadinessBoundary>(_host);
        }

        private static void RegisterGlobal<T>(T instance) where T : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                if (!ReferenceEquals(existing, instance))
                {
                    throw new InvalidOperationException($"Global dependency '{typeof(T).Name}' is already registered with a different instance.");
                }

                return;
            }

            DependencyManager.Provider.RegisterGlobal(instance);
        }

        private void UnregisterGlobalsIfNeeded()
        {
            if (!_globalsRegistered)
            {
                return;
            }

            if (_catalog != null)
            {
                UnregisterGlobal(_catalog);
            }

            if (_pipeline != null)
            {
                UnregisterGlobal(_pipeline);
                UnregisterGlobal<ISessionActivityEntryHandoffReceiver>(_pipeline);
                UnregisterGlobal<ISessionActivitySnapshotPayloadProvider>(_pipeline);
            }

            if (_host != null)
            {
                UnregisterGlobal<ISessionActivityRouteExitTeardownBoundary>(_host);
                UnregisterGlobal<ISessionActivityVisualReadinessBoundary>(_host);
            }

            _globalsRegistered = false;
        }

        private static void UnregisterGlobal<T>(T instance) where T : class
        {
            if (instance == null)
            {
                return;
            }

            DependencyManager.Provider.UnregisterGlobal(instance);
        }
    }
}
