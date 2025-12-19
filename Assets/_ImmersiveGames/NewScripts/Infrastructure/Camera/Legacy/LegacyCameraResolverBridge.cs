// TEMP: Legacy bridge. Remove after NewScripts Camera system is implemented (NS-CAM-001).
using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.CameraSystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Cameras.Legacy
{
    /// <summary>
    /// Resolver mínimo de câmera para o baseline NewScripts enquanto o sistema definitivo não chega.
    /// </summary>
    public sealed class LegacyCameraResolverBridge : ICameraResolver
    {
        private readonly Dictionary<int, Camera> _registeredCameras = new(4);

        public event Action<Camera> OnDefaultCameraChanged
        {
            add { }
            remove { }
        }

        public IReadOnlyDictionary<int, Camera> AllCameras => _registeredCameras;

        public void RegisterCamera(int playerId, Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            _registeredCameras[playerId] = camera;
        }

        public void UnregisterCamera(int playerId, Camera camera)
        {
            if (_registeredCameras.TryGetValue(playerId, out var existing) && existing == camera)
            {
                _registeredCameras.Remove(playerId);
            }
        }

        public Camera GetCamera(int playerId)
        {
            return _registeredCameras.TryGetValue(playerId, out var camera) ? camera : null;
        }

        public Camera GetDefaultCamera()
        {
            if (_registeredCameras.TryGetValue(0, out var registeredDefault) && registeredDefault != null)
            {
                return registeredDefault;
            }

            if (Camera.main != null)
            {
                return Camera.main;
            }

            var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
            foreach (var cam in cameras)
            {
                if (cam != null && cam.isActiveAndEnabled)
                {
                    return cam;
                }
            }

            return null;
        }
    }
}
