using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.CameraSystems
{
    /// <summary>
    /// Implementação global do sistema de resolução de câmeras.
    /// Suporta multiplayer futuro através da indexação por playerId.
    /// Fornece fallback seguro e notificação de troca da câmera padrão.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public class CameraResolverService : ICameraResolver
    {
        private readonly Dictionary<int, Camera> _cameraByPlayerId = new();
        private Camera _defaultCamera;

        public event Action<Camera> OnDefaultCameraChanged;

        public IReadOnlyDictionary<int, Camera> AllCameras => _cameraByPlayerId;

        public void RegisterCamera(int playerId, Camera camera)
        {
            if (camera == null)
                return;

            // Idempotência: se já é a mesma câmera, não faz nada.
            if (_cameraByPlayerId.TryGetValue(playerId, out var current) && current == camera)
            {
                return;
            }

            _cameraByPlayerId[playerId] = camera;

            if (playerId == 0)
            {
                if (_defaultCamera == camera)
                {
                    return;
                }

                _defaultCamera = camera;
                OnDefaultCameraChanged?.Invoke(camera);

                DebugUtility.LogVerbose<CameraResolverService>(
                    $"DefaultCamera alterada para: {camera.name} (playerId=0).");
            }
        }

        public void UnregisterCamera(int playerId, Camera camera)
        {
            if (camera == null)
                return;

            if (_cameraByPlayerId.TryGetValue(playerId, out var current) && current == camera)
            {
                _cameraByPlayerId.Remove(playerId);

                if (playerId == 0)
                {
                    if (_defaultCamera != null)
                    {
                        _defaultCamera = null;
                        OnDefaultCameraChanged?.Invoke(null);

                        DebugUtility.LogVerbose<CameraResolverService>(
                            "DefaultCamera removida (playerId=0).");
                    }
                }
            }
        }

        public Camera GetCamera(int playerId)
        {
            if (_cameraByPlayerId.TryGetValue(playerId, out var camera) && camera != null)
            {
                return camera;
            }

            return _defaultCamera;
        }

        public Camera GetDefaultCamera() => _defaultCamera;
    }
}
