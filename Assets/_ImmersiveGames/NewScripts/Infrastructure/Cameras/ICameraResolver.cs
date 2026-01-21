/*
 * ChangeLog
 * - Documentado contrato de resolução de câmera com expectativa de fallback resiliente.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Cameras
{
    public interface ICameraResolver
    {
        void RegisterCamera(int playerId, Camera camera);
        void UnregisterCamera(int playerId, Camera camera);
        Camera GetCamera(int playerId);
        Camera GetDefaultCamera();
        event Action<Camera> OnDefaultCameraChanged;
        IReadOnlyDictionary<int, Camera> AllCameras { get; }
    }
}
