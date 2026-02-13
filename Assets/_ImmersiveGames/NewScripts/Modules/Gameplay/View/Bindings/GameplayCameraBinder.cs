using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Gameplay.View.Bindings
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class GameplayCameraBinder : MonoBehaviour
    {
        [SerializeField] private int playerId;

        private Camera _camera;
        private ICameraResolver _resolver;

        private bool _registered;

        private void Awake()
        {
            if (RuntimeFailFastUtility.IsFatalLatched)
            {
                DebugUtility.LogVerbose<GameplayCameraBinder>("[OBS][Boot] Aborting camera binder Awake due to fatal latch.", DebugUtility.Colors.Info);
                return;
            }

            _camera = GetComponent<Camera>();
            EnsureRequiredDependenciesOrThrow();
        }

        private void OnEnable()
        {
            if (RuntimeFailFastUtility.IsFatalLatched)
            {
                DebugUtility.LogVerbose<GameplayCameraBinder>("[OBS][Boot] Aborting camera binder OnEnable due to fatal latch.", DebugUtility.Colors.Info);
                return;
            }

            RegisterCameraIfNeeded();
        }

        private void OnDisable()
        {
            TryUnregisterCamera();
        }

        private void OnDestroy()
        {
            TryUnregisterCamera();
        }

        private void EnsureRequiredDependenciesOrThrow()
        {
            if (_camera == null)
            {
                throw RuntimeFailFastUtility.FailFastAndCreateException(
                    "DI",
                    "GameplayCameraBinder requires a Camera component on the same GameObject.",
                    this);
            }

            if (!DependencyManager.Provider.TryGetGlobal(out _resolver) || _resolver == null)
            {
                throw RuntimeFailFastUtility.FailFastAndCreateException(
                    "DI",
                    "Missing required ICameraResolver in global DI during GameplayCameraBinder bootstrap.",
                    this);
            }

            DebugUtility.LogVerbose<GameplayCameraBinder>(
                "ICameraResolver resolved during GameplayCameraBinder bootstrap.",
                DebugUtility.Colors.Info);
        }

        private void RegisterCameraIfNeeded()
        {
            if (_registered)
            {
                return;
            }

            _resolver.RegisterCamera(playerId, _camera);
            _registered = true;

            DebugUtility.Log<GameplayCameraBinder>(
                $"Gameplay camera registered (playerId={playerId}): {_camera.name}.",
                DebugUtility.Colors.Info);
        }

        private void TryUnregisterCamera()
        {
            if (!_registered)
            {
                return;
            }

            _resolver.UnregisterCamera(playerId, _camera);
            _registered = false;
        }
    }
}
