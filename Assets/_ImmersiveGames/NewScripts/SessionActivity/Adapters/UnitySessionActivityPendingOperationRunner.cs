using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    public sealed class UnitySessionActivityPendingOperationRunner : ISessionActivityPendingOperationRunner
    {
        private readonly ISessionActivityWindowSceneAdapter _windowSceneAdapter;
        private readonly IActivityContentSceneAdapter _activityContentSceneAdapter;
        private readonly IActivityContentSceneReleaseAdapter _activityContentSceneReleaseAdapter;

        public UnitySessionActivityPendingOperationRunner(
            ISessionActivityWindowSceneAdapter windowSceneAdapter,
            IActivityContentSceneAdapter activityContentSceneAdapter,
            IActivityContentSceneReleaseAdapter activityContentSceneReleaseAdapter)
        {
            _windowSceneAdapter = windowSceneAdapter ?? throw new ArgumentNullException(nameof(windowSceneAdapter));
            _activityContentSceneAdapter = activityContentSceneAdapter ?? throw new ArgumentNullException(nameof(activityContentSceneAdapter));
            _activityContentSceneReleaseAdapter = activityContentSceneReleaseAdapter ?? throw new ArgumentNullException(nameof(activityContentSceneReleaseAdapter));
        }

        public void RunWindowOperation(
            SessionActivityPendingOperation operation,
            SceneKeyAsset sceneKey,
            ISessionActivityPendingOperationCallback callback)
        {
            if (!operation.IsValid)
            {
                throw new InvalidOperationException("Pending operation is invalid.");
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            _ = RunWindowOperationAsync(operation, sceneKey, callback);
        }

        public void RunActivityContentOperation(
            SessionActivityPendingOperation operation,
            ActivityContentSceneLoadCommand command,
            ISessionActivityPendingOperationCallback callback)
        {
            if (!operation.IsValid)
            {
                throw new InvalidOperationException("Pending operation is invalid.");
            }

            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityContentSceneLoadCommand is invalid.");
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            _ = RunActivityContentOperationAsync(operation, command, callback);
        }

        public void RunActivityContentReleaseOperation(
            SessionActivityPendingOperation operation,
            ActivityContentSceneUnloadCommand command,
            ISessionActivityPendingOperationCallback callback)
        {
            if (!operation.IsValid)
            {
                throw new InvalidOperationException("Pending operation is invalid.");
            }

            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityContentSceneUnloadCommand is invalid.");
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            _ = RunActivityContentReleaseOperationAsync(operation, command, callback);
        }

        private async Task RunWindowOperationAsync(
            SessionActivityPendingOperation operation,
            SceneKeyAsset sceneKey,
            ISessionActivityPendingOperationCallback callback)
        {
            try
            {
                switch (operation.OperationKind)
                {
                    case SessionActivityPendingOperationKind.ActivationWindowSceneLoad:
                        await _windowSceneAdapter.LoadAdditiveAsync(sceneKey, operation.ActivityId, "activation_window", operation.Source, operation.Reason);
                        break;
                    case SessionActivityPendingOperationKind.ActivationWindowSceneUnload:
                        await _windowSceneAdapter.UnloadAsync(sceneKey, operation.ActivityId, "activation_window", operation.Source, operation.Reason);
                        break;
                    case SessionActivityPendingOperationKind.DeactivationWindowSceneLoad:
                        await _windowSceneAdapter.LoadAdditiveAsync(sceneKey, operation.ActivityId, "deactivation_window", operation.Source, operation.Reason);
                        break;
                    case SessionActivityPendingOperationKind.DeactivationWindowSceneUnload:
                        await _windowSceneAdapter.UnloadAsync(sceneKey, operation.ActivityId, "deactivation_window", operation.Source, operation.Reason);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported pending window operation kind '{operation.OperationKind}'.");
                }

                callback.CompletePendingOperation(operation, operation.Source, operation.Reason);
            }
            catch (Exception exception)
            {
                callback.FailPendingOperation(operation, operation.Source, operation.Reason, exception.Message);
            }
        }

        private async Task RunActivityContentOperationAsync(
            SessionActivityPendingOperation operation,
            ActivityContentSceneLoadCommand command,
            ISessionActivityPendingOperationCallback callback)
        {
            try
            {
                ActivityContentSceneLoadResult result = await _activityContentSceneAdapter.LoadAdditiveAsync(command);
                if (!result.IsValid)
                {
                    callback.FailPendingOperation(operation, operation.Source, operation.Reason, "activity_content_scene_load_result_invalid");
                    return;
                }

                if (result.IsLoaded)
                {
                    try
                    {
                        callback.CompletePendingOperation(operation, result.Source, result.Reason);
                    }
                    catch (Exception callbackException)
                    {
                        Debug.LogError(
                            $"[FATAL][SessionActivityPendingOperationRunner] Activity content completion callback failed operationId='{operation.OperationId}' operationKind='{operation.OperationKind}' activityId='{operation.ActivityId}' entrySequence='{operation.EntrySequence}' sceneName='{operation.SceneName}' source='{result.Source}' reason='{result.Reason}' error='{callbackException}'.");
                        callback.FailPendingOperation(operation, result.Source, result.Reason, $"activity_content_completion_callback_failed: {callbackException.Message}");
                    }

                    return;
                }

                if (result.IsRejected || result.IsFailed)
                {
                    callback.FailPendingOperation(operation, result.Source, result.Reason, result.Message);
                    return;
                }

                callback.FailPendingOperation(operation, result.Source, result.Reason, $"activity_content_scene_load_unexpected_kind:{result.Kind}");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[FATAL][SessionActivityPendingOperationRunner] Activity content operation failed operationId='{operation.OperationId}' operationKind='{operation.OperationKind}' activityId='{operation.ActivityId}' entrySequence='{operation.EntrySequence}' sceneName='{operation.SceneName}' source='{operation.Source}' reason='{operation.Reason}' error='{exception}'.");
                callback.FailPendingOperation(operation, operation.Source, operation.Reason, exception.Message);
            }
        }

        private async Task RunActivityContentReleaseOperationAsync(
            SessionActivityPendingOperation operation,
            ActivityContentSceneUnloadCommand command,
            ISessionActivityPendingOperationCallback callback)
        {
            try
            {
                ActivityContentSceneUnloadResult result = await _activityContentSceneReleaseAdapter.UnloadAdditiveAsync(command);
                if (!result.IsValid)
                {
                    callback.FailPendingOperation(operation, operation.Source, operation.Reason, "activity_content_scene_unload_result_invalid");
                    return;
                }

                if (result.IsUnloaded || result.IsSkippedNoContent)
                {
                    try
                    {
                        callback.CompleteActivityContentSceneUnloadOperation(operation, result);
                    }
                    catch (Exception callbackException)
                    {
                        Debug.LogError(
                            $"[FATAL][SessionActivityPendingOperationRunner] Activity content release completion callback failed operationId='{operation.OperationId}' operationKind='{operation.OperationKind}' activityId='{operation.ActivityId}' entrySequence='{operation.EntrySequence}' sceneName='{operation.SceneName}' source='{result.Source}' reason='{result.Reason}' error='{callbackException}'.");
                        callback.FailPendingOperation(operation, result.Source, result.Reason, $"activity_content_release_completion_callback_failed: {callbackException.Message}");
                    }

                    return;
                }

                if (result.IsRejected || result.IsFailed)
                {
                    callback.FailPendingOperation(operation, result.Source, result.Reason, result.Message);
                    return;
                }

                callback.FailPendingOperation(operation, result.Source, result.Reason, $"activity_content_scene_unload_unexpected_kind:{result.Kind}");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[FATAL][SessionActivityPendingOperationRunner] Activity content release operation failed operationId='{operation.OperationId}' operationKind='{operation.OperationKind}' activityId='{operation.ActivityId}' entrySequence='{operation.EntrySequence}' sceneName='{operation.SceneName}' source='{operation.Source}' reason='{operation.Reason}' error='{exception}'.");
                callback.FailPendingOperation(operation, operation.Source, operation.Reason, exception.Message);
            }
        }
    }
}
