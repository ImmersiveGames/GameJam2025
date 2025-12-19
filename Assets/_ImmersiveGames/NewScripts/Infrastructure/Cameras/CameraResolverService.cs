/*
 * ChangeLog
 * - Ajustado GetDefaultCamera para fallback em Camera.main quando não houver padrão registrado.
 * - Mantida idempotência de registro/desregistro com logs coerentes e evento de câmera padrão.
 * - Resiliência extra: evita eventos duplicados e usa fallback de câmera padrão ao consultar player específico.
 */
using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Cameras
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class CameraResolverService : ICameraResolver
    {
        private readonly Dictionary<int, Camera> _cameraByPlayerId = new();
        private Camera _defaultCamera;

        public event Action<Camera> OnDefaultCameraChanged;

        public IReadOnlyDictionary<int, Camera> AllCameras => _cameraByPlayerId;

        public void RegisterCamera(int playerId, Camera camera)
        {
            if (camera == null)
            {
                DebugUtility.LogVerbose<CameraResolverService>(
                    $"Ignoring camera registration for playerId={playerId} because camera is null.");
                return;
            }

            if (_cameraByPlayerId.TryGetValue(playerId, out var currentCamera) && currentCamera == camera)
            {
                return;
            }

            _cameraByPlayerId[playerId] = camera;

            DebugUtility.LogVerbose<CameraResolverService>(
                $"Camera registered for playerId={playerId}: {camera.name}.",
                DebugUtility.Colors.Info);

            if (playerId == 0)
            {
                UpdateDefaultCamera(camera);
            }
        }

        public void UnregisterCamera(int playerId, Camera camera)
        {
            if (!_cameraByPlayerId.TryGetValue(playerId, out var currentCamera) || currentCamera != camera)
            {
                return;
            }

            _cameraByPlayerId.Remove(playerId);

            DebugUtility.LogVerbose<CameraResolverService>(
                $"Camera unregistered for playerId={playerId}: {camera.name}.",
                DebugUtility.Colors.Info);

            if (playerId == 0)
            {
                UpdateDefaultCamera(null);
            }
        }

        public Camera GetCamera(int playerId)
        {
            if (_cameraByPlayerId.TryGetValue(playerId, out var camera) && camera != null)
            {
                return camera;
            }

            return GetDefaultCamera();
        }

        public Camera GetDefaultCamera()
        {
            return _defaultCamera ?? Camera.main;
        }

        private void UpdateDefaultCamera(Camera nextDefault)
        {
            if (_defaultCamera == nextDefault)
            {
                return;
            }

            _defaultCamera = nextDefault;
            OnDefaultCameraChanged?.Invoke(_defaultCamera);

            var description = nextDefault == null ? "null" : nextDefault.name;

            DebugUtility.LogVerbose<CameraResolverService>(
                $"Default camera updated to {description} (playerId=0).",
                DebugUtility.Colors.Info);
        }
    }
}
