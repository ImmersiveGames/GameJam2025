using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.CameraSystems
{
    /// <summary>
    /// Implementação global do sistema de resolução de câmeras.
    /// Suporta multiplayer futuro através da indexação por playerId.
    /// Fornece fallback seguro e notificação de troca da câmera padrão.
    /// </summary>
    public class CameraResolverService : ICameraResolver
    {
        private readonly Dictionary<int, Camera> _cameraByPlayerId = new();
        private Camera _defaultCamera;

        public event System.Action<Camera> OnDefaultCameraChanged;

        public IReadOnlyDictionary<int, Camera> AllCameras => _cameraByPlayerId;

        public void RegisterCamera(int playerId, Camera camera)
        {
            if (camera == null) return;

            _cameraByPlayerId[playerId] = camera;

            if (playerId == 0)
            {
                _defaultCamera = camera;
                OnDefaultCameraChanged?.Invoke(camera);
            }
        }

        public void UnregisterCamera(int playerId, Camera camera)
        {
            if (_cameraByPlayerId.TryGetValue(playerId, out var current) && current == camera)
            {
                _cameraByPlayerId.Remove(playerId);

                if (playerId == 0)
                {
                    _defaultCamera = null;
                    OnDefaultCameraChanged?.Invoke(null);
                }
            }
        }

        public Camera GetCamera(int playerId)
        {
            return _cameraByPlayerId.GetValueOrDefault(playerId, _defaultCamera);
        }

        public Camera GetDefaultCamera() => _defaultCamera;
    }
}