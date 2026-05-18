using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    public sealed class UnitySessionActivityPendingOperationRunner : ISessionActivityPendingOperationRunner
    {
        private readonly ISessionActivityWindowSceneAdapter _windowSceneAdapter;

        public UnitySessionActivityPendingOperationRunner(ISessionActivityWindowSceneAdapter windowSceneAdapter)
        {
            _windowSceneAdapter = windowSceneAdapter ?? throw new ArgumentNullException(nameof(windowSceneAdapter));
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
    }
}
