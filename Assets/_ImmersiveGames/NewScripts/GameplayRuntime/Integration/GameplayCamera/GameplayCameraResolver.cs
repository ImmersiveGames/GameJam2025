/*
 * ChangeLog
 * - Ajustado GetDefaultCamera para fallback em Camera.main quando n�o houver padr�o registrado.
 * - Mantida idempot�ncia de registro/desregistro com logs coerentes e evento de c�mera padr�o.
 * - Resili�ncia extra: evita eventos duplicados e usa fallback de c�mera padr�o ao consultar player espec�fico.
 */

using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Integration.GameplayCamera
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayCameraResolver : IGameplayCameraResolver
    {
        private readonly Dictionary<int, UnityEngine.Camera> _cameraByPlayerId = new();
        private UnityEngine.Camera _defaultCamera;

        public event Action<UnityEngine.Camera> OnDefaultCameraChanged;

        public IReadOnlyDictionary<int, UnityEngine.Camera> AllCameras => _cameraByPlayerId;

        public void RegisterCamera(int playerId, UnityEngine.Camera camera)
        {
            if (camera == null)
            {
                DebugUtility.LogVerbose<GameplayCameraResolver>(
                    $"Ignoring camera registration for playerId={playerId} because camera is null.");
                return;
            }

            if (_cameraByPlayerId.TryGetValue(playerId, out var currentCamera) && currentCamera == camera)
            {
                return;
            }

            _cameraByPlayerId[playerId] = camera;

            DebugUtility.LogVerbose<GameplayCameraResolver>(
                $"Camera registered for playerId={playerId}: {camera.name}.",
                DebugUtility.Colors.Info);

            if (playerId == 0)
            {
                UpdateDefaultCamera(camera);
            }
        }

        public void UnregisterCamera(int playerId, UnityEngine.Camera camera)
        {
            if (!_cameraByPlayerId.TryGetValue(playerId, out var currentCamera) || currentCamera != camera)
            {
                return;
            }

            _cameraByPlayerId.Remove(playerId);

            DebugUtility.LogVerbose<GameplayCameraResolver>(
                $"Camera unregistered for playerId={playerId}: {camera.name}.",
                DebugUtility.Colors.Info);

            if (playerId == 0)
            {
                UpdateDefaultCamera(null);
            }
        }

        public UnityEngine.Camera GetCamera(int playerId)
        {
            if (_cameraByPlayerId.TryGetValue(playerId, out var camera) && camera != null)
            {
                return camera;
            }

            return GetDefaultCamera();
        }

        public UnityEngine.Camera GetDefaultCamera()
        {
            return _defaultCamera ?? UnityEngine.Camera.main;
        }

        private void UpdateDefaultCamera(UnityEngine.Camera nextDefault)
        {
            if (_defaultCamera == nextDefault)
            {
                return;
            }

            _defaultCamera = nextDefault;
            OnDefaultCameraChanged?.Invoke(_defaultCamera);

            string description = nextDefault == null ? "null" : nextDefault.name;

            DebugUtility.LogVerbose<GameplayCameraResolver>(
                $"Default camera updated to {description} (playerId=0).",
                DebugUtility.Colors.Info);
        }
    }
}


