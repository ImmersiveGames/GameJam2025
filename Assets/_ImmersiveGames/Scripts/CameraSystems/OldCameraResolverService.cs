using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.Scripts.CameraSystems
{
    /// <summary>
    /// Implementa��o global do sistema de resolu��o de c�meras.
    /// Suporta multiplayer futuro atrav�s da indexa��o por playerId.
    /// Fornece fallback seguro e notifica��o de troca da c�mera padr�o.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public class OldCameraResolverService : IOldCameraResolver
    {
        private readonly Dictionary<int, Camera> _cameraByPlayerId = new();
        private Camera _defaultCamera;

        public event Action<Camera> OnDefaultCameraChanged;

        public IReadOnlyDictionary<int, Camera> AllCameras => _cameraByPlayerId;

        public void RegisterCamera(int playerId, Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            // Idempot�ncia: se j� � a mesma c�mera, n�o faz nada.
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

                DebugUtility.LogVerbose<OldCameraResolverService>(
                    $"DefaultCamera alterada para: {camera.name} (playerId=0).");
            }
        }

        public void UnregisterCamera(int playerId, Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            if (_cameraByPlayerId.TryGetValue(playerId, out var current) && current == camera)
            {
                _cameraByPlayerId.Remove(playerId);

                if (playerId == 0)
                {
                    if (_defaultCamera != null)
                    {
                        _defaultCamera = null;
                        OnDefaultCameraChanged?.Invoke(null);

                        DebugUtility.LogVerbose<OldCameraResolverService>(
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

